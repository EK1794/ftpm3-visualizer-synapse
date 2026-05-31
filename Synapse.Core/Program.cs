using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Management;

namespace Synapse.Core;

class Program
{
    static readonly Dictionary<int, ICameraProvider> _cameras = new();
    static readonly IOcrEngine _ocrEngine = new WindowsOcrEngine();
    static MidiProvider? _midiProvider;

    static async Task Main(string[] args)
    {
        Console.InputEncoding = System.Text.Encoding.UTF8;
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        _midiProvider = new MidiProvider();
        Console.WriteLine(JsonSerializer.Serialize(new { @event = "sidecar_ready" }));

        // Start listening to stdin for commands from Tauri
        while (true)
        {
            var line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
            {
                await Task.Delay(100);
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
                Console.WriteLine(JsonSerializer.Serialize(new { @event = "error", message = ex.Message }));
            }
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
                    
                    Console.WriteLine(JsonSerializer.Serialize(new { @event = "ack", command = envelope.command, slot = assignPayload.slotIndex }));
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
                            Console.WriteLine(JsonSerializer.Serialize(new { 
                                @event = "OcrResultEvent", 
                                slotIndex = slot, 
                                title = extracted.Title, 
                                artist = extracted.Artist, 
                                album = extracted.Album 
                            }));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(JsonSerializer.Serialize(new { @event = "error", message = "OCR Failed: " + ex.Message }));
                        }
                    }
                }
                Console.WriteLine(JsonSerializer.Serialize(new { @event = "ack", command = envelope.command, slot = slot }));
                break;

            case "UpdateStagingBuffer":
                var updatePayload = JsonSerializer.Deserialize<UpdateStagingBufferPayload>(envelope.payload.GetRawText());
                // TODO: Implement staging buffer update
                Console.WriteLine(JsonSerializer.Serialize(new { @event = "ack", command = envelope.command, title = updatePayload?.title }));
                break;

            case "TriggerOutput":
                var outPayload = JsonSerializer.Deserialize<TriggerOutputPayload>(envelope.payload.GetRawText());
                if (outPayload != null)
                {
                    try
                    {
                        await OutputManager.WriteNextTrackAsync(
                            outPayload.title ?? "", 
                            outPayload.artist ?? "", 
                            outPayload.album ?? "", 
                            outPayload.base64Image
                        );
                        Console.WriteLine(JsonSerializer.Serialize(new { @event = "ack", command = envelope.command, success = true }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(JsonSerializer.Serialize(new { @event = "error", message = "Output write failed: " + ex.Message }));
                    }
                }
                break;

            case "GetCameraDevices":
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
                    Console.WriteLine(JsonSerializer.Serialize(new { @event = "error", message = "WMI Error: " + ex.Message }));
                }
                
                Console.WriteLine(JsonSerializer.Serialize(new { @event = "CameraDevicesResult", devices = devicesInfo }));
                break;

            default:
                Console.WriteLine(JsonSerializer.Serialize(new { @event = "unknown_command", command = envelope.command }));
                break;
        }
    }
}
