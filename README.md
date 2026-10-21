# PathForge

Ứng dụng WPF local để ghi nhận bài toán, chia nhỏ phạm vi, thực hiện phiên làm việc và lưu báo cáo/minh chứng.

## Công nghệ

- .NET 6, SDK 6.0.428, C# 10
- WPF, EF Core 6, SQLite
- NLayer đơn giản: Domain, Application, Infrastructure, Desktop
- Không có backend/API

## Chạy từ mã nguồn

Mở `PathForge.sln` bằng Visual Studio hoặc chạy:

```powershell
dotnet run --project PathForge.Desktop\PathForge.Desktop.csproj
```

`global.json` ghim SDK 6.0.428. Ứng dụng tạo database tại `%LOCALAPPDATA%\PathForge\pathforge.db` và ảnh tại `%LOCALAPPDATA%\PathForge\Evidence`.

## Publish Windows x64

```powershell
dotnet publish PathForge.Desktop\PathForge.Desktop.csproj /p:PublishProfile=WinX64
```

Profile tạo bản self-contained dạng thư mục trong `PathForge.Desktop\bin\Release\net6.0-windows\win-x64\publish`. Dữ liệu người dùng ở LocalAppData nên không nằm trong thư mục publish.
