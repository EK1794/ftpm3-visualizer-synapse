using System;
using System.Text.Json;
using NAudio.Midi;

namespace Synapse.Core;

public class MidiProvider : IDisposable
{
    private MidiIn? _midiIn;

    public MidiProvider()
    {
        if (MidiIn.NumberOfDevices > 0)
        {
            try
            {
                _midiIn = new MidiIn(0); // Connect to first available MIDI device
                _midiIn.MessageReceived += MidiIn_MessageReceived;
                _midiIn.Start();
                Console.WriteLine(JsonSerializer.Serialize(new { @event = "log", message = $"Connected to MIDI Device: {MidiIn.DeviceInfo(0).ProductName}" }));
            }
            catch (Exception ex)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { @event = "error", message = "MIDI Init Failed: " + ex.Message }));
            }
        }
        else
        {
            Console.WriteLine(JsonSerializer.Serialize(new { @event = "error", message = "No MIDI Devices found." }));
        }
    }

    private void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
    {
        if (e.MidiEvent is NoteOnEvent noteOnEvent)
        {
            Console.WriteLine(JsonSerializer.Serialize(new 
            { 
                @event = "MidiMessageEvent", 
                noteNumber = noteOnEvent.NoteNumber, 
                velocity = noteOnEvent.Velocity, 
                isNoteOn = noteOnEvent.Velocity > 0 
            }));
        }
        else if (e.MidiEvent is NoteEvent noteEvent && noteEvent.CommandCode == MidiCommandCode.NoteOff)
        {
            Console.WriteLine(JsonSerializer.Serialize(new 
            { 
                @event = "MidiMessageEvent", 
                noteNumber = noteEvent.NoteNumber, 
                velocity = noteEvent.Velocity, 
                isNoteOn = false
            }));
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
