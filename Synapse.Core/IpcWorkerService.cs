using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using Microsoft.Extensions.Hosting;

namespace Synapse.Core;

/// <summary>
/// Background service that manages the IPC loop (stdin/stdout) with the Tauri frontend.
/// Replaces the old Program.Main loop. Maintains all Issue #1 (zombie prevention)
/// and Issue #2 (async WMI) behaviors.
/// </summary>
public class IpcWorkerService : BackgroundService
{
    private readonly IStdoutWriter _writer;
    private readonly IOcrEngine _ocrEngine;
    private readonly MidiProvider _midiProvider;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly Dictionary<int, ICameraProvider> _cameras = new();

    public IpcWorkerService(
        IStdoutWriter writer,
        IOcrEngine ocrEngine,
        MidiProvider midiProvider,
        IHostApplicationLifetime lifetime)
    {
        _writer = writer;
        _ocrEngine = ocrEngine;
        _midiProvider = midiProvider;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.InputEncoding = System.Text.Encoding.UTF8;
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        _writer.WriteLine(JsonSerializer.Serialize(new { @event = "sidecar_ready" }));

        try
        {
            // Main IPC loop: listen to stdin for commands from Tauri
            while (!stoppingToken.IsCancellationRequested)
            {
                var line = Console.ReadLine();

                // Issue #1: null means stdin pipe was closed (parent process terminated)
                // This is the primary mechanism for detecting Tauri shutdown.
                if (line == null)
                {
                    break;
                }

                // Empty line: skip and continue (not a pipe disconnect)
                if (string.IsNullOrWhiteSpace(line))
                {
                    try
                    {
                        await Task.Delay(50, stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { break; }
                    continue;
                }

                try
                {
                    var envelope = JsonSerializer.Deserialize<CommandEnvelope>(line);
                    if (envelope == null || string.IsNullOrEmpty(envelope.command)) continue;

                    await HandleCommand(envelope, stoppingToken);
                }
                catch (Exception ex)
                {
                    _writer.WriteLine(JsonSerializer.Serialize(new { @event = "error", message = ex.Message }));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown - no action needed
        }
        finally
        {
            // Signal the host to stop, which triggers graceful shutdown of all services
            _lifetime.StopApplication();
        }
    }

    public async Task HandleCommand(CommandEnvelope envelope, CancellationToken stoppingToken)
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
                    var provider = new OpenCvCameraProvider(assignPayload.slotIndex, _writer);
                    await provider.ConnectAsync(assignPayload.deviceId);
                    _cameras[assignPayload.slotIndex] = provider;

                    _writer.WriteLine(JsonSerializer.Serialize(new { @event = "ack", command = envelope.command, slot = assignPayload.slotIndex }));
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
                            _writer.WriteLine(JsonSerializer.Serialize(new
                            {
                                @event = "OcrResultEvent",
                                slotIndex = slot,
                                title = extracted.Title,
                                artist = extracted.Artist,
                                album = extracted.Album
                            }));
                        }
                        catch (Exception ex)
                        {
                            _writer.WriteLine(JsonSerializer.Serialize(new { @event = "error", message = "OCR Failed: " + ex.Message }));
                        }
                    }
                }
                _writer.WriteLine(JsonSerializer.Serialize(new { @event = "ack", command = envelope.command, slot = slot }));
                break;

            case "UpdateStagingBuffer":
                var updatePayload = JsonSerializer.Deserialize<UpdateStagingBufferPayload>(envelope.payload.GetRawText());
                // TODO: Implement staging buffer update
                _writer.WriteLine(JsonSerializer.Serialize(new { @event = "ack", command = envelope.command, title = updatePayload?.title }));
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
                        _writer.WriteLine(JsonSerializer.Serialize(new { @event = "error", message = "WMI Error: " + ex.Message }));
                    }

                    _writer.WriteLine(JsonSerializer.Serialize(new { @event = "CameraDevicesResult", devices = devicesInfo }));
                });
                break;

            case "GetStatus":
                // Issue #9: Pull-based handshake. Frontend calls this after listener registration
                // to reliably detect Sidecar readiness, regardless of startup timing.
                _writer.WriteLine(JsonSerializer.Serialize(new { @event = "StatusResponse", status = "Ready" }));
                break;

            default:
                _writer.WriteLine(JsonSerializer.Serialize(new { @event = "unknown_command", command = envelope.command }));
                break;
        }
    }

    /// <summary>
    /// Graceful shutdown: release all hardware resources (cameras).
    /// Called automatically by the Host when StopApplication() is invoked.
    /// MIDI resources are released via MidiProvider.Dispose() by the DI container.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Release all camera resources (VideoCapture handles, capture threads)
        foreach (var kvp in _cameras)
        {
            try { kvp.Value.Disconnect(); } catch { }
        }
        _cameras.Clear();

        await base.StopAsync(cancellationToken);
    }
}
