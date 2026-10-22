using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PathForge.Desktop.Services;

public static class KnowledgeDocumentCodec
{
    private const string FormatPrefix = "PATHFORGE-FLOWDOCUMENT-PACKAGE-1:";

    public static string Serialize(FlowDocument document)
    {
        using var stream = new MemoryStream();
        new TextRange(document.ContentStart, document.ContentEnd).Save(stream, DataFormats.XamlPackage);
        return FormatPrefix + Convert.ToBase64String(stream.ToArray());
    }

    public static FlowDocument Deserialize(string? serialized)
    {
        var document = CreateEmptyDocument();
        if (string.IsNullOrWhiteSpace(serialized))
            return document;
        if (!serialized.StartsWith(FormatPrefix, StringComparison.Ordinal))
            throw new InvalidDataException("Định dạng nội dung tài liệu không được hỗ trợ.");

        var packageBytes = Convert.FromBase64String(serialized.Substring(FormatPrefix.Length));
        using var stream = new MemoryStream(packageBytes, writable: false);
        new TextRange(document.ContentStart, document.ContentEnd).Load(stream, DataFormats.XamlPackage);
        document.PageWidth = double.NaN;
        document.PageHeight = double.NaN;
        document.PagePadding = new Thickness(0);
        document.ColumnWidth = double.PositiveInfinity;
        return document;
    }

    public static FlowDocument CreateEmptyDocument()
    {
        var document = new FlowDocument(new Paragraph())
        {
            PageWidth = double.NaN,
            PageHeight = double.NaN,
            PagePadding = new Thickness(0),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new FontFamily("Calibri"),
            FontSize = 14,
            Foreground = Brushes.Black
        };
        return document;
    }
}
