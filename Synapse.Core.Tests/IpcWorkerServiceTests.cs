using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Moq;
using Synapse.Core;
using Xunit;

namespace Synapse.Core.Tests;

public class IpcWorkerServiceTests
{
    [Fact]
    public async Task HandleCommand_UpdateStagingBuffer_ShouldOutputAck()
    {
        // Arrange
        var writerMock = new Mock<IStdoutWriter>();
        var ocrMock = new Mock<IOcrEngine>();
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        
        using var midiProvider = new MidiProvider(writerMock.Object);
        var service = new IpcWorkerService(writerMock.Object, ocrMock.Object, midiProvider, lifetimeMock.Object);
        
        var envelope = new CommandEnvelope
        {
            command = "UpdateStagingBuffer",
            payload = JsonDocument.Parse("{\"title\":\"Test Title\", \"artist\":\"Test Artist\", \"album\":\"Test Album\", \"isImageForced\":false}").RootElement
        };

        // Act
        await service.HandleCommand(envelope, CancellationToken.None);

        // Assert
        writerMock.Verify(w => w.WriteLine(It.Is<string>(s => 
            s.Contains("\"event\":\"ack\"") && 
            s.Contains("\"command\":\"UpdateStagingBuffer\"") &&
            s.Contains("\"title\":\"Test Title\"")
        )), Times.Once);
    }
}
