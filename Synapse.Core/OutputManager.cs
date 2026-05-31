using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Synapse.Core;

public static class OutputManager
{
    private static readonly string OutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SynapseOutput");

    public static async Task WriteNextTrackAsync(string title, string artist, string album, string? base64Image)
    {
        if (!Directory.Exists(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
        }

        var utf8Bom = new System.Text.UTF8Encoding(true);
        await File.WriteAllTextAsync(Path.Combine(OutputFolder, "title.txt"), title, utf8Bom);
        await File.WriteAllTextAsync(Path.Combine(OutputFolder, "artist.txt"), artist, utf8Bom);
        await File.WriteAllTextAsync(Path.Combine(OutputFolder, "album.txt"), album, utf8Bom);

        if (!string.IsNullOrEmpty(base64Image))
        {
            try 
            {
                byte[] imgBytes = Convert.FromBase64String(base64Image.Replace("data:image/jpeg;base64,", "").Replace("data:image/png;base64,", ""));
                await File.WriteAllBytesAsync(Path.Combine(OutputFolder, "artwork.jpg"), imgBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { @event = "error", message = "Failed to decode artwork: " + ex.Message }));
            }
        }
    }
}
