using System.Text;

using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace JobMagnet.Api.Cv;

public static class CvTextExtractor
{
    public const string PdfExtension = ".pdf";
    public const string DocxExtension = ".docx";

    public static bool IsSupported(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() is PdfExtension or DocxExtension;

    public static string Extract(Stream stream, string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        PdfExtension => ExtractPdf(stream),
        DocxExtension => ExtractDocx(stream),
        _ => throw new ArgumentException($"Unsupported CV format: {fileName}", nameof(fileName))
    };

    private static string ExtractPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }

    private static string ExtractDocx(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, isEditable: false);
        var paragraphs = document.MainDocumentPart?.Document?.Body?
            .Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
            .Select(p => p.InnerText) ?? [];

        return string.Join('\n', paragraphs);
    }
}
