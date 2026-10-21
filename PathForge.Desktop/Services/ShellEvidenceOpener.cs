using System.Diagnostics;

namespace PathForge.Desktop.Services;

public sealed class ShellEvidenceOpener : IEvidenceOpener
{
    public void Open(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}
