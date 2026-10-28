using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using JobMagnet.Api.Cv;

namespace JobMagnet.Tests.Api;

public class CvTextExtractorTests
{
    [Theory]
    [InlineData("cv.pdf", true)]
    [InlineData("CV.DOCX", true)]
    [InlineData("cv.doc", false)]
    [InlineData("photo.jpg", false)]
    public void IsSupported_AcceptsPdfAndDocx(string fileName, bool expected) =>
        Assert.Equal(expected, CvTextExtractor.IsSupported(fileName));

    [Fact]
    public void Extract_ReadsDocxParagraphs()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(
                new Paragraph(new Run(new Text("Jānis Bērziņš"))),
                new Paragraph(new Run(new Text("Senior .NET developer")))));
        }

        stream.Position = 0;
        var text = CvTextExtractor.Extract(stream, "cv.docx");

        Assert.Equal("Jānis Bērziņš\nSenior .NET developer", text);
    }
}
