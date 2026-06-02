using System;
using System.Text.Json;
using NAudio.Midi;

namespace Synapse.Core;

public class MidiProvider : IDisposable
{
    private MidiIn? _midiIn;
    private readonly IStdoutWriter _writer;

    public MidiProvider(IStdoutWriter writer)
    {
        _writer = writer;
        if (MidiIn.NumberOfDevices > 0)
        {
            try
            {
                _midiIn = new MidiIn(0); // Connect to first available MIDI device
                _midiIn.MessageReceived += MidiIn_MessageReceived;
                _midiIn.Start();
                _writer.WriteLine(JsonSerializer.Serialize(new { @event = "log", message = $"Connected to MIDI Device: {MidiIn.DeviceInfo(0).ProductName}" }));
            }
            catch (Exception ex)
            {
                _writer.WriteLine(JsonSerializer.Serialize(new { @event = "error", message = "MIDI Init Failed: " + ex.Message }));
            }
        }
        else
        {
            _writer.WriteLine(JsonSerializer.Serialize(new { @event = "error", message = "No MIDI Devices found." }));
        }
    }

    private void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
    {
        if (e.MidiEvent is NoteOnEvent noteOnEvent)
        {
            bool isNoteOn = noteOnEvent.Velocity > 0;

            // Debug log to identify DDJ-SP1 channel mapping
            if (isNoteOn)
            {
                _writer.WriteLine(JsonSerializer.Serialize(new { 
                    @event = "log", 
                    message = $"DEBUG_MIDI: Channel={noteOnEvent.Channel}, Note={noteOnEvent.NoteNumber}" 
                }));
            }

            // Issue #7: MIDI Multi-mapping (16 Pad Routing)
            if (isNoteOn)
            {
                // Channel 8 = Left Deck (Slots 0, 1)
                // Channel 9 = Right Deck (Slots 2, 3)
                int baseSlot = (noteOnEvent.Channel == 9) ? 2 : 0;
                int slotIndex = baseSlot + (noteOnEvent.NoteNumber / 4);
                int actionIndex = noteOnEvent.NoteNumber % 4;
                string action = actionIndex switch
                {
                    0 => "go",
                    1 => "undo",
                    2 => "redo",
                    3 => "toggle",
                    _ => "unknown"
                };

                _writer.WriteLine(JsonSerializer.Serialize(new 
                { 
                    @event = "MidiAction", 
                    payload = new { slotIndex = slotIndex, action = action }
                }));
            }
        }
    }

    public void Dispose()
    {
        if (_midiIn != null)
        {
            _midiIn.Stop();
            _midiIn.Dispose();
            _midiIn = null;
        }
    }
}
