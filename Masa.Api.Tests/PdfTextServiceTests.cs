using Masa.Api.Materials;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Masa.Api.Tests;

public class PdfTextServiceTests
{
    private readonly PdfTextService _service = new();

    private static MemoryStream BuildPdf(params string?[] pageTexts)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        foreach (var text in pageTexts)
        {
            var page = builder.AddPage(PageSize.A4);
            if (text is null) continue;
            var y = 800;
            foreach (var chunk in text.Chunk(80))
            {
                page.AddText(new string(chunk), 8, new PdfPoint(20, y), font);
                y -= 10;
            }
        }
        return new MemoryStream(builder.Build());
    }

    [Fact]
    public void Pdf_without_text_is_empty()
    {
        var result = _service.Extract(BuildPdf(null, null));
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void Short_pdf_is_not_truncated()
    {
        var result = _service.Extract(BuildPdf("Stack is LIFO.", "Queue is FIFO."));
        Assert.False(result.Truncated);
        Assert.Equal([1, 2], result.Pages.Select(p => p.PageNumber));
        Assert.Contains("LIFO", result.Pages[0].Text);
    }

    [Fact]
    public void More_than_fifteen_pages_is_truncated_at_fifteen()
    {
        var result = _service.Extract(BuildPdf(Enumerable.Range(1, 20).Select(i => (string?)$"Page {i}").ToArray()));
        Assert.True(result.Truncated);
        Assert.Equal(PdfTextService.MaxPages, result.Pages.Count);
    }

    [Fact]
    public void Text_is_cut_at_twelve_thousand_characters()
    {
        var page = new string('a', 5000);
        var result = _service.Extract(BuildPdf(page, page, page, page));
        Assert.True(result.Truncated);
        Assert.Equal(PdfTextService.MaxChars, result.Pages.Sum(p => p.Text.Length));
        Assert.Equal(3, result.Pages.Count);
    }
}
