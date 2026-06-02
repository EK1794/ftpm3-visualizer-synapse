using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Management;

namespace Synapse.Core;

class Program
{
    static readonly Dictionary<int, ICameraProvider> _cameras = new();
    static readonly IOcrEngine _ocrEngine = new WindowsOcrEngine();
    static MidiProvider? _midiProvider;
    static readonly CancellationTokenSource _shutdownCts = new();
    static readonly object _stdoutLock = new();
    static int _shutdownFlag = 0;

    /// <summary>
    /// Thread-safe stdout writer. Prevents interleaved JSON output from
    /// concurrent background tasks (camera frames, WMI queries, MIDI events).
    /// </summary>
    internal static void SafeWriteLine(string message)
    {
        lock (_stdoutLock)
        {
            Console.WriteLine(message);
            Console.Out.Flush();
        }
    }

    static async Task Main(string[] args)
    {
        Console.InputEncoding = System.Text.Encoding.UTF8;
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Register graceful shutdown handlers for Ctrl+C and OS process exit
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _shutdownCts.Cancel();
        };
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();

        _midiProvider = new MidiProvider();
        SafeWriteLine(JsonSerializer.Serialize(new { @event = "sidecar_ready" }));

        try
        {
            // Main IPC loop: listen to stdin for commands from Tauri
            while (!_shutdownCts.IsCancellationRequested)
            {
                var line = Console.ReadLine();

                // Issue #1: null means stdin pipe was closed (parent process terminated)
                // This is the primary mechanism for detecting Tauri shutdown.
                // Previously, null and "" were both caught by string.IsNullOrEmpty,
                // causing the process to loop forever after parent exit.
                if (line == null)
                {
                    break;
                }

                // Empty line: skip and continue (not a pipe disconnect)
                if (string.IsNullOrWhiteSpace(line))
                {
                    try
                    {
                        await Task.Delay(50, _shutdownCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { break; }
                    continue;
                }

                try
                {
                    var envelope = JsonSerializer.Deserialize<CommandEnvelope>(line);
                    if (envelope == null || string.IsNullOrEmpty(envelope.command)) continue;

                    await HandleCommand(envelope);
                }
                catch (Exception ex)
                {
                    SafeWriteLine(JsonSerializer.Serialize(new { @event = "error", message = ex.Message }));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown - no action needed
        }
        finally
        {
            Shutdown();
        }
    }

    static async Task HandleCommand(CommandEnvelope envelope)
    {
        switch (envelope.command)
        {
            case "AssignCamera":
                var assignPayload = JsonSerializer.Deserialize<AssignCameraPayload>(envelope.payload.GetRawText());
                if (assignPayload != null)
                {
                    if (_cameras.TryGetValue(assignPayload.slotIndex, out var existing))
                    {
                        existing.Disconnect();
                    }
                    var provider = new OpenCvCameraProvider(assignPayload.slotIndex);
                    // Use a fire-and-forget or await the connection
                    await provider.ConnectAsync(assignPayload.deviceId);
                    _cameras[assignPayload.slotIndex] = provider;
                    
                    SafeWriteLine(JsonSerializer.Serialize(new { @event = "ack", command = envelope.command, slot = assignPayload.slotIndex }));
                }
                break;

            case "TriggerOcr":
                var triggerPayload = JsonSerializer.Deserialize<TriggerOcrPayload>(envelope.payload.GetRawText());
                int slot = triggerPayload?.slotIndex ?? -1;
                
                if (slot != -1 && _cameras.TryGetValue(slot, out var camProvider))
                {
                    var bytes = camProvider.GetLatestFrame();
                    if (bytes != null)
                    {
                        try 
                        {
                            var extracted = await _ocrEngine.RecognizeTextAsync(bytes, default);
                            SafeWriteLine(JsonSerializer.Serialize(new { 
                                @event = "OcrResultEvent", 
                                slotIndex = slot, 
                                title = extracted.Title, 
                                artist = extracted.Artist, 
                                album = extracted.Album 
                            }));
                        }
                        catch (Exception ex)
                        {
                            SafeWriteLine(JsonSerializer.Serialize(new { @event = "error", message = "OCR Failed: " + ex.Message }));
                        }
                    }
                }
                SafeWriteLine(JsonSerializer.Serialize(new { @event = "ack", command = envelope.command, slot = slot }));
                break;

            case "UpdateStagingBuffer":
                var updatePayload = JsonSerializer.Deserialize<UpdateStagingBufferPayload>(envelope.payload.GetRawText());
                // TODO: Implement staging buffer update
                SafeWriteLine(JsonSerializer.Serialize(new { @event = "ack", command = envelope.command, title = updatePayload?.title }));
                break;



            case "GetCameraDevices":
                // Issue #2: Run WMI query on background thread to prevent blocking the IPC loop.
                // WMI queries (ManagementObjectSearcher) can take hundreds of milliseconds,
                // during which stdin ReadLine and MIDI events would be stalled.
                _ = Task.Run(() =>
                {
                    var devicesInfo = new List<object>();
                    try
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')");
                            int index = 0;
                            foreach (var device in searcher.Get())
                            {
                                var name = device["Caption"]?.ToString() ?? $"Camera {index}";
                                devicesInfo.Add(new { id = index.ToString(), name = name });
                                index++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SafeWriteLine(JsonSerializer.Serialize(new { @event = "error", message = "WMI Error: " + ex.Message }));
                    }

                    SafeWriteLine(JsonSerializer.Serialize(new { @event = "CameraDevicesResult", devices = devicesInfo }));
                });
                break;

            case "GetStatus":
                // Issue #9: Pull-based handshake. Frontend calls this after listener registration
                // to reliably detect Sidecar readiness, regardless of startup timing.
                SafeWriteLine(JsonSerializer.Serialize(new { @event = "StatusResponse", status = "Ready" }));
                break;

            default:
                SafeWriteLine(JsonSerializer.Serialize(new { @event = "unknown_command", command = envelope.command }));
                break;
        }
    }

    /// <summary>
    /// Graceful shutdown: release all hardware resources (cameras, MIDI) and exit.
    /// Called on stdin EOF (parent process terminated), Ctrl+C, or OS ProcessExit.
    /// Thread-safe and idempotent (safe to call multiple times).
    /// </summary>
    static void Shutdown()
    {
        // Ensure shutdown logic runs only once via atomic flag
        if (Interlocked.Exchange(ref _shutdownFlag, 1) != 0)
            return;

        if (!_shutdownCts.IsCancellationRequested)
            _shutdownCts.Cancel();

        // Release all camera resources (VideoCapture handles, capture threads)
        foreach (var kvp in _cameras)
        {
            try { kvp.Value.Disconnect(); } catch { }
        }
        _cameras.Clear();

        // Release MIDI resources (NAudio MidiIn handles)
        try { _midiProvider?.Dispose(); } catch { }
        _midiProvider = null;
    }
}
