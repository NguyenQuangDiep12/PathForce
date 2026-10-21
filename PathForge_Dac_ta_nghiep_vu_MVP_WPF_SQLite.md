# PathForge -- Đặc tả nghiệp vụ và yêu cầu MVP

**Phiên bản:** 1.0\
**Nền tảng:** Windows Desktop, WPF, C#, .NET 6, SQLite local\
**Đối tượng sử dụng:** Một người dùng trên một máy\
**Mục đích tài liệu:** Là nguồn yêu cầu chính để AI coding agent xây
dựng MVP. Khi có điểm chưa quy định, agent phải giữ giải pháp đơn giản,
không tự thêm tính năng hoặc mở rộng phạm vi.

------------------------------------------------------------------------

## 1. Mục tiêu sản phẩm

PathForge giúp người dùng giải quyết một bài toán học tập hoặc kỹ thuật
theo một vòng lặp rõ ràng:

1.  Phân tích vấn đề bằng 5W-1H, sau đó xác định mục tiêu và kết quả mong muốn.
2.  Chia bài toán thành các vấn đề nhỏ.
3.  Chọn một vấn đề quan trọng để xử lý ở thời điểm hiện tại.
4.  Thực hiện công việc và ghi nhận kết quả thực tế.
5.  Đánh giá phần đã giải quyết, phần còn vướng và bước tiếp theo.
6.  Tiếp tục từ trạng thái gần nhất hoặc xác nhận bài toán đã hoàn
    thành.

Sản phẩm không nhằm thay người dùng nghiên cứu, lập luận hoặc quyết
định. Nó cung cấp cấu trúc để người dùng không lan man, không nhầm lẫn
giữa hoạt động và kết quả, và có thể tiếp tục công việc mà không phải
dựng lại toàn bộ bối cảnh.

### 1.1 Nguyên tắc nghiệp vụ cốt lõi

-   Một bài toán phải hướng đến một kết quả có thể diễn đạt rõ ràng.
-   Phân tích 5W-1H giúp làm rõ vấn đề; người dùng có thể bổ sung dần khi có thông tin mới.
-   Người dùng chủ động xác định mục tiêu, phạm vi và nhiệm vụ; PathForge không tự sinh nhiệm vụ từ phần phân tích.
-   Hoạt động đã làm không đồng nghĩa với kết quả đã đạt.
-   Mỗi phiên thực hiện có một nhiệm vụ chính.
-   Chủ đề phát sinh được xử lý ngay, để sau hoặc loại khỏi phạm vi;
    không mặc định nghiên cứu tất cả.
-   Khi bị mắc kẹt, người dùng phải xác định trở ngại và chọn hành động
    tiếp theo, không chỉ ghi "chưa xong".
-   Chỉ người dùng xác nhận hoàn thành bài toán sau khi đối chiếu mục
    tiêu và tiêu chí thành công.
-   Lịch sử phải giữ được ngữ cảnh cần thiết để tiếp tục công việc.

## 2. Phạm vi MVP

### 2.1 Bao gồm

1.  Tổng quan: hiển thị bài toán đang làm và hành động tiếp theo.
2.  Bài toán: tạo, xem, chỉnh sửa, tìm kiếm, thay đổi trạng thái.
3.  Phân rã phạm vi: ghi các vấn đề/chủ đề nhỏ, xác định cần làm ngay,
    để sau, hoàn thành hoặc loại bỏ.
4.  Thực hiện: bắt đầu một phiên làm việc gắn với một bài toán và một
    nhiệm vụ chính.
5.  Báo cáo phiên: ghi việc đã làm, kết quả, điều đã học, trở ngại và
    bước tiếp theo.
6.  Minh chứng: đính kèm ảnh tùy chọn vào bài toán hoặc phiên làm việc.
7.  Lịch sử: tìm kiếm và xem lại bài toán, phiên thực hiện, báo cáo và
    minh chứng.
8.  Đóng gói chạy local trên Windows, dữ liệu giữ lại giữa các lần mở
    ứng dụng.

### 2.2 Không thuộc MVP

-   Backend, Web API, tài khoản, đăng nhập, phân quyền nhiều người dùng.
-   Đồng bộ đám mây, truy cập từ nhiều thiết bị, chế độ offline-sync.
-   AI tự nghiên cứu, tự tạo kế hoạch, tự đánh giá mức độ hiểu hoặc tự
    xác nhận hoàn thành.
-   Chặn website/ứng dụng, giám sát hoạt động máy tính.
-   Điểm số, streak, xếp hạng, chấm điểm kỷ luật.
-   Dashboard phân tích nâng cao, biểu đồ năng suất hoặc phần trăm tiến
    độ không có cách tính đáng tin cậy.
-   Knowledge graph, vector search, kho tri thức riêng, hệ thống quản lý
    dự án phức tạp.
-   Bắt buộc đính kèm ảnh cho mọi phiên.
-   Hệ thống plugin, microservices hoặc các tầng kiến trúc không cần
    thiết.

## 3. Khái niệm nghiệp vụ

-   **Bài toán (Problem):** mục tiêu lớn mà người dùng muốn giải quyết.
-   **Phân tích 5W-1H:** sáu nội dung What, Why, Who, Where, When và How dùng để làm rõ vấn đề và bối cảnh; đây là dữ liệu tùy chọn, có thể cập nhật dần.
-   **Tiêu chí thành công (Success Criteria):** điều kiện để người dùng
    kiểm tra xem mục tiêu đã đạt hay chưa.
-   **Vấn đề nhỏ / mục phạm vi (Scope Item):** một câu hỏi, phần kiến
    thức, trở ngại hoặc đầu việc liên quan đến bài toán.
-   **Phiên thực hiện (Work Session):** một khoảng làm việc tập trung
    vào một nhiệm vụ chính.
-   **Báo cáo phiên (Session Report):** ghi nhận kết quả thực tế sau
    phiên.
-   **Minh chứng (Evidence):** ảnh hoặc tệp hình ảnh hỗ trợ việc xem lại
    kết quả. Minh chứng không tự chứng minh toàn bộ bài toán đã hoàn
    thành.
-   **Để sau (Backlog):** chủ đề có liên quan nhưng chưa cần xử lý trong
    phạm vi hiện tại.

## 4. Vai trò và điều hướng

MVP chỉ có một người dùng local, không có vai trò quản trị hay đăng
nhập.

Thanh điều hướng chính gồm: - **Tổng quan** - **Bài toán** - **Thực
hiện** - **Lịch sử**

Không tạo thêm khu vực riêng cho phân tích, kho tri thức, quản lý dự án
hoặc cài đặt phức tạp. Các thao tác liên quan nên nằm ngay trong màn
hình nghiệp vụ tương ứng.

## 5. Quy trình nghiệp vụ chính

### 5.1 Bắt đầu một bài toán mới

1.  Người dùng chọn tạo bài toán.
2.  Nhập tên bài toán và mục tiêu.
3.  Có thể nhập thêm phân tích 5W-1H, mô tả, tiêu chí thành công, hành động tiếp theo và
    các vấn đề nhỏ.
4.  Lưu bài toán với trạng thái `NotStarted`.
5.  Bài toán xuất hiện trong danh sách và Tổng quan nếu được chọn làm
    bài toán hiện tại.

Tên và mục tiêu là bắt buộc. Các trường khác có thể bổ sung sau. Ứng
dụng không được bắt người dùng hoàn thiện một biểu mẫu dài trước khi có
thể bắt đầu.

### 5.2 Làm rõ bài toán bằng 5W-1H

Người dùng có thể phân tích vấn đề trước khi xác định cách giải quyết. Sáu nội dung gồm:

- **What:** vấn đề cụ thể hoặc kết quả cần đạt.
- **Why:** lý do cần giải quyết vấn đề.
- **Who:** người, nhóm hoặc đối tượng liên quan.
- **Where:** bối cảnh, môi trường hoặc thành phần nơi vấn đề xảy ra.
- **When:** thời điểm, điều kiện xảy ra hoặc hạn chót nếu có.
- **How:** hướng tiếp cận, giải quyết hoặc kiểm chứng ban đầu.

Không bắt buộc điền đủ sáu nội dung để lưu bài toán. Người dùng có thể tạo bài toán bằng tên và mục tiêu trước, rồi bổ sung hoặc chỉnh sửa phân tích, mục tiêu và phạm vi khi có thông tin mới. `How` chỉ là hướng tiếp cận hoặc giả thuyết ban đầu, không phải kế hoạch cố định.

Luồng tham chiếu là **Phân tích 5W-1H → Xác định mục tiêu và tiêu chí thành công → Xác định phạm vi → Chọn nhiệm vụ → Thực hiện và đánh giá**. Đây là cấu trúc hỗ trợ suy nghĩ, không phải biểu mẫu bắt buộc phải hoàn tất trước khi lưu. Phân tích không tự tạo nhiệm vụ hoặc tự mở rộng phạm vi.

### 5.3 Phân rã bài toán

Người dùng tạo các Scope Item với tiêu đề và lý do/ngữ cảnh tùy chọn. Mỗi mục có một trong các trạng thái: `InScope`, `Backlog`, `Done` hoặc `Excluded`.

Quy tắc:

1. Một mục nhỏ nên mô tả câu hỏi, vấn đề hoặc kết quả nhỏ cần đạt, không chỉ là tên chủ đề quá rộng. Khi bắt đầu nghiên cứu, người dùng có thể ghi câu hỏi cần trả lời hoặc giả thuyết cần kiểm chứng; lý do và cách kiểm chứng có thể ghi ngắn gọn, không bắt buộc điền biểu mẫu dài.
2. `InScope` dành cho nội dung liên quan trực tiếp đến mục tiêu hiện tại hoặc cần xử lý để tiếp tục, ví dụ nội dung đang chặn tiến độ, giả định quan trọng cần xác minh, lỗi/mâu thuẫn làm hướng hiện tại không còn phù hợp, hoặc thông tin mới làm thay đổi đáng kể cách hiểu vấn đề.
3. Chủ đề đáng tìm hiểu nhưng chưa ảnh hưởng đến nhiệm vụ hiện tại có thể được ghi vào `Backlog`; nội dung không phục vụ mục tiêu hiện tại có thể được đánh dấu `Excluded`.
4. Khi đang thực hiện, người dùng có thể ghi nhanh Scope Item ngay trên màn hình Thực hiện: chỉ tiêu đề là bắt buộc, lý do tùy chọn và trạng thái do người dùng chọn. Sau khi lưu, ứng dụng quay lại phiên đang làm với nhiệm vụ hiện tại được giữ nguyên.
5. Người dùng tự chọn trạng thái. Hệ thống không tự phân loại, không tự chuyển trạng thái, không chặn cứng việc đổi hướng và không yêu cầu xác nhận lặp lại.
6. Chuyển một mục sang `Done` không làm bài toán cha tự động hoàn thành. Kết thúc phiên cũng không tự đánh dấu Scope Item là `Done`; người dùng xác nhận trạng thái riêng.
7. Người dùng được sửa tiêu đề, lý do và trạng thái khi có thông tin mới. Không tự động tạo nhiệm vụ hoặc mở rộng phạm vi.
8. Không bắt buộc tạo Scope Item nếu bài toán đơn giản và có thể làm trực tiếp.

### 5.4 Chọn việc cần làm

Trước khi bắt đầu phiên, người dùng chọn bài toán, một nhiệm vụ chính (có thể chọn từ Scope Item hoặc nhập tiêu đề riêng) và thời lượng dự kiến tùy chọn.

Tên nhiệm vụ nên mô tả một hành động có thể thực hiện hoặc một câu hỏi có thể kiểm chứng, thay vì chỉ là chủ đề rộng. Ví dụ: “Kiểm tra API có từ chối request không có token hay không” cụ thể hơn “Học JWT”. Người dùng có thể điều chỉnh nhiệm vụ trước hoặc trong phiên nếu giả định ban đầu sai hoặc có thông tin mới.

Mỗi phiên chỉ có một nhiệm vụ chính. Các ý tưởng khác phát sinh trong phiên có thể được ghi vào Scope Item với trạng thái do người dùng chọn, nhưng không tự thay thế nhiệm vụ đang thực hiện. Nếu chưa có nhiệm vụ cụ thể, giao diện hướng người dùng mô tả việc tiếp theo bằng một hành động có thể thực hiện được; không bắt buộc lập kế hoạch nhiều bước.

### 5.5 Thực hiện phiên

Người dùng có thể bắt đầu phiên với hoặc không có bộ đếm thời gian. Thời lượng gợi ý: 15, 25, 45 hoặc 60 phút; mặc định 25 phút nhưng có thể thay đổi hoặc bỏ qua.

Trong phiên, người dùng có thể xem nhiệm vụ và mục tiêu bài toán hiện tại, tạm dừng/tiếp tục, điều chỉnh nhiệm vụ khi cần, kết thúc để ghi báo cáo, hoặc ghi nhanh chủ đề phát sinh vào Scope Item. Thao tác ghi chủ đề chỉ yêu cầu tiêu đề, lý do là tùy chọn; người dùng chọn `InScope`, `Backlog`, `Done` hoặc `Excluded`, rồi quay lại nhiệm vụ đang thực hiện.

Timer chỉ là công cụ hỗ trợ. Hết thời gian không tự động đánh dấu nhiệm vụ hoặc bài toán hoàn thành, cũng không tự động hủy phiên.

### 5.6 Báo cáo sau phiên

Khi kết thúc phiên, mở biểu mẫu báo cáo gồm: **Đã làm gì?**, **Kết quả đạt được**, **Điều đã học / điều đã hiểu**, **Trở ngại**, **Bước tiếp theo**.

Quy tắc:

1. Ít nhất một trong hai trường `Result` (Kết quả đạt được) hoặc `Learned` (Điều đã học) phải có nội dung trước khi hoàn tất báo cáo.
2. Các trường còn lại không bắt buộc. Khuyến khích ghi `NextStep` để xác định hành động tiếp theo có thể bắt đầu ngay.
3. Kết quả có thể là thành công, lỗi được phát hiện, giả thuyết bị bác bỏ, hướng không phù hợp hoặc nội dung còn chưa xác định. Kết quả khám phá vẫn có giá trị nếu được ghi nhận rõ ràng.
4. Có thể ghi kết luận, kết quả thử nghiệm hoặc điều chưa biết vào `Result`/`Learned`; minh chứng hình ảnh là tùy chọn, không bắt buộc cho mỗi phiên.
5. Nếu có trở ngại, khuyến khích mô tả điều đang cản trở và những gì đã thử; không yêu cầu điền tất cả trường.
6. Nếu có bước tiếp theo, đưa ra gợi ý trên Tổng quan. Không tự tạo Scope Item hoặc nhiệm vụ mới nếu người dùng chưa xác nhận.
7. Hoàn tất một phiên không tự động hoàn tất bài toán hoặc Scope Item. Bài toán chưa giải quyết vẫn giữ trạng thái phù hợp; kết thúc hoặc hủy phiên không tự chuyển bài toán sang `Blocked` hay `Resolved`.
8. Báo cáo đã lưu có thể được xem lại và chỉnh sửa. Khi lưu báo cáo thất bại, giữ nguyên nội dung người dùng đã nhập để thử lại.

### 5.7 Đánh giá và tiếp tục bài toán

Sau khi xem báo cáo, người dùng quyết định bước tiếp theo: - Tiếp tục
nhiệm vụ hiện tại. - Chọn một Scope Item khác. - Chuyển một chủ đề phát
sinh sang Backlog. - Ghi nhận trở ngại và đổi cách tiếp cận. - Điều
chỉnh phân tích 5W-1H, phạm vi, mục tiêu hoặc tiêu chí thành công. - Xác nhận bài toán đã hoàn
thành.

Nếu chưa đạt mục tiêu, bài toán vẫn ở trạng thái đang thực hiện hoặc bị
chặn. Không đánh dấu hoàn thành chỉ vì đã dùng hết thời gian, đã đọc
xong tài liệu hoặc đã hoàn tất tất cả mục nhỏ.

## 6. Quy tắc trạng thái

### 6.1 Trạng thái Problem

-   `NotStarted`: đã tạo nhưng chưa bắt đầu làm.
-   `InProgress`: đang được xử lý.
-   `Blocked`: hiện bị cản trở và chưa thể tiến triển bình thường.
-   `Resolved`: người dùng xác nhận mục tiêu đã đạt theo tiêu chí thành
    công.

Chuyển trạng thái hợp lệ: - `NotStarted` → `InProgress` - `NotStarted` →
`Blocked` - `InProgress` → `Blocked` - `Blocked` → `InProgress` -
`InProgress` → `Resolved` - `Blocked` → `Resolved`, nếu người dùng xác
nhận mục tiêu đã đạt - `Resolved` → `InProgress`, khi người dùng chủ
động mở lại bài toán

Quy tắc bổ sung: 1. Chỉ người dùng mới xác nhận `Resolved`. 2. Nếu có
tiêu chí thành công, yêu cầu người dùng xác nhận đã kiểm tra các tiêu
chí đó. 3. Nếu chưa có tiêu chí thành công, hỏi xác nhận rõ ràng rằng
mục tiêu đã đạt; không tự suy đoán. 4. Nếu mở lại bài toán, giữ nguyên
lịch sử phiên và báo cáo trước đó. 5. Phiên bị bỏ dở không tự động
chuyển bài toán sang `Blocked`. 6. Chuyển một Scope Item sang `Done`
không tự đổi trạng thái Problem. 7. Không cần tính phần trăm tiến độ
trong MVP.

### 6.2 Trạng thái Scope Item

-   `InScope`
-   `Backlog`
-   `Done`
-   `Excluded`

Người dùng có thể đổi trạng thái trực tiếp. Chuyển từ `Done` về
`InScope` được phép nếu phát hiện còn việc cần xử lý. Trạng thái của
Scope Item không tự quyết định trạng thái Problem.

### 6.3 Trạng thái Work Session

-   `Running`: phiên đang chạy.
-   `Paused`: người dùng tạm dừng.
-   `AwaitingReport`: phiên đã kết thúc phần làm việc, đang chờ hoàn tất
    báo cáo.
-   `Completed`: báo cáo đã được lưu hợp lệ.
-   `Abandoned`: người dùng chủ động bỏ phiên mà không hoàn tất báo cáo.

Chuyển trạng thái: - Tạo phiên → `Running` - `Running` → `Paused` -
`Paused` → `Running` - `Running` → `AwaitingReport` khi người dùng kết
thúc phần làm việc - `Paused` → `AwaitingReport` khi người dùng kết thúc
phiên - `AwaitingReport` → `Completed` khi báo cáo hợp lệ được lưu -
`Running` hoặc `Paused` → `Abandoned` khi người dùng xác nhận bỏ phiên

Không cho phép tiếp tục phiên `Completed` hoặc `Abandoned`. Nếu muốn làm
tiếp, tạo phiên mới. Không cho phép có nhiều hơn một phiên `Running`
cùng lúc trong ứng dụng. Trước khi bắt đầu phiên mới, người dùng phải
tạm dừng, kết thúc hoặc bỏ phiên đang chạy.

Nếu ứng dụng đóng khi có phiên `Running` hoặc `Paused`, lần mở sau phải
tải lại trạng thái đã lưu và tính thời lượng từ timestamp đã lưu; không
được dựa hoàn toàn vào biến đếm trong bộ nhớ. Không tự đánh dấu
`Completed` hoặc `Abandoned` khi ứng dụng đóng đột ngột.

## 7. Quy tắc kết thúc bài toán

1.  Người dùng phải thực hiện thao tác xác nhận hoàn thành có chủ đích.
2.  Nếu có tiêu chí thành công, hiển thị chúng để người dùng đối chiếu
    trước khi xác nhận.
3.  Nếu tiêu chí chưa đạt, người dùng có thể quay lại mà không cần thay
    đổi trạng thái.
4.  Nếu không có tiêu chí thành công, yêu cầu người dùng xác nhận mục
    tiêu đã đạt bằng một bước xác nhận rõ ràng.
5.  Minh chứng là tùy chọn mặc định. Chỉ khi người dùng chủ động đánh
    dấu rằng bài toán yêu cầu minh chứng thì mới bắt buộc có ít nhất một
    ảnh trước khi hoàn thành.
6.  Sau khi hoàn thành, bài toán vẫn xuất hiện trong lịch sử và có thể
    mở lại.
7.  Không tự đánh giá chất lượng câu trả lời, mức độ hiểu hoặc độ chính
    xác bằng AI.

## 8. Minh chứng hình ảnh

1.  Người dùng có thể đính kèm ảnh cho bài toán hoặc một phiên làm việc.
2.  Ảnh là tùy chọn; không bắt buộc cho mọi phiên.
3.  MVP hỗ trợ PNG, JPG/JPEG và WebP.
4.  Giới hạn kích thước ban đầu: tối đa 5 MB mỗi ảnh.
5.  Ảnh được sao chép vào thư mục dữ liệu riêng của PathForge; SQLite
    chỉ lưu metadata và đường dẫn tương đối.
6.  Metadata gồm: Id, ProblemId, WorkLogId tùy chọn, tên hiển thị, đường
    dẫn tương đối, mô tả tùy chọn và thời điểm tạo.
7.  Người dùng có thể xem ảnh, mở ảnh và xóa ảnh.
8.  Trước khi xóa ảnh, yêu cầu xác nhận.
9.  Không chấp nhận đường dẫn ngoài thư mục dữ liệu làm nơi lưu lâu dài;
    khi thêm ảnh, ứng dụng sao chép ảnh vào thư mục Evidence của ứng
    dụng.
10. Nếu sao chép ảnh thất bại, báo lỗi và không tạo bản ghi Evidence mồ
    côi trong database.
11. Không tự động suy ra rằng ảnh chứng minh bài toán đã hoàn thành.

## 9. Tìm kiếm và lịch sử

### 9.1 Tìm kiếm

Người dùng có thể tìm kiếm theo từ khóa trong: - Tên và mục tiêu bài
toán. - Tiêu đề Scope Item. - Tên nhiệm vụ của phiên. - Nội dung báo
cáo: kết quả, điều đã học, trở ngại và bước tiếp theo. - Mô tả minh
chứng.

Yêu cầu: 1. Tìm kiếm không phân biệt chữ hoa/chữ thường. 2. Tìm kiếm
tiếng Việt có dấu được hỗ trợ. Không bắt buộc hỗ trợ tìm kiếm không dấu
trong MVP. 3. Kết quả phải cho biết nó thuộc bài toán, mục nhỏ, phiên
hay minh chứng. 4. Bài toán được sắp xếp theo lần cập nhật gần nhất;
lịch sử phiên theo thời điểm bắt đầu gần nhất. 5. Có trạng thái rỗng rõ
ràng khi không có kết quả. 6. Chọn kết quả sẽ mở đúng chi tiết liên
quan. 7. Không cần AI search, vector search hoặc bộ lọc phức tạp.

### 9.2 Tiếp tục công việc cũ

Từ Tổng quan hoặc Lịch sử, người dùng có thể mở bài toán chưa hoàn thành. Thông tin cần truy cập thuận tiện trên màn hình bài toán gồm mục tiêu và tiêu chí thành công; phân tích 5W-1H đã ghi; trạng thái hiện tại; Scope Item đang xử lý và các mục `Backlog`; báo cáo phiên gần nhất; trở ngại chưa giải quyết; và bước tiếp theo đã ghi nhận.

Người dùng không cần đọc lại toàn bộ lịch sử trước khi tiếp tục. Từ báo cáo hoặc mục phạm vi, có thể điều hướng đến bài toán liên quan và Scope Item của phiên nếu có. Ứng dụng không tự tạo bước tiếp theo; nếu chưa có, hiển thị lời nhắc ngắn để người dùng tự chọn.

## 10. Đặc tả màn hình

### 10.1 Tổng quan

Mục tiêu: giúp người dùng bắt đầu hoặc tiếp tục nhanh chóng.

Nếu chưa có bài toán: hiển thị giới thiệu ngắn và nút chính `Tạo bài toán`.

Nếu có bài toán đang thực hiện, ưu tiên hiển thị tên, mục tiêu, trạng thái và `NextStep` mới nhất đã lưu trong báo cáo phiên. Nếu báo cáo gần nhất chưa có `NextStep`, hiển thị `Problem.NextAction`; nếu cả hai đều trống, hiển thị lời nhắc để người dùng tự chọn việc tiếp theo, không tự tạo nhiệm vụ. Hiển thị phiên đang chạy hoặc tạm dừng để người dùng quay lại, cùng kết quả ngắn của báo cáo gần nhất nếu có. Hành động chính là `Tiếp tục thực hiện`; hành động phụ gồm xem bài toán hoặc chọn bài toán khác.

Không hiển thị biểu đồ, điểm năng suất, streak hoặc phần trăm tiến độ.

### 10.2 Danh sách Bài toán

-   Hiển thị tên, mục tiêu rút gọn, trạng thái và thời điểm cập nhật.
-   Có tìm kiếm từ khóa.
-   Có lọc đơn giản theo trạng thái nếu cần.
-   Cho phép tạo, mở và chỉnh sửa bài toán.
-   Bài toán `Resolved` vẫn có thể xem và mở lại.
-   Xóa bài toán phải có xác nhận và nêu rõ dữ liệu con sẽ bị xóa.

### 10.3 Chi tiết Bài toán

Các phần: thông tin bài toán (tên, mục tiêu, mô tả, tiêu chí thành công, trạng thái); phân tích 5W-1H; bước tiếp theo; Scope Item theo trạng thái; lịch sử phiên gần đây và báo cáo gần nhất; minh chứng liên quan.

Người dùng có thể chỉnh sửa phân tích, mục tiêu, tiêu chí và phạm vi bất cứ lúc nào khi có thông tin mới. Sáu trường 5W-1H là tùy chọn và không cần hoàn tất cùng lúc.

Hành động chính: chỉnh sửa thông tin; thêm Scope Item; bắt đầu phiên thực hiện; đánh dấu bị chặn / quay lại thực hiện; xác nhận hoàn thành / mở lại. Không bắt buộc điền mọi trường tùy chọn.

### 10.4 Thực hiện

- Chọn bài toán và một nhiệm vụ chính; hiển thị rõ nhiệm vụ cùng mục tiêu bài toán trong phiên.
- Chọn thời lượng hoặc bỏ qua timer; timer không phải điều kiện để hoàn thành.
- Có các hành động tạm dừng, tiếp tục, kết thúc phiên và bỏ phiên.
- Hỗ trợ ghi nhanh chủ đề mới thành Scope Item ngay trong phiên: tiêu đề bắt buộc, lý do tùy chọn, người dùng tự chọn trạng thái. Lưu xong quay lại phiên và giữ nguyên nhiệm vụ hiện tại.
- Không hiển thị danh sách dài các việc không liên quan trong lúc tập trung.

Khi người dùng chọn `Kết thúc phiên`, chuyển sang màn hình báo cáo, không tự hoàn tất bài toán hoặc Scope Item.

### 10.5 Báo cáo phiên

-   Có năm trường: đã làm gì, kết quả, điều đã học, trở ngại, bước tiếp
    theo.
-   Hiển thị validation nếu cả kết quả và điều đã học đều trống.
-   Cho phép thêm ảnh minh chứng.
-   Nút chính: `Lưu báo cáo`.
-   Nút phụ: `Tiếp tục chỉnh sửa` nếu phù hợp với trạng thái hiện tại.
-   Nội dung không được mất nếu lưu thất bại.

### 10.6 Lịch sử

-   Tìm kiếm toàn bộ nội dung được hỗ trợ.
-   Hiển thị các bài toán và phiên theo thứ tự gần nhất.
-   Khi mở một phiên, hiển thị nhiệm vụ, thời lượng, báo cáo và ảnh; cho phép xem kết quả, trở ngại, bước tiếp theo và điều hướng đến bài toán hoặc Scope Item liên quan nếu có.
-   Cho phép mở bài toán cha để tiếp tục.
-   Không xây timeline phức tạp hoặc analytics trong MVP.

## 11. Quy tắc dữ liệu và xóa

### 11.1 Bài toán

Các trường: `Id`; `Title` (bắt buộc); `Goal` (bắt buộc); `Description` (tùy chọn); `AnalysisWhat`, `AnalysisWhy`, `AnalysisWho`, `AnalysisWhere`, `AnalysisWhen`, `AnalysisHow` (đều tùy chọn, tương ứng sáu câu hỏi 5W-1H); `SuccessCriteria` (tùy chọn); `NextAction` (tùy chọn); `Status`; `RequiresEvidence` (mặc định false); `CreatedAt`; `UpdatedAt`.

Sáu trường phân tích lưu trực tiếp trên Problem để người dùng có thể bổ sung dần. Thêm các cột nullable qua migration, giữ nguyên dữ liệu bài toán hiện có; không tạo bảng hoặc module phân tích riêng. Khi chỉnh sửa, cập nhật `UpdatedAt`.

### 11.2 Scope Item

Các trường: - `Id` - `ProblemId` - `Title` (bắt buộc) - `Reason` (tùy
chọn) - `Status` - `CreatedAt` - `UpdatedAt`

Scope Item thuộc đúng một Problem.

### 11.3 WorkLog

Các trường: `Id`; `ProblemId`; `ScopeItemId` (nullable); `TaskTitle` (bắt buộc); `PlannedDurationMinutes` (nullable); `StartedAt`; `EndedAt` (nullable); `Status`; `WhatWasDone` (tùy chọn); `Result` (tùy chọn); `Learned` (tùy chọn); `Blocker` (tùy chọn); `NextStep` (tùy chọn); `CreatedAt`; `UpdatedAt`.

Một WorkLog thuộc đúng một Problem. `ScopeItemId` liên kết phiên với mục phạm vi được chọn để lịch sử có thể điều hướng đến mục đó; để null khi phiên dùng nhiệm vụ tự nhập. Nếu có liên kết, Scope Item phải thuộc cùng Problem. Trường mới nullable để giữ nguyên và tương thích các WorkLog đã có.

### 11.4 Evidence

Các trường: - `Id` - `ProblemId` - `WorkLogId` (nullable) -
`DisplayName` - `RelativePath` - `Description` (tùy chọn) - `CreatedAt`

Evidence thuộc một Problem và có thể gắn thêm với một WorkLog của chính
Problem đó. Không được gắn một WorkLog thuộc bài toán khác.

### 11.5 Xóa dữ liệu

-   Xóa Problem cần xác nhận rõ rằng Scope Item, WorkLog và Evidence
    liên quan cũng sẽ bị xóa.
-   Xóa theo một thao tác nghiệp vụ thống nhất, tránh để lại bản ghi con
    không còn bài toán cha.
-   Khi xóa Evidence, xóa bản ghi database và file tương ứng. Nếu xóa
    file thất bại, báo rõ và ghi nhận để xử lý lại; không hiển thị thông
    báo rằng mọi thứ đã xóa thành công.
-   Không có chức năng xóa hàng loạt trong MVP.
-   Khi xóa Scope Item đang được WorkLog tham chiếu, đặt WorkLog.ScopeItemId thành null và giữ nguyên tiêu đề nhiệm vụ cùng báo cáo để không làm mất lịch sử phiên.
-   Ứng dụng nên cung cấp thao tác sao lưu database hoặc hướng dẫn rõ
    nơi lưu dữ liệu để người dùng tự sao lưu. Không tự động xóa dữ liệu
    cũ.

## 12. Quy tắc lưu và phục hồi trải nghiệm

Đây là quy tắc phục vụ tính đúng đắn của nghiệp vụ, không phải yêu cầu
tối ưu hệ thống.

-   Các thay đổi biểu mẫu chỉ được thông báo đã lưu sau khi database xác
    nhận thành công.
-   Với biểu mẫu chỉnh sửa, dùng nút Lưu rõ ràng; nếu người dùng rời màn
    hình khi có thay đổi chưa lưu, hỏi có muốn lưu, bỏ thay đổi hoặc
    quay lại.
-   Khi lưu thất bại, giữ nội dung đang nhập và thông báo lỗi dễ hiểu.
-   Khi hoàn tất báo cáo, chỉ chuyển phiên sang `Completed` sau khi nội
    dung báo cáo đã được lưu thành công.
-   Nếu ứng dụng khởi động lại khi có phiên đang chạy hoặc tạm dừng,
    khôi phục phiên từ dữ liệu đã lưu.
-   Nếu người dùng đóng ứng dụng khi phiên đang chạy, lần sau cho phép
    tiếp tục hoặc kết thúc phiên; không tự suy diễn rằng phiên đã hoàn
    tất.
-   Những thao tác quan trọng như xóa bài toán, bỏ phiên hoặc đánh dấu
    hoàn thành phải yêu cầu xác nhận rõ ràng.

## 13. Mô hình dữ liệu đề xuất

MVP dùng bốn bảng nghiệp vụ chính: - `Problems` - `ScopeItems` -
`WorkLogs` - `Evidence`

Không cần bảng `Users` vì ứng dụng chỉ phục vụ một người dùng local và
không có đăng nhập.

Quan hệ: - Problem 1 --- N ScopeItems - Problem 1 --- N WorkLogs -
Problem 1 --- N Evidence - ScopeItem 1 --- N WorkLogs (WorkLog có thể không gắn ScopeItem) - WorkLog 1 --- N Evidence (một Evidence có thể
không gắn WorkLog)

WorkLog luôn thuộc một Problem; nếu có ScopeItemId, lớp Application kiểm tra Scope Item thuộc cùng Problem. Xóa Scope Item đặt liên kết tùy chọn thành null, giữ nguyên WorkLog.

Dùng khóa chính đơn giản, khóa ngoại và quy tắc toàn vẹn phù hợp trong
SQLite. Ngày giờ được lưu theo một định dạng thống nhất, khuyến nghị UTC
trong database và hiển thị theo giờ local.

## 14. Yêu cầu UI/UX

-   Giao diện sáng hoặc trung tính, dễ đọc, ít màu; màu sắc chỉ dùng để
    phân biệt trạng thái khi thực sự cần.
-   Thanh điều hướng trái: Tổng quan, Bài toán, Thực hiện, Lịch sử.
-   Mỗi màn hình có một hành động chính rõ ràng.
-   Tránh biểu mẫu dài, thuật ngữ trừu tượng và nhiều hộp thoại liên
    tiếp.
-   Dùng tiếng Việt nhất quán trong giao diện.
-   Trạng thái phải dễ hiểu: Chưa bắt đầu, Đang thực hiện, Bị chặn, Đã
    hoàn thành; Chờ báo cáo, Đã lưu, Đã bỏ.
-   Thông báo lỗi phải nói rõ điều gì chưa được thực hiện và cách tiếp
    tục.
-   Không dùng màu sắc, animation hoặc hiệu ứng làm giảm khả năng đọc.
-   Không đặt nghiệp vụ quan trọng hoàn toàn trong code-behind; UI gọi
    ViewModel/application service.

## 15. Công nghệ và kiến trúc triển khai được yêu cầu

### 15.0 Công nghệ sử dụng

| Hạng mục | Lựa chọn |
|---|---|
| Framework | .NET 6 |
| SDK | 6.0.428 |
| Giao diện | WPF |
| Ngôn ngữ | C# (C# 10, mặc định của .NET 6) |
| Cơ sở dữ liệu | SQLite |
| ORM | Entity Framework Core 6 (`Microsoft.EntityFrameworkCore.Sqlite` 6.0.x) |
| Kiến trúc | NLayer + MVVM đơn giản |
| Backend / API | Không có |
| Lưu dữ liệu | File SQLite cục bộ |
| Ảnh minh chứng | Lưu trong thư mục trên máy tính |

Ràng buộc đi kèm:

-   Project desktop dùng `TargetFramework` là `net6.0-windows` và
    `UseWPF` bật. Các project còn lại dùng `net6.0`.
-   Thêm `global.json` ở thư mục gốc để cố định SDK 6.0.428 và cho phép
    cập nhật bản vá: `{ "sdk": { "version": "6.0.428", "rollForward":
    "latestPatch" } }`.
-   Tất cả gói NuGet của Entity Framework Core và
    `Microsoft.Extensions.*` dùng phiên bản 6.0.x. Không dùng gói
    8.x vì không tương thích .NET 6.
-   Không dùng API hoặc cú pháp chỉ có từ C# 11 / .NET 7 trở lên (ví dụ
    `required` member, raw string literal, `TimeProvider`). Được dùng
    file-scoped namespace, record và nullable reference types.
-   Bật nullable reference types cho mọi project.
-   Gói bổ trợ được phép dùng: `CommunityToolkit.Mvvm` (MVVM) và
    `Microsoft.Extensions.DependencyInjection` (đăng ký dịch vụ ở mức
    đơn giản). Không thêm gói khác nếu chưa có nhu cầu cụ thể.
-   Không có ASP.NET Core, Web API, cloud storage, đăng nhập hay bất kỳ
    dịch vụ chạy nền riêng.
-   .NET 6 đã hết hỗ trợ bảo mật chính thức (từ 11/2024). Chấp nhận với
    ứng dụng cá nhân chạy cục bộ không cần Internet. Giữ code tránh phụ
    thuộc riêng .NET 6 để có thể nâng lên bản LTS mới hơn sau này chỉ
    bằng việc đổi `TargetFramework` và phiên bản gói.

### 15.0.1 Kiến trúc NLayer + MVVM đơn giản

Bốn lớp, mỗi lớp là một project:

-   **Domain:** entity, enum và quy tắc nghiệp vụ cốt lõi (ví dụ chuyển
    trạng thái hợp lệ). Không tham chiếu project nào khác.
-   **Application:** service nghiệp vụ theo từng nhóm (Problems,
    ScopeItems, WorkLogs, Evidence), kiểm tra dữ liệu và trả kết quả có
    thông báo tiếng Việt. Định nghĩa các interface nó cần từ lớp dưới
    (ví dụ lưu trữ file ảnh). Chỉ tham chiếu Domain.
-   **Infrastructure:** `DbContext` của EF Core, migration, repository
    và lưu file ảnh vào thư mục máy. Cài đặt các interface của
    Application. Tham chiếu Domain và Application.
-   **Desktop (Presentation):** View, ViewModel và các dịch vụ riêng của
    giao diện (điều hướng, hộp thoại, đọc ảnh từ clipboard). Tham chiếu
    Application; chỉ `App.xaml.cs` tham chiếu Infrastructure để đăng ký
    dịch vụ.

Quy tắc phụ thuộc: View → ViewModel → Application service →
interface do Application định nghĩa → cài đặt trong Infrastructure.
ViewModel không gọi `DbContext`, SQL hay `System.IO` trực tiếp. Quy tắc
nghiệp vụ nằm ở Domain hoặc Application, không nằm trong code-behind.

MVVM chỉ ở mức vừa đủ: mỗi màn hình có một ViewModel, dùng lệnh và
binding; không cần framework điều hướng phức tạp.

Cấu trúc solution đề xuất:

``` text
PathForge/
├── global.json
├── PathForge.sln
├── PathForge.Domain/
│   ├── Entities/
│   └── Enums/
├── PathForge.Application/
│   ├── Problems/
│   ├── ScopeItems/
│   ├── WorkLogs/
│   └── Evidence/
├── PathForge.Infrastructure/
│   ├── Persistence/
│   ├── Repositories/
│   └── FileStorage/
└── PathForge.Desktop/
    ├── Views/
    ├── ViewModels/
    ├── Services/
    ├── Resources/
    └── App.xaml
```

Đây là cấu trúc gợi ý, không phải yêu cầu tạo lớp/interface cho mọi tệp.
Nếu một service hoặc repository nhỏ không đem lại giá trị, giữ giải pháp
đơn giản. Tránh generic repository framework, CQRS, MediatR, event bus
hoặc pattern chỉ để tăng số lượng tệp. Việc đăng ký dịch vụ bằng
`Microsoft.Extensions.DependencyInjection` được phép nhưng giữ ở mức
một nơi cấu hình duy nhất.

### 15.1 Lưu trữ local

-   Database đặt ở `%LOCALAPPDATA%\\PathForge\\pathforge.db`, không đặt
    cạnh file executable.
-   Ảnh nằm trong `%LOCALAPPDATA%\\PathForge\\Evidence\\`.
-   Bản publish phải tạo thư mục dữ liệu nếu chưa tồn tại.
-   Không xóa hoặc ghi đè database hiện có khi khởi động.
-   Khởi tạo schema/migration một cách an toàn.
-   Đóng gói self-contained cho Windows; dữ liệu người dùng phải tồn tại
    qua các lần cập nhật ứng dụng.
-   Không yêu cầu Internet để sử dụng các chức năng cốt lõi.

### 15.2 Quy ước dùng EF Core 6 với SQLite

-   Dùng migration của EF Core; khi khởi động gọi `Database.Migrate()`.
    Không gọi `EnsureDeleted` hoặc `EnsureCreated` trên database của
    người dùng.
-   Ngày giờ lưu bằng `DateTime` ở UTC. Không dùng `DateTimeOffset` vì
    provider SQLite không sắp xếp được theo kiểu này.
-   Enum lưu dạng chuỗi bằng value conversion để dữ liệu dễ đọc và không
    vỡ khi thêm giá trị mới.
-   Cấu hình khóa ngoại và xóa theo tầng cho dữ liệu con của Problem
    (Scope Item, WorkLog, Evidence); xác nhận ràng buộc khóa ngoại của
    SQLite đang hoạt động.
-   Truy vấn chỉ đọc dùng `AsNoTracking`. Mọi thao tác ghi nhiều bước
    (xóa bài toán, hoàn tất báo cáo) chạy trong một lần `SaveChanges` hoặc
    một transaction.
-   Chuỗi kết nối trỏ tới `%LOCALAPPDATA%\\PathForge\\pathforge.db`, được
    tạo từ đường dẫn thư mục dữ liệu, không hardcode đường dẫn máy cá
    nhân.

### 15.3 Đóng gói với .NET 6

-   Publish self-contained cho `win-x64`, không cần cài .NET Runtime.
-   Nếu dùng single-file thì bật `IncludeNativeLibrariesForSelfExtract`
    để thư viện native của SQLite được đóng gói đúng. Nếu gặp lỗi, ưu
    tiên publish dạng thư mục thay vì single-file.
-   Đóng gói và cập nhật không được ghi đè hoặc xóa thư mục dữ liệu ở
    `%LOCALAPPDATA%\\PathForge\\`.

## 16. Tiêu chí nghiệm thu MVP

### Nhóm A: Bài toán

-   [ ] Tạo bài toán với tên và mục tiêu hợp lệ.
-   [ ] Không cho lưu nếu thiếu tên hoặc mục tiêu; báo lỗi cạnh trường
    tương ứng.
-   [ ] Có thể lưu khi mô tả, tiêu chí thành công và sáu trường 5W-1H chưa có.
-   [ ] Có thể cập nhật riêng từng trường 5W-1H, mục tiêu và phạm vi sau khi lưu; dữ liệu vẫn còn sau khi mở lại ứng dụng.
-   [ ] Chỉnh sửa và mở lại bài toán vẫn giữ dữ liệu.
-   [ ] Trạng thái chuyển theo các quy tắc ở mục 6.
-   [ ] Bài toán đã hoàn thành vẫn xem được và có thể mở lại.

### Nhóm B: Phân rã

-   [ ] Tạo, sửa và đổi trạng thái Scope Item.
-   [ ] Chuyển mục sang Backlog không làm mất nội dung.
-   [ ] Đánh dấu Scope Item Done không tự hoàn thành Problem.
-   [ ] Không bắt buộc tạo Scope Item cho mọi bài toán.
-   [ ] Có thể ghi nhanh Scope Item với tiêu đề, lý do tùy chọn và trạng thái do người dùng chọn; không tự phân loại hoặc tạo nhiệm vụ.

### Nhóm C: Phiên thực hiện

-   [ ] Bắt đầu phiên với một nhiệm vụ chính có thể được mô tả thành hành động hoặc câu hỏi kiểm chứng; người dùng có thể điều chỉnh nhiệm vụ khi có thông tin mới.
-   [ ] Ghi nhanh chủ đề phát sinh không làm mất phiên hoặc thay nhiệm vụ hiện tại; người dùng có thể chọn Backlog rồi tiếp tục phiên.
-   [ ] Timer có thể sử dụng hoặc bỏ qua.
-   [ ] Có thể tạm dừng và tiếp tục.
-   [ ] Không thể có hai phiên Running đồng thời.
-   [ ] Kết thúc phiên đưa người dùng đến báo cáo.
-   [ ] Bỏ phiên yêu cầu xác nhận và không tự làm bài toán thành
    Blocked.
-   [ ] Phiên được khôi phục đúng sau khi đóng/mở ứng dụng.

### Nhóm D: Báo cáo

-   [ ] Không thể hoàn tất báo cáo nếu cả Result và Learned đều trống.
-   [ ] Có thể ghi kết quả chưa đạt, giả thuyết bị bác bỏ hoặc nội dung còn chưa xác định.
-   [ ] Không bắt buộc ảnh minh chứng cho mỗi phiên; NextStep đã lưu được ưu tiên hiển thị trên Tổng quan.
-   [ ] Báo cáo đã lưu xem lại và chỉnh sửa được.
-   [ ] Lỗi lưu không làm mất nội dung đang nhập.
-   [ ] Hoàn tất báo cáo không tự hoàn thành bài toán.

### Nhóm E: Minh chứng

-   [ ] Thêm ảnh PNG/JPEG/WebP hợp lệ.
-   [ ] Từ chối định dạng không hỗ trợ và ảnh lớn hơn giới hạn.
-   [ ] Xem và xóa ảnh được.
-   [ ] Không có bản ghi Evidence nếu thao tác sao chép ảnh thất bại.
-   [ ] Khi Problem yêu cầu minh chứng, không cho xác nhận hoàn thành
    nếu chưa có ảnh.

### Nhóm F: Lịch sử và dữ liệu

-   [ ] Tìm kiếm bài toán, nhiệm vụ và nội dung báo cáo.
-   [ ] Mở kết quả tìm kiếm đến đúng nội dung.
-   [ ] Sau khi đóng/mở ứng dụng, truy cập thuận tiện mục tiêu, 5W-1H, Scope Item trong phạm vi/Backlog, báo cáo gần nhất, trở ngại và NextStep.
-   [ ] Lịch sử phiên điều hướng được đến Problem và Scope Item liên quan khi phiên được tạo từ một mục phạm vi.
-   [ ] Dữ liệu vẫn còn sau khi đóng và mở ứng dụng.
-   [ ] Xóa bài toán có xác nhận và không để lại bản ghi con.
-   [ ] Chạy ứng dụng khi không có Internet.

### Nhóm G: Đóng gói

-   [ ] Chạy được trên Windows theo cấu hình publish đã chọn.
-   [ ] Không cần cài database server hoặc chạy backend.
-   [ ] Không cần tự cài .NET Runtime nếu phát hành bản self-contained.
-   [ ] Dữ liệu nằm ngoài thư mục cài đặt.
-   [ ] Cập nhật executable không làm mất database và ảnh hiện có.

## 17. Kế hoạch triển khai theo thứ tự

### Giai đoạn 1: Nền tảng và bài toán

-   Tạo solution và các project tối thiểu.
-   Tạo SQLite và migration/schema.
-   Làm CRUD Problem.
-   Làm điều hướng và Tổng quan cơ bản.
-   Kiểm tra dữ liệu tồn tại sau khi khởi động lại.

### Giai đoạn 2: Phân rã bài toán

-   CRUD Scope Item.
-   Trạng thái InScope, Backlog, Done, Excluded.
-   Tích hợp danh sách Scope Item vào chi tiết Problem.

### Giai đoạn 3: Phiên thực hiện và báo cáo

-   Bắt đầu, tạm dừng, tiếp tục, kết thúc và bỏ phiên.
-   Timer tùy chọn dựa trên timestamp.
-   Báo cáo phiên và validation.
-   Khôi phục phiên khi mở lại ứng dụng.

### Giai đoạn 4: Minh chứng và lịch sử

-   Lưu ảnh local và metadata.
-   Tìm kiếm.
-   Xem lại phiên, báo cáo và ảnh.
-   Tiếp tục bài toán từ lịch sử.

### Giai đoạn 5: Hoàn thiện MVP

-   Xác nhận hoàn thành/mở lại bài toán.
-   Xóa dữ liệu có xác nhận.
-   Xử lý các trạng thái lỗi và nội dung chưa lưu.
-   Chạy toàn bộ tiêu chí nghiệm thu.
-   Publish self-contained và kiểm tra trên thư mục dữ liệu sạch lẫn thư
    mục đã có dữ liệu.

Không bắt đầu bằng các tính năng ngoài MVP. Hoàn tất và kiểm tra từng
giai đoạn trước khi chuyển sang giai đoạn tiếp theo.

## 18. Chỉ dẫn bắt buộc cho AI coding agent

1.  Đọc toàn bộ tài liệu trước khi viết code.
2.  Đây là ứng dụng WPF local chạy trên .NET 6 với EF Core 6 và SQLite.
    Không tạo backend ASP.NET Core, Web API, tài khoản, đăng nhập, cloud
    database hoặc cloud image storage. Không nâng phiên bản .NET hoặc EF
    Core khi chưa được người dùng đồng ý.
3.  Không tự bổ sung tính năng ngoài phạm vi MVP.
4.  Ưu tiên luồng nghiệp vụ đơn giản và dễ dùng hơn kiến trúc phức tạp.
5.  Không coi thời gian tập trung hoặc số lượng nhiệm vụ hoàn tất là
    bằng chứng bài toán đã giải quyết.
6.  Không tự chuyển Problem sang Resolved; phải có xác nhận của người
    dùng.
7.  Không tự sinh nhiệm vụ tiếp theo hoặc tự động mở rộng phạm vi.
8.  Tất cả chuyển trạng thái phải được kiểm soát trong logic nghiệp vụ,
    không chỉ ẩn/hiện nút trên UI.
9.  Thực hiện thao tác lưu/xóa theo cách tránh dữ liệu dở dang; thông
    báo đúng kết quả thực tế.
10. Không ghi đè hoặc xóa database khi khởi động, cập nhật hoặc publish.
11. Tạo test cho các rule trạng thái và tiêu chí nghiệm thu quan trọng;
    không chỉ kiểm tra giao diện.
12. Trước khi triển khai mỗi giai đoạn, nêu ngắn gọn tệp sẽ tạo/sửa và
    tiêu chí hoàn thành. Sau đó triển khai, build và chạy test liên
    quan.
13. Nếu gặp điểm chưa rõ, chọn phương án đơn giản nhất phù hợp với mục
    tiêu sản phẩm và ghi lại giả định. Chỉ hỏi người dùng khi quyết định
    đó làm thay đổi đáng kể nghiệp vụ hoặc phạm vi.
14. Không tuyên bố build/test thành công nếu chưa thực sự chạy lệnh và
    kiểm tra kết quả.
15. Không tạo thêm project, interface, abstraction hoặc package chỉ vì
    thông lệ nếu chưa có nhu cầu cụ thể.

## 19. Định nghĩa hoàn thành MVP

MVP được xem là hoàn thành khi người dùng có thể thực hiện trọn vòng lặp
sau mà không cần Internet:

**Tạo bài toán → phân tích 5W-1H (có thể bổ sung dần) → xác định mục tiêu → chia nhỏ phạm vi → chọn một nhiệm vụ để
làm → thực hiện phiên và khám phá có kiểm soát → ghi kết quả → xác định bước tiếp theo → tiếp tục
hoặc xác nhận hoàn thành → tìm lại lịch sử.**

Ứng dụng phải lưu được dữ liệu giữa các lần chạy, không tự kết luận thay
người dùng và không biến việc quản lý công cụ thành một công việc lớn
hơn chính bài toán cần giải quyết.
