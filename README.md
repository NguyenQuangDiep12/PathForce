# PathForge

PathForge là ứng dụng desktop Windows dùng để ghi nhận và theo dõi công việc theo từng bài toán. Ứng dụng chạy local với WPF và SQLite; không cần backend hay máy chủ cơ sở dữ liệu.

## Chức năng

- Dashboard theo dõi bài toán và phiên làm việc gần đây.
- Tạo, tìm kiếm, cập nhật trạng thái và xóa bài toán.
- Chia bài toán thành các đầu việc/phạm vi nhỏ, theo dõi trạng thái và liên kết với phiên làm việc.
- Bắt đầu, tạm dừng, tiếp tục, kết thúc hoặc hủy phiên làm việc; ghi báo cáo và đính kèm minh chứng.
- Xem và tìm kiếm lịch sử phiên làm việc, báo cáo.
- Soạn tài liệu kiến thức với định dạng văn bản, tiêu đề, danh sách, màu, căn lề và ảnh.

## Công nghệ

- .NET 6, C# 10, WPF
- Entity Framework Core 6 và SQLite
- SDK được ghim trong `global.json` ở phiên bản 6.0.428
- Solution gồm `PathForge.Domain`, `PathForge.Application`, `PathForge.Infrastructure`, `PathForge.Desktop` và `PathForge.Tests`

## Dữ liệu và quyền riêng tư

Database mẫu SQLite được nhúng trong `PathForge.Desktop\Resources\pathforge.db`. Đây là database khởi tạo, không chứa dữ liệu nghiệp vụ. Khi chạy lần đầu, ứng dụng sao chép database này vào thư mục dữ liệu người dùng; nếu database đã tồn tại, ứng dụng giữ nguyên và dùng database đó.

- Database đang sử dụng: `%LOCALAPPDATA%\PathForge\pathforge.db`
- Minh chứng: `%LOCALAPPDATA%\PathForge\Evidence`
- Ảnh tài liệu kiến thức: `%LOCALAPPDATA%\PathForge\KnowledgeImages`

Nếu cần chạy một bộ dữ liệu độc lập để kiểm thử hoặc dùng portable, đặt biến môi trường PATHFORGE_DATA_DIRECTORY thành thư mục dữ liệu mong muốn trước khi mở app. Khi không đặt biến này, PathForge vẫn dùng %LOCALAPPDATA%\PathForge.

Dữ liệu nằm ngoài thư mục cài đặt/publish để vẫn còn khi cập nhật ứng dụng. Để sao lưu, thoát ứng dụng rồi sao chép toàn bộ thư mục `%LOCALAPPDATA%\PathForge`. Không xóa thư mục này nếu muốn giữ dữ liệu người dùng. PathForge xử lý dữ liệu local và không kết nối dịch vụ từ xa.

## Chạy từ mã nguồn

Cần cài .NET SDK 6.0.428 trên máy phát triển. Tại thư mục gốc của repository:

```powershell
dotnet restore PathForce.sln
dotnet run --project PathForge.Desktop\PathForge.Desktop.csproj
```

Build Release:

```powershell
dotnet build PathForce.sln -c Release
```

Chạy bộ test:

```powershell
dotnet test PathForce.sln -c Release
```

## Build ứng dụng kèm .NET runtime

Các lệnh publish dưới đây tạo bản Windows x64 self-contained. Máy chạy ứng dụng không cần cài riêng .NET Desktop Runtime. Cần .NET SDK trên máy dùng để build.

### Bản thư mục self-contained

```powershell
dotnet publish PathForge.Desktop\PathForge.Desktop.csproj -c Release /p:PublishProfile=WinX64
```

Thư mục đầu ra:

```text
PathForge.Desktop\bin\Release\net6.0-windows\win-x64\publish\
```

Chép toàn bộ thư mục `publish` sang máy đích rồi chạy `PathForge.Desktop.exe`. Bản này đi kèm runtime và các thư viện phụ thuộc; không chỉ chép riêng file EXE.

### Một file EXE self-contained

Profile này đóng gói ứng dụng và .NET runtime vào file EXE chính:

```powershell
dotnet publish PathForge.Desktop\PathForge.Desktop.csproj -c Release /p:PublishProfile=WinX64SingleFile
```

File đầu ra:

```text
PathForge.Desktop\bin\Release\net6.0-windows\win-x64\publish-single\PathForge.Desktop.exe
```

Có thể chép EXE này sang máy Windows x64 để chạy mà không cần cài .NET Runtime. SQLite mẫu đã nhúng trong ứng dụng; database làm việc vẫn được tạo trong thư mục dữ liệu người dùng ở trên.

## Tài liệu kiến thức

Trình soạn thảo dùng WPF RichTextBox/FlowDocument và lưu nội dung có định dạng vào bảng `KnowledgeDocuments` trong SQLite. Bảng tài liệu độc lập, không có khóa ngoại tới các bảng nghiệp vụ. Hình ảnh PNG/JPG/JPEG tối đa 10 MB được quản lý trong thư mục `KnowledgeImages`; WebP hiện chưa được hỗ trợ. Trình soạn thảo dùng trang cuộn liên tục, chưa có phân trang/in như Microsoft Word.
