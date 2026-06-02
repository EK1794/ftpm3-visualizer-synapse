using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Synapse.Core;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // Core Services
                services.AddSingleton<IStdoutWriter, ConsoleStdoutWriter>();
                services.AddSingleton<IOcrEngine, WindowsOcrEngine>();
                
                // Hardware Providers
                services.AddSingleton<MidiProvider>();
                
                // Background Worker (IPC Loop)
                services.AddHostedService<IpcWorkerService>();
            })
            .Build();

        await host.RunAsync();
    }
}

