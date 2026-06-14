using System.IO;
using System.Text;
using UglyToad.PdfPig;

namespace RAGSharp.IO
{
    /// <summary>
    /// Extracts plain‑text from a PDF file using PdfPig.
    /// </summary>
    public static class PdfTextExtractor
    {
        /// <summary>
        /// Returns the concatenated text of all pages in <paramref name="pdfPath"/>.
        /// If the file cannot be opened or contains no extractable text,
        /// an empty string is returned.
        /// </summary>
        public static string Extract(string pdfPath)
        {
            if (!File.Exists(pdfPath))
                return string.Empty;

            var sb = new StringBuilder();

            // PdfPig automatically handles most PDF encodings.
            using var document = PdfDocument.Open(pdfPath);
            foreach (var page in document.GetPages())
            {
                var text = page.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text.Trim());
                    sb.AppendLine(); // separate pages visually
                }
            }

            return sb.ToString();
        }
    }
}
