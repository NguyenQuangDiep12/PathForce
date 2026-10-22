using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PathForge.Desktop.Services;

public sealed class EvidencePreviewConverter : IValueConverter
{
    private const int MaximumCachedImages = 24;
    private readonly ConcurrentDictionary<string, BitmapImage> _cache = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        if (_cache.TryGetValue(path, out var cached))
            return cached;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 640;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            if (_cache.Count >= MaximumCachedImages)
                _cache.Clear();
            return _cache.GetOrAdd(path, bitmap);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
