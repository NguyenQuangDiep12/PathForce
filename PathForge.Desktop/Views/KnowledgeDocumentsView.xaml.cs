using Microsoft.Win32;
using PathForge.Desktop.Services;
using PathForge.Desktop.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PathForge.Desktop.Views;

public partial class KnowledgeDocumentsView : UserControl
{
    private bool _isLoadingDocument;
    private bool _isEditorReady;
    private bool _isContentLoadFailed;
    private bool _isSubscribed;
    private KnowledgeDocumentsViewModel? _viewModel;
    private InlineUIContainer? _selectedImageContainer;
    private Image? _selectedImage;
    private double _zoom = 1.0;

    public KnowledgeDocumentsView()
    {
        InitializeComponent();
        DocumentEditor.Document = KnowledgeDocumentCodec.CreateEmptyDocument();
    }

    private void KnowledgeDocumentsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not KnowledgeDocumentsViewModel viewModel)
            return;
        _isEditorReady = false;

        if (!ReferenceEquals(_viewModel, viewModel))
        {
            if (_viewModel is not null)
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel = viewModel;
            _viewModel.RegisterEditorSnapshotProvider(CaptureContentForSave);
        }

        if (!_isSubscribed)
        {
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _isSubscribed = true;
        }
        LoadContent(viewModel.CurrentContent);
        Dispatcher.BeginInvoke(new Action(() => _isEditorReady = true), DispatcherPriority.ContextIdle);
    }

    private void KnowledgeDocumentsView_Unloaded(object sender, RoutedEventArgs e)
    {
        _isEditorReady = false;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _isSubscribed = false;
            _viewModel.RegisterEditorSnapshotProvider(CaptureContentForSave);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(KnowledgeDocumentsViewModel.CurrentContent) && _viewModel is not null)
            LoadContent(_viewModel.CurrentContent);
    }

    private void LoadContent(string content)
    {
        DocumentEditor.TextChanged -= DocumentEditor_TextChanged;
        try
        {
            _isLoadingDocument = true;
            DocumentEditor.Document = KnowledgeDocumentCodec.Deserialize(content);
            _isContentLoadFailed = false;
            DocumentEditor.IsReadOnly = false;
            SaveButton.IsEnabled = true;
            ResetSelectedImage();
            AttachImageInteractions(DocumentEditor.Document.Blocks);
        }
        catch (Exception exception)
        {
            DocumentEditor.Document = KnowledgeDocumentCodec.CreateEmptyDocument();
            _isContentLoadFailed = true;
            DocumentEditor.IsReadOnly = true;
            SaveButton.IsEnabled = false;
            MessageBox.Show("Không thể khôi phục nội dung hoặc hình ảnh của tài liệu. Nội dung biên tập mới chưa bị lưu đè.\n\n" + exception.Message,
                "Không đọc được tài liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _isLoadingDocument = false;
            DocumentEditor.TextChanged += DocumentEditor_TextChanged;
        }
    }

    private void DocumentEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isEditorReady && !_isLoadingDocument)
            _viewModel?.MarkEditorChanged();
    }

    private string CaptureContentForSave()
    {
        if (_isContentLoadFailed)
            throw new InvalidOperationException("Không thể lưu vì nội dung hiện tại không được khôi phục đầy đủ.");
        return KnowledgeDocumentCodec.Serialize(DocumentEditor.Document);
    }

    private void DocumentEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            _viewModel?.SaveCommand.Execute(null);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key is Key.B or Key.I or Key.U)
        {
            var command = e.Key switch
            {
                Key.B => EditingCommands.ToggleBold,
                Key.I => EditingCommands.ToggleItalic,
                _ => EditingCommands.ToggleUnderline
            };
            command.Execute(null, DocumentEditor);
            if (_isEditorReady)
                _viewModel?.MarkEditorChanged();
            e.Handled = true;
        }
    }

    private void EditorCommand_Click(object sender, RoutedEventArgs e)
    {
        if (_isEditorReady)
            _viewModel?.MarkEditorChanged();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel?.SearchCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void FontFamilyBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontFamilyBox.SelectedItem is ComboBoxItem item && item.Content is string family && IsLoaded)
            ApplyTextProperty(TextElement.FontFamilyProperty, new FontFamily(family));
    }

    private void FontSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontSizeBox.SelectedItem is ComboBoxItem item && double.TryParse(item.Content?.ToString(), out var size) && IsLoaded)
            ApplyTextProperty(TextElement.FontSizeProperty, size);
    }

    private void TextColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TextColorBox.SelectedItem is ComboBoxItem item && item.Tag is string color && IsLoaded)
            ApplyTextProperty(TextElement.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)));
    }

    private void HighlightBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HighlightBox.SelectedItem is not ComboBoxItem item || item.Tag is not string color || !IsLoaded)
            return;
        var brush = color == "Transparent" ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        ApplyTextProperty(TextElement.BackgroundProperty, brush);
    }

    private void StyleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StyleBox.SelectedItem is not ComboBoxItem item || item.Tag is not string style || !IsLoaded)
            return;

        var (size, weight, foreground, margin, italic) = style switch
        {
            "Title" => (28d, FontWeights.Bold, Brushes.Black, new Thickness(0, 0, 0, 12), FontStyles.Normal),
            "Subtitle" => (16d, FontWeights.Normal, Brushes.Gray, new Thickness(0, 0, 0, 12), FontStyles.Italic),
            "Heading1" => (22d, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(49, 94, 82)), new Thickness(0, 18, 0, 8), FontStyles.Normal),
            "Heading2" => (18d, FontWeights.SemiBold, new SolidColorBrush(Color.FromRgb(49, 94, 82)), new Thickness(0, 14, 0, 6), FontStyles.Normal),
            "Heading3" => (15d, FontWeights.SemiBold, Brushes.Black, new Thickness(0, 10, 0, 4), FontStyles.Normal),
            _ => (14d, FontWeights.Normal, Brushes.Black, new Thickness(0, 0, 0, 8), FontStyles.Normal)
        };
        DocumentEditor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
        DocumentEditor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, weight);
        DocumentEditor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, foreground);
        DocumentEditor.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, italic);
        DocumentEditor.Selection.ApplyPropertyValue(Paragraph.MarginProperty, margin);
        if (_isEditorReady)
            _viewModel?.MarkEditorChanged();
        FocusEditor();
    }

    private void ApplyTextProperty(DependencyProperty property, object value)
    {
        DocumentEditor.Selection.ApplyPropertyValue(property, value);
        if (_isEditorReady)
            _viewModel?.MarkEditorChanged();
        FocusEditor();
    }

    private void InsertImage_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog
        {
            Title = "Chọn ảnh để chèn vào tài liệu",
            Filter = "Ảnh PNG/JPG|*.png;*.jpg;*.jpeg|PNG|*.png|JPEG|*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };
        if (picker.ShowDialog(Window.GetWindow(this)) != true)
            return;
        _ = InsertImageAsync(picker.FileName);
    }

    private async Task InsertImageAsync(string sourcePath)
    {
        if (_viewModel is null)
            return;

        try
        {
            var sourceFrame = DecodeImage(sourcePath);
            var stored = await _viewModel.StoreImageAsync(sourcePath);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(stored.FullPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            var maxWidth = 620d;
            var scale = Math.Min(1d, maxWidth / sourceFrame.PixelWidth);
            var image = new Image
            {
                Source = bitmap,
                Width = sourceFrame.PixelWidth * scale,
                Height = sourceFrame.PixelHeight * scale,
                Stretch = Stretch.Uniform,
                Tag = stored.RelativePath,
                ToolTip = "Ảnh trong tài liệu · chọn ảnh để căn chỉnh, đổi cỡ hoặc xóa"
            };
            var container = new InlineUIContainer(image, DocumentEditor.CaretPosition);
            AttachImageInteraction(container, image);
            DocumentEditor.CaretPosition = container.ElementEnd;
            DocumentEditor.Focus();
            _viewModel.MarkEditorChanged();
        }
        catch (Exception exception)
        {
            MessageBox.Show("Không thể chèn ảnh. Hãy chọn tệp PNG/JPG/JPEG hợp lệ có dung lượng không quá 10 MB.\n\n" + exception.Message,
                "Không chèn được ảnh", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static BitmapFrame DecodeImage(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not ".png" and not ".jpg" and not ".jpeg")
            throw new InvalidDataException("Chỉ hỗ trợ PNG, JPG và JPEG.");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames.FirstOrDefault() ?? throw new InvalidDataException("Tệp ảnh không có khung hình hợp lệ.");
        if ((long)frame.PixelWidth * frame.PixelHeight > 40_000_000)
            throw new InvalidDataException("Ảnh có kích thước điểm ảnh quá lớn để chèn an toàn.");
        if ((extension == ".png" && decoder is not PngBitmapDecoder) ||
            (extension is ".jpg" or ".jpeg" && decoder is not JpegBitmapDecoder))
            throw new InvalidDataException("Phần mở rộng không khớp với định dạng ảnh.");
        return frame;
    }

    private void AttachImageInteractions(BlockCollection blocks)
    {
        foreach (var block in blocks)
        {
            if (block is Paragraph paragraph)
            {
                foreach (var inline in paragraph.Inlines)
                {
                    if (inline is InlineUIContainer container && container.Child is Image image)
                        AttachImageInteraction(container, image);
                }
            }
            else if (block is List list)
            {
                foreach (var item in list.ListItems)
                    AttachImageInteractions(item.Blocks);
            }
            else if (block is Section section)
            {
                AttachImageInteractions(section.Blocks);
            }
        }
    }

    private void AttachImageInteraction(InlineUIContainer container, Image image)
    {
        image.PreviewMouseLeftButtonDown += (_, _) => SelectImage(container, image);
    }

    private void SelectImage(InlineUIContainer container, Image image)
    {
        _selectedImageContainer = container;
        _selectedImage = image;
        ImageSelectionLabel.Text = "Đã chọn ảnh";
        SetImageButtonsEnabled(true);
    }

    private void ResizeImage(InlineUIContainer container, Image image, double factor)
    {
        SelectImage(container, image);
        var currentWidth = double.IsNaN(image.Width) ? image.ActualWidth : image.Width;
        var currentHeight = double.IsNaN(image.Height) ? image.ActualHeight : image.Height;
        if (currentWidth <= 0 || currentHeight <= 0)
        {
            if (image.Source is BitmapSource source)
            {
                currentWidth = source.PixelWidth;
                currentHeight = source.PixelHeight;
            }
        }
        var nextWidth = Math.Clamp(currentWidth * factor, 48, 720);
        image.Width = nextWidth;
        image.Height = currentHeight * (nextWidth / currentWidth);
        _viewModel?.MarkEditorChanged();
    }

    private void AlignImage(InlineUIContainer container, TextAlignment alignment)
    {
        if (container.Parent is Paragraph paragraph)
        {
            paragraph.TextAlignment = alignment;
            SelectImage(container, container.Child as Image ?? _selectedImage!);
            _viewModel?.MarkEditorChanged();
        }
    }

    private void DeleteImage(InlineUIContainer container, Image image)
    {
        if (container.Parent is Paragraph paragraph)
            paragraph.Inlines.Remove(container);
        if (ReferenceEquals(_selectedImage, image))
            ResetSelectedImage();
        _viewModel?.MarkEditorChanged();
        FocusEditor();
    }

    private void ImageSmaller_Click(object sender, RoutedEventArgs e) => ResizeSelectedImage(0.8);
    private void ImageLarger_Click(object sender, RoutedEventArgs e) => ResizeSelectedImage(1.2);
    private void ImageLeft_Click(object sender, RoutedEventArgs e) => AlignSelectedImage(TextAlignment.Left);
    private void ImageCenter_Click(object sender, RoutedEventArgs e) => AlignSelectedImage(TextAlignment.Center);
    private void ImageRight_Click(object sender, RoutedEventArgs e) => AlignSelectedImage(TextAlignment.Right);

    private void ImageDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedImageContainer is not null && _selectedImage is not null)
            DeleteImage(_selectedImageContainer, _selectedImage);
    }

    private void ResizeSelectedImage(double factor)
    {
        if (_selectedImageContainer is not null && _selectedImage is not null)
            ResizeImage(_selectedImageContainer, _selectedImage, factor);
    }

    private void AlignSelectedImage(TextAlignment alignment)
    {
        if (_selectedImageContainer is not null)
            AlignImage(_selectedImageContainer, alignment);
    }

    private void SetImageButtonsEnabled(bool enabled)
    {
        ImageSmallerButton.IsEnabled = enabled;
        ImageLargerButton.IsEnabled = enabled;
        ImageLeftButton.IsEnabled = enabled;
        ImageCenterButton.IsEnabled = enabled;
        ImageRightButton.IsEnabled = enabled;
        ImageDeleteButton.IsEnabled = enabled;
    }

    private void ResetSelectedImage()
    {
        _selectedImage = null;
        _selectedImageContainer = null;
        ImageSelectionLabel.Text = "Chưa chọn ảnh";
        SetImageButtonsEnabled(false);
    }

    private void InsertRule_Click(object sender, RoutedEventArgs e)
    {
        var paragraph = DocumentEditor.CaretPosition.Paragraph;
        if (paragraph is null)
            return;
        var rule = new Paragraph
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(197, 201, 194)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Margin = new Thickness(0, 10, 0, 10)
        };
        paragraph.SiblingBlocks.InsertAfter(paragraph, rule);
        DocumentEditor.CaretPosition = rule.ContentStart;
        _viewModel?.MarkEditorChanged();
        FocusEditor();
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) => SetZoom(Math.Min(1.6, _zoom + 0.1));
    private void ZoomOut_Click(object sender, RoutedEventArgs e) => SetZoom(Math.Max(0.7, _zoom - 0.1));
    private void ZoomReset_Click(object sender, RoutedEventArgs e) => SetZoom(1.0);

    private void SetZoom(double zoom)
    {
        _zoom = zoom;
        DocumentEditor.LayoutTransform = new ScaleTransform(_zoom, _zoom);
        ZoomLabel.Text = $"{Math.Round(_zoom * 100)}%";
    }

    private void FocusEditor()
    {
        DocumentEditor.Focus();
    }
}
