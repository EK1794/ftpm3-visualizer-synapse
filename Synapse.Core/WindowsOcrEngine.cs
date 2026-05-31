using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace Synapse.Core;

public class WindowsOcrEngine : IOcrEngine
{
    private readonly OcrEngine? _ocrEngine;

    public WindowsOcrEngine()
    {
        var lang = new Windows.Globalization.Language("ja-JP");
        if (OcrEngine.IsLanguageSupported(lang))
        {
            _ocrEngine = OcrEngine.TryCreateFromLanguage(lang);
        }
        else
        {
            _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
        }
    }

    public async Task<OcrExtractedData> RecognizeTextAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        if (_ocrEngine == null)
            throw new NotSupportedException("Windows OCR Engine could not be initialized.");

        using var memStream = new InMemoryRandomAccessStream();
        using (var dataWriter = new DataWriter(memStream.GetOutputStreamAt(0)))
        {
            dataWriter.WriteBytes(imageBytes);
            await dataWriter.StoreAsync();
        }

        var decoder = await BitmapDecoder.CreateAsync(memStream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);

        var lines = ocrResult.Lines.Select(l => l.Text).ToList();
        
        string title = lines.Count > 0 ? lines[0] : "";
        string artist = lines.Count > 1 ? lines[1] : "";
        string album = lines.Count > 2 ? string.Join(" ", lines.Skip(2)) : "";

        return new OcrExtractedData(title, artist, album);
    }
}
