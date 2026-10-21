using System.Reflection;

namespace PathForge.TestHost;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Environment.SetEnvironmentVariable("PATHFORGE_DATA_DIRECTORY", @"C:\PathForce\PathForce\.e2e-data");
        var desktopAssembly = @"C:\PathForce\PathForce\PathForge.Desktop\bin\Release\net6.0-windows\win-x64\publish\PathForge.Desktop.dll";
        var application = Assembly.LoadFrom(desktopAssembly);
        application.EntryPoint!.Invoke(null, null);
    }
}