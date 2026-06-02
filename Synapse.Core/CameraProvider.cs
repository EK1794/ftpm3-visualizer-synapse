using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Synapse.Core;

public class OpenCvCameraProvider : ICameraProvider
{
    public string DeviceId { get; private set; } = "";
    public int TargetFps { get; set; } = 5;

    private byte[]? _latestFrameBytes;
    private VideoCapture? _capture;
    private CancellationTokenSource? _cts;
    private Task? _captureTask;
    private readonly int _slotIndex;
    private readonly IStdoutWriter _writer;

    public OpenCvCameraProvider(int slotIndex, IStdoutWriter writer)
    {
        _slotIndex = slotIndex;
        _writer = writer;
    }

    public Task ConnectAsync(string deviceId)
    {
        Disconnect();
        DeviceId = deviceId;
        
        if (!int.TryParse(deviceId, out int camIndex))
        {
            throw new ArgumentException("Invalid device ID format. Expected integer index.");
        }

        _capture = new VideoCapture(camIndex, VideoCaptureAPIs.ANY); // Let OpenCV use MSMF or DirectShow
        if (!_capture.IsOpened())
        {
            throw new Exception($"Failed to open camera index {camIndex}");
        }

        // Set properties 
        _capture.Set(VideoCaptureProperties.FrameWidth, 640);
        _capture.Set(VideoCaptureProperties.FrameHeight, 480);
        _capture.Set(VideoCaptureProperties.Fps, TargetFps); 

        _cts = new CancellationTokenSource();
        _captureTask = Task.Run(() => CaptureLoop(_cts.Token), _cts.Token);

        return Task.CompletedTask;
    }

    private async Task CaptureLoop(CancellationToken token)
    {
        using var mat = new Mat();
        
        while (!token.IsCancellationRequested && _capture != null && _capture.IsOpened())
        {
            var sw = Stopwatch.StartNew();

            if (_capture.Read(mat) && !mat.Empty())
            {
                // Encode to JPEG
                var imgBytes = mat.ImEncode(".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, 70));
                _latestFrameBytes = imgBytes;
                
                var base64 = Convert.ToBase64String(imgBytes);

                // Output event
                var evt = new 
                {
                    @event = "CameraFrameEvent",
                    slotIndex = _slotIndex,
                    base64Image = "data:image/jpeg;base64," + base64,
                    timestamp = DateTime.UtcNow.ToString("O")
                };
                
                _writer.WriteLine(JsonSerializer.Serialize(evt));
            }

            int delayMs = (1000 / TargetFps) - (int)sw.ElapsedMilliseconds;
            if (delayMs > 0)
            {
                try
                {
                    await Task.Delay(delayMs, token);
                }
                catch (TaskCanceledException) { break; }
            }
        }
    }

    public void Disconnect()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            try { _captureTask?.Wait(500); } catch {}
            _cts.Dispose();
            _cts = null;
        }

        if (_capture != null)
        {
            _capture.Release();
            _capture.Dispose();
            _capture = null;
        }
    }

    public byte[]? GetLatestFrame() => _latestFrameBytes;
}
