# Phase 3: インターフェース定義書 (Interface Definition)

このファイルはC#バックエンド実装の「技術的契約」です。AIは以下の `interface` と `record` の構造を遵守すること。

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Synapse.Core
{
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
    public record GetStatusPayload(); // Handshake (Issue #9)

    // Event Payloads (C# -> UI)
    public record CameraFrameEvent(int slotIndex, string base64Image, string timestamp);
    public record OcrResultEvent(int slotIndex, string title, string artist, string album);
    public record StatusResponseEvent(string status); // Handshake (Issue #9)
    
    // ★新規: MIDIアクションイベント (GO, Undo, Redo, Toggle)
    public record MidiActionEvent(int slotIndex, string action);

    // --- 3. Hardware Interfaces ---
    public interface ICameraProvider
    {
        string DeviceId { get; }
        string DeviceName { get; }
        bool CanSetExposure { get; }
        bool CanSetFocus { get; }
        int TargetFps { get; set; } 
        Task ConnectAsync(string deviceId);
        void Disconnect();
    }

    public interface IOcrEngine
    {
        Task<OcrResultEvent> RecognizeTextAsync(byte[] imageBytes, CancellationToken cancellationToken);
    }
}
```