using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Synapse.Core;

// --- 1. Domain Models ---
public record TrackInfo(
    string Title,
    string Artist,
    string Album,
    byte[]? ArtworkBytes,
    DataSourceType SourceType,
    bool IsImageForced
);

public enum DataSourceType { ProLink, Vision, Manual }

// --- 2. IPC Message Envelopes ---
public class CommandEnvelope
{
    public string command { get; set; } = string.Empty;
    public JsonElement payload { get; set; }
}

// Command Payloads (UI -> C#)
public record AssignCameraPayload(int slotIndex, string deviceId);
public record TriggerOcrPayload(int slotIndex);
public record UpdateStagingBufferPayload(string title, string artist, string album, bool isImageForced);
public record TriggerOutputPayload(string title, string artist, string album, string? base64Image, bool isImageForced);

// Event Payloads (C# -> UI)
public record CameraFrameEvent(int slotIndex, string base64Image, string timestamp);
public record OcrResultEvent(int slotIndex, string title, string artist, string album);
public record MidiMessageEvent(int noteNumber, int velocity, bool isNoteOn);

// --- 3. Hardware Interfaces ---
public interface ICameraProvider
{
    string DeviceId { get; }
    int TargetFps { get; set; } 
    Task ConnectAsync(string deviceId);
    void Disconnect();
    byte[]? GetLatestFrame();
}

public record OcrExtractedData(string Title, string Artist, string Album);

public interface IOcrEngine
{
    Task<OcrExtractedData> RecognizeTextAsync(byte[] imageBytes, CancellationToken cancellationToken);
}
