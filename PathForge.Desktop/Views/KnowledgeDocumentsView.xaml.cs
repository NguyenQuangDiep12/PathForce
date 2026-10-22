using Microsoft.Win32;
using PathForge.Application.KnowledgeDocuments;
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
    private double _libraryWidthBeforeCollapse = 260;
    private bool _isLibraryCollapsed;
    private bool _isFocusMode;
    private bool _focusModeInitialized;
    private bool _libraryWasExpandedBeforeFocus;
    private bool _isUpdatingToolbar;

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
        if (!_focusModeInitialized)
        {
            SetFocusMode(true);
            _focusModeInitialized = true;
        }
        Dispatcher.BeginInvoke(new Action(() => _isEditorReady = true), DispatcherPriority.ContextIdle);
    }

    private void KnowledgeDocumentsView_Unloaded(object sender, RoutedEventArgs e)
    {
        _isEditorReady = false;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _isSubscribed = false;
            _viewModel.RegisterEditorSnapshotProvider(null);
        }
    }

    private void ToggleLibrary_Click(object sender, RoutedEventArgs e)
    {
        SetLibraryCollapsed(!_isLibraryCollapsed);
    }

    private void SetLibraryCollapsed(bool collapsed)
    {
        if (_isLibraryCollapsed == collapsed)
            return;

        if (!collapsed)
        {
            LibraryColumn.MinWidth = 180;
            LibraryColumn.Width = new GridLength(Math.Max(180, _libraryWidthBeforeCollapse));
            LibrarySplitterColumn.Width = new GridLength(10);
            LibrarySplitterGrip.Visibility = Visibility.Visible;
            ToggleLibraryButton.Content = "Ẩn danh sách";
            ToggleLibraryButton.ToolTip = "Ẩn danh sách để dành thêm chỗ cho vùng soạn thảo";
        }
        else
        {
            if (LibraryColumn.ActualWidth >= LibraryColumn.MinWidth)
                _libraryWidthBeforeCollapse = LibraryColumn.ActualWidth;

            LibraryColumn.MinWidth = 0;
            LibraryColumn.Width = new GridLength(0);
            LibrarySplitterColumn.Width = new GridLength(0);
            LibrarySplitterGrip.Visibility = Visibility.Collapsed;
            ToggleLibraryButton.Content = "Hiện danh sách";
            ToggleLibraryButton.ToolTip = "Hiện lại thư viện tài liệu";
        }

        _isLibraryCollapsed = collapsed;
    }

    private void FocusMode_Click(object sender, RoutedEventArgs e)
    {
        SetFocusMode(!_isFocusMode);
    }

    private void SetFocusMode(bool enabled)
    {
        if (_isFocusMode == enabled)
            return;

        _isFocusMode = enabled;
        if (enabled)
        {
            _libraryWasExpandedBeforeFocus = !_isLibraryCollapsed;
            if (_libraryWasExpandedBeforeFocus)
                SetLibraryCollapsed(true);

            PageLayout.Margin = new Thickness(8);
            PageHeaderGrid.Visibility = Visibility.Collapsed;
            PageHeaderSpacerRow.Height = new GridLength(0);
            DocumentCard.Padding = new Thickness(8);
            DocumentTitlePanel.Visibility = Visibility.Collapsed;
            ToggleLibraryButton.Visibility = Visibility.Collapsed;
            DocumentStatusBar.Visibility = Visibility.Collapsed;
            FormattingToolbar.Visibility = Visibility.Collapsed;
            EditorViewport.Margin = new Thickness(0);
            DocumentPage.MaxWidth = double.PositiveInfinity;
            DocumentPage.Padding = new Thickness(12);
            DocumentPage.Margin = new Thickness(0);
            FocusModeButton.Content = "Hiện công cụ";
            FocusModeButton.ToolTip = "Hiện tên tài liệu và thanh công cụ soạn thảo";
            ZoomLabel.Text = "100%";
            _zoom = 1.0;
        }
        else
        {
            PageLayout.Margin = new Thickness(28, 24, 28, 24);
            PageHeaderGrid.Visibility = Visibility.Visible;
            PageHeaderSpacerRow.Height = new GridLength(14);
            DocumentCard.Padding = new Thickness(18);
            DocumentTitlePanel.Visibility = Visibility.Visible;
            ToggleLibraryButton.Visibility = Visibility.Visible;
            DocumentStatusBar.Visibility = Visibility.Visible;
            FormattingToolbar.Visibility = Visibility.Visible;
            EditorViewport.Margin = new Thickness(0, 8, 0, 0);
            DocumentPage.MaxWidth = 900;
            DocumentPage.Padding = new Thickness(30);
            DocumentPage.Margin = new Thickness(0, 4, 0, 12);
            FocusModeButton.Content = "Toàn trang";
            FocusModeButton.ToolTip = "Ẩn thanh công cụ để mở rộng vùng nhập liệu";

            DocumentPage.HorizontalAlignment = HorizontalAlignment.Center;
            DocumentPage.VerticalAlignment = VerticalAlignment.Stretch;
            _zoom = 1.0;
            ZoomLabel.Text = "100%";

            if (_libraryWasExpandedBeforeFocus && _isLibraryCollapsed)
                SetLibraryCollapsed(false);
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
        var loadedSuccessfully = false;
        try
        {
            _isLoadingDocument = true;
            DocumentEditor.Document = KnowledgeDocumentCodec.Deserialize(content);
            _isContentLoadFailed = false;
            DocumentEditor.IsReadOnly = false;
            SaveButton.IsEnabled = true;
            ResetSelectedImage();
            AttachImageInteractions(DocumentEditor.Document.Blocks);
            loadedSuccessfully = true;
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
            if (loadedSuccessfully)
                _viewModel?.ReleaseCurrentContentAfterLoad();
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

    private void KnowledgeDocumentsView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            TitleBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            _viewModel?.SaveCommand.Execute(null);
            e.Handled = true;
        }
        else if (DocumentEditor.IsKeyboardFocusWithin && Keyboard.Modifiers == ModifierKeys.Control && e.Key is Key.B or Key.I or Key.U)
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
        if (!_isUpdatingToolbar && FontFamilyBox.SelectedItem is ComboBoxItem item && item.Content is string family && IsLoaded)
            ApplyTextProperty(TextElement.FontFamilyProperty, new FontFamily(family));
    }

    private void FontSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdatingToolbar && FontSizeBox.SelectedItem is ComboBoxItem item && double.TryParse(item.Content?.ToString(), out var size) && IsLoaded)
            ApplyTextProperty(TextElement.FontSizeProperty, size);
    }

    private void TextColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdatingToolbar && TextColorBox.SelectedItem is ComboBoxItem item && item.Tag is string color && IsLoaded)
            ApplyTextProperty(TextElement.ForegroundProperty, CreateFrozenBrush((Color)ColorConverter.ConvertFromString(color)));
    }

    private void HighlightBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingToolbar || HighlightBox.SelectedItem is not ComboBoxItem item || item.Tag is not string color || !IsLoaded)
            return;
        var brush = color == "Transparent" ? Brushes.Transparent : CreateFrozenBrush((Color)ColorConverter.ConvertFromString(color));
        ApplyTextProperty(TextElement.BackgroundProperty, brush);
    }

    private void StyleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingToolbar || StyleBox.SelectedItem is not ComboBoxItem item || item.Tag is not string style || !IsLoaded)
            return;

        var (size, weight, foreground, margin, italic) = style switch
        {
            "Title" => (28d, FontWeights.Bold, Brushes.Black, new Thickness(0, 0, 0, 12), FontStyles.Normal),
            "Subtitle" => (16d, FontWeights.Normal, Brushes.Gray, new Thickness(0, 0, 0, 12), FontStyles.Italic),
            "Heading1" => (22d, FontWeights.Bold, CreateFrozenBrush(Color.FromRgb(49, 94, 82)), new Thickness(0, 18, 0, 8), FontStyles.Normal),
            "Heading2" => (18d, FontWeights.SemiBold, CreateFrozenBrush(Color.FromRgb(49, 94, 82)), new Thickness(0, 14, 0, 6), FontStyles.Normal),
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
            var (sourceWidth, sourceHeight) = await Task.Run(() => DecodeImageDimensions(sourcePath));
            string? resizedPath = null;
            StoredKnowledgeImage stored;
            try
            {
                if (sourceWidth > 1600)
                {
                    var targetPath = Path.Combine(Path.GetTempPath(), $"pathforge-{Guid.NewGuid():N}{Path.GetExtension(sourcePath)}");
                    resizedPath = targetPath;
                    await Task.Run(() => ResizeImageForStorage(sourcePath, targetPath));
                }
                stored = await _viewModel.StoreImageAsync(resizedPath ?? sourcePath);
            }
            finally
            {
                if (resizedPath is not null && File.Exists(resizedPath))
                {
                    try { File.Delete(resizedPath); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            var bitmap = await Task.Run(() => DecodeDisplayBitmap(stored.FullPath));

            var maxWidth = 620d;
            var scale = Math.Min(1d, maxWidth / sourceWidth);
            var image = new Image
            {
                Source = bitmap,
                Width = sourceWidth * scale,
                Height = sourceHeight * scale,
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

    private static (int Width, int Height) DecodeImageDimensions(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not ".png" and not ".jpg" and not ".jpeg")
            throw new InvalidDataException("Chỉ hỗ trợ PNG, JPG và JPEG.");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation | BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
        var frame = decoder.Frames.FirstOrDefault() ?? throw new InvalidDataException("Tệp ảnh không có khung hình hợp lệ.");
        if ((long)frame.PixelWidth * frame.PixelHeight > 40_000_000)
            throw new InvalidDataException("Ảnh có kích thước điểm ảnh quá lớn để chèn an toàn.");
        if ((extension == ".png" && decoder is not PngBitmapDecoder) ||
            (extension is ".jpg" or ".jpeg" && decoder is not JpegBitmapDecoder))
            throw new InvalidDataException("Phần mở rộng không khớp với định dạng ảnh.");
        return (frame.PixelWidth, frame.PixelHeight);
    }

    private static void ResizeImageForStorage(string sourcePath, string destinationPath)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var resized = new BitmapImage();
        resized.BeginInit();
        resized.CacheOption = BitmapCacheOption.OnLoad;
        resized.DecodePixelWidth = 1600;
        resized.UriSource = new Uri(sourcePath, UriKind.Absolute);
        resized.EndInit();
        resized.Freeze();

        BitmapEncoder encoder = extension == ".png"
            ? new PngBitmapEncoder()
            : new JpegBitmapEncoder { QualityLevel = 90 };
        encoder.Frames.Add(BitmapFrame.Create(resized));
        using var output = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        encoder.Save(output);
    }

    private static BitmapImage DecodeDisplayBitmap(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = 1240;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
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
            BorderBrush = CreateFrozenBrush(Color.FromRgb(197, 201, 194)),
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
        DocumentPage.LayoutTransform = new ScaleTransform(_zoom, _zoom);
        ZoomLabel.Text = $"{Math.Round(_zoom * 100)}%";
    }

    private void DocumentEditor_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoadingDocument || !IsLoaded)
            return;

        _isUpdatingToolbar = true;
        try
        {
            var selection = DocumentEditor.Selection;
            var fontFamily = selection.GetPropertyValue(TextElement.FontFamilyProperty) as FontFamily;
            FontFamilyBox.SelectedItem = fontFamily is null
                ? null
                : FontFamilyBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item =>
                    string.Equals(item.Content?.ToString(), fontFamily.Source, StringComparison.OrdinalIgnoreCase));

            var fontSize = selection.GetPropertyValue(TextElement.FontSizeProperty);
            FontSizeBox.SelectedItem = fontSize is double size
                ? FontSizeBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item =>
                    double.TryParse(item.Content?.ToString(), out var itemSize) && Math.Abs(itemSize - size) < 0.01)
                : null;

            var weight = selection.GetPropertyValue(TextElement.FontWeightProperty);
            var styleTag = fontSize is double knownSize && weight is FontWeight knownWeight
                ? MatchStyle(knownSize, knownWeight)
                : null;
            StyleBox.SelectedItem = styleTag is null
                ? null
                : StyleBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => Equals(item.Tag, styleTag));
        }
        finally
        {
            _isUpdatingToolbar = false;
        }
    }

    private static string? MatchStyle(double size, FontWeight weight) =>
        size >= 27.5 && weight == FontWeights.Bold ? "Title" :
        Math.Abs(size - 16) < 0.01 ? "Subtitle" :
        Math.Abs(size - 22) < 0.01 && weight == FontWeights.Bold ? "Heading1" :
        Math.Abs(size - 18) < 0.01 && weight == FontWeights.SemiBold ? "Heading2" :
        Math.Abs(size - 15) < 0.01 && weight == FontWeights.SemiBold ? "Heading3" :
        Math.Abs(size - 14) < 0.01 && weight == FontWeights.Normal ? "Normal" : null;

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private void FocusEditor()
    {
        DocumentEditor.Focus();
    }
}
