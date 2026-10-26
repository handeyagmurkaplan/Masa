using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Masa.Api.Materials;

public sealed record PdfPageText(int PageNumber, string Text);

public sealed record PdfExtraction(IReadOnlyList<PdfPageText> Pages, bool Truncated)
{
    public bool IsEmpty => Pages.Count == 0;
}

public sealed class PdfTextService
{
    public const int MaxPages = 15;
    public const int MaxChars = 12_000;

    /// <summary>İlk 15 sayfa veya 12.000 karakter, hangisi önce dolarsa. Metinsiz sayfa atlanır.</summary>
    public PdfExtraction Extract(Stream pdf)
    {
        using var document = PdfDocument.Open(pdf);
        var pages = new List<PdfPageText>();
        var total = 0;
        var truncated = document.NumberOfPages > MaxPages;

        foreach (var page in document.GetPages().Take(MaxPages))
        {
            var text = Normalize(ContentOrderTextExtractor.GetText(page));
            if (text.Length == 0) continue;

            var remaining = MaxChars - total;
            if (text.Length > remaining)
            {
                text = text[..remaining];
                truncated = true;
            }

            pages.Add(new PdfPageText(page.Number, text));
            total += text.Length;
            if (total >= MaxChars)
            {
                // Sonraki sayfalarda da metin olabilir; kesildi say.
                truncated = truncated || page.Number < document.NumberOfPages;
                break;
            }
        }

        return new PdfExtraction(pages, truncated);
    }

    private static string Normalize(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0) sb.Append(trimmed).Append('\n');
        }
        return sb.ToString().TrimEnd();
    }
}
