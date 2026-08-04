using Moq;
using Synapse.Core;
using Xunit;

namespace Synapse.Core.Tests;

public class MidiProviderTests
{
    [Theory]
    [InlineData(8, 0, 0, "go")]
    [InlineData(8, 1, 0, "undo")]
    [InlineData(8, 2, 0, "redo")]
    [InlineData(8, 3, 0, "toggle")]
    [InlineData(8, 4, 1, "go")]
    [InlineData(8, 7, 1, "toggle")]
    [InlineData(9, 0, 2, "go")]
    [InlineData(9, 7, 3, "toggle")]
    public void ProcessNoteOn_ShouldMapToCorrectSlotAndAction(int channel, int noteNumber, int expectedSlot, string expectedAction)
    {
        // Arrange
        var writerMock = new Mock<IStdoutWriter>();
        using var provider = new MidiProvider(writerMock.Object);

        // Act
        provider.ProcessNoteOn(channel, noteNumber, 127);

        // Assert
        writerMock.Verify(w => w.WriteLine(It.Is<string>(s => 
            s.Contains("\"event\":\"MidiAction\"") && 
            s.Contains($"\"slotIndex\":{expectedSlot}") && 
            s.Contains($"\"action\":\"{expectedAction}\"")
        )), Times.Once);
    }
}
