using Microsoft.Win32;

namespace PathForge.Desktop.Services;

public sealed class WpfEvidenceFilePicker : IEvidenceFilePicker
{
    public string? ChooseImageFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh minh chứng",
            Filter = "Ảnh hỗ trợ|*.png;*.jpg;*.jpeg;*.webp|Tất cả tệp|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
