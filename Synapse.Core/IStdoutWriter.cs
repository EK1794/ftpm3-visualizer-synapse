using System;

namespace Synapse.Core;

/// <summary>
/// Abstraction for thread-safe stdout writing.
/// Allows DI injection instead of static dependency on Program.SafeWriteLine.
/// </summary>
public interface IStdoutWriter
{
    void WriteLine(string message);
}

/// <summary>
/// Thread-safe stdout writer. Prevents interleaved JSON output from
/// concurrent background tasks (camera frames, WMI queries, MIDI events).
/// </summary>
public class ConsoleStdoutWriter : IStdoutWriter
{
    private readonly object _lock = new();

    public void WriteLine(string message)
    {
        lock (_lock)
        {
            Console.WriteLine(message);
            Console.Out.Flush();
        }
    }
}
