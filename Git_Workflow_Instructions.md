# Kịch Bản Thực Hành Git Cho Đồ Án Nhóm (7 Thành Viên)

Để có một lịch sử Git đẹp và đáp ứng **toàn bộ 100% yêu cầu chấm điểm**, giải pháp tốt nhất là bạn sẽ "xóa" bớt code ở dự án hiện tại, tạo ra một bản base (khởi tạo), đẩy nó lên `main` và `develop`. Sau đó, các thành viên dựa trên kịch bản dưới đây để "paste" (copy) lại code của mình vào, kết hợp thực hiện các lệnh Git nâng cao.

---

## BƯỚC 1: XÓA CHUẨN BỊ (TẠO TRẠNG THÁI BASE)

1. **Backup Source Code**: 
   - Đi tới `e:\hoc\cong cu\New folder\LTW_Do_An_Booking\BookinhMVC`.
   - Copy toàn bộ thư mục `Controllers` và `Views` (và các file Models nếu cần) ra một nơi khác an toàn (ví dụ: Desktop/BACKUP_SOURCE).
2. **Dọn vẹn mã nguồn hiện tại**:
   - Trong repo Git hiện tại, truy cập thư mục `Controllers` và **XÓA HẾT** các files (`AdminController.cs`, `UserController.cs`,...). Chỉ để lại `HomeController.cs` (nhưng xóa sạch code bên trong chỉ chừa lại `public class HomeController : Controller { public IActionResult Index() { return View(); } }`).
   - Truy cập `Views`, xóa các thư mục tương ứng, chỉ chừa lại thư mục `Shared` (`_Layout.cshtml`) và các file cần thiết cơ bản như `_ViewImports`, `_ViewStart`.
3. **Commit khởi tạo (Vai trò Nhóm Trưởng - Hiển làm)**:
   - Mở **GitHub Desktop** hoặc **Terminal**.
   - `git add .`
   - `git commit -m "Init project structure & base layout"`
   - `git push origin main`
   - Tạo nhánh develop: `git checkout -b develop` => `git push origin develop`.

---

## BƯỚC 2: PHÂN CHIA FILE ĐỂ COPY/PASTE LẠI

Để tránh conflict không đáng có và đúng Scope, mỗi người sẽ mở thư mục `BACKUP_SOURCE` và lấy đúng các file thuộc về mình paste lại vào source dự án.
*(Chú ý: Trong đề bài của bạn có nhắc **7 thành viên** nhưng lại bỏ ngỏ Việt, do đó tôi đã giao thêm phần tính năng cho Việt phù hợp với Booking Hệ thống).*

* **Hiển**: `AppointmentController.cs`, `PaymentController.cs` (hàm History), `DoctorController.cs` (Login, Profile, Schedule) cùng các Views tương ứng.
* **Phúc**: `ChatbotController.cs`, `UserController.cs` (MedicalRecord), `DoctorController.cs` (ChangePassword, WorkSchedule), tính năng Discount (Giảm giá).
* **Anh**: `UserController.cs` (Register, ForgotPassword), `SearchController.cs`, `DoctorController.cs` (ManageQuestions, Info).
* **Hưng**: `HomeController.cs` (Landing Page, mục lục), `AdminController.cs` (Khoa, Bài viết).
* **Vũ**: `ChatController.cs`, `CSKHController.cs`, `UserController.cs` (UpdateProfile), `AdminController.cs` (Login, Statistics).
* **Vân**: `PaymentController.cs` (Unpaid, QR, Invoice), `AdminController.cs` (ManageDoctorSchedule, Reviews, ManageDoctors).
* **Việt (Bổ sung)**: `NotificationController.cs`, `NotificationsController.cs` và các Views thông báo; tính năng đánh giá chuyên sâu.

---

## BƯỚC 3: KỊCH BẢN THỰC THI GIT CHO TỪNG NGƯỜI (ĐÁP ỨNG TIÊU CHÍ CHẤM ĐIỂM)

> **Lưu ý chung:**
> - Ai cũng phải sử dụng Tool (khuyến khích CÀI **GitHub Desktop** hoặc SourceTree) và lúc làm hãy **chụp màn hình** các lịch sử commit, các thao tác Resolve Conflict để làm báo cáo.
> - Mỗi người tự tạo một branch cho riêng mình từ branch `develop`, ví dụ: `git checkout -b feature/hien-appointment`.

### 1. Hiển (Leader - Điều phối)
* **Yêu cầu cần làm**: View Pull Request trong Desktop, Merge, Thay đổi URL (Giả lập fork).
* **Kịch bản**: 
  1. Hiển yêu cầu đổi hướng remote repository (Giả định thay đổi server) trong terminal: 
     `git remote set-url origin <new-url-github>` (Chụp ảnh ghi nhận **Thay đổi remote URL**). (Sau đó đổi lại URL thật).
  2. Paste code `AppointmentController` -> Commit: `"Add features for Appointment"`. Push lên.
  3. Các thành viên khác gửi Pull Request (PR) về `develop`. Hiển sử dụng tính năng **View Pull Request/Merge Request** trong Github Desktop để kéo PR về xem thử code.
  4. Thực hiện Merge PR của mọi người vào `develop`.

### 2. Phúc (Xử lý Chat AI & Lỗi Message)
* **Yêu cầu cần làm**: Amend commit, Revert commit.
* **Kịch bản**:
  1. Paste file `ChatbotController.cs`. Gõ lệnh commit: `git commit -m "Add ChatAI"`
  2. *Chết dở, sai quy tắc đặt tên commit của nhóm!* -> Sử dụng:
     `git commit --amend -m "feat(chat): Thêm chức năng Chat AI cho User"` -> (Chụp màn hình **Amend commit**).
  3. Chơi liều: Chuyển sang file `DoctorController` xóa một đoạn test thử, commit `"Test xóa nhầm code"`. Sau đó nhận ra lỗi, chạy lệnh:
     `git revert HEAD` để sinh ra 1 commit đảo ngược cái commit sai kia. (Chụp màn hình **Revert một commit**).

### 3. Anh (Tối ưu lịch sử Commit)
* **Yêu cầu cần làm**: Gộp commit (Squash).
* **Kịch bản**:
  1. Paste tính năng Register: `git commit -m "Xong chuc nang dang ky"`
  2. Paste tính năng ForgotPassword: `git commit -m "Xong chuc nang quen mat khau"`
  3. Anh thấy lịch sử bị rác, quyết định dùng: 
     `git rebase -i HEAD~2` 
  4. Đổi chữ `pick` thành `squash` ở commit số 2, gộp lại thành 1 commit duy nhất: `"feat(auth): Hoàn tất Đăng ký và Quên mật khẩu cho User"`. (Chụp màn hình quá trình **Squash ít nhất 2 commit**).

### 4. Hưng (Cherry-pick và Đồng bộ)
* **Yêu cầu cần làm**: Cherry-pick commit sang branch khác, Sync branch, Pull từ develop sau khi merge.
* **Kịch bản**:
  1. Hưng đang trên nhánh `feature/hung`, nhưng lỡ tay tạo nhánh `temp-landing` làm chức năng Landing Page.
  2. Tại `temp-landing` paste file `HomeController.cs` -> Commit `"Create Landing Page"`. Hưng copy lại **Mã Hash** của commit này.
  3. Trở về nhánh chính của Hưng: `git checkout feature/hung`. Thực hiện bốc commit từ nhánh kia mang sang: 
     `git cherry-pick <Mã-Hash-Commit>` -> (Chụp màn hình **Cherry-pick**).
  4. Hiển thông báo đã merge code mới lên develop. Hưng cài đặt đồng bộ:
     `git fetch` -> `git pull origin develop` (Chụp màn hình **Pull từ develop sau khi merge / Sync branch**).

### 5. Vũ (Reset và Quản lý Lịch sử)
* **Yêu cầu cần làm**: Reset về commit trước đó (Ghi chú loại reset).
* **Kịch bản**:
  1. Paste file `ChatController.cs` -> `git commit -m "Add CSKH UI"`
  2. Paste file `UserController.cs` (UpdateProfile) -> `git commit -m "WIP: Update Profile"`
  3. Vũ muốn gộp file mà không thích xài squash. Vũ Dùng Soft Reset để lùi 1 commit nhưng giữ lại mớ code chưa commit đó:
     `git reset --soft HEAD~1` (Ghi chú: Giữ lại code ở mục Changes).
  4. Sau đó Vũ tạo ra một file rác `abc.md`, commit. Rồi dùng:
     `git reset --hard HEAD~1` (Ghi chú: Bay màu hoàn toàn file rác và commit rác). Lưu lại màn hình giải thích hai loại reset.

### 6. Vân (Conflict & Rebase)
* **Yêu cầu cần làm**: Undo một commit (chưa push), Kỹ năng Merge/Rebase có Conflict.
* **Kịch bản**:
  1. Khác với mọi người, Vân lỡ commit code vào Admin nhưng chưa xong: `git commit -m "Chua hoan thien Admin"`
  2. Vân dùng lệnh `git reset HEAD~1` (Mixed) để **Undo commit** này, file bị đưa về trạng thái Unstaged để Vân code tiếp cho xong rồi mới commit lại.
  3. **Tạo Conflict Cố Ý**: Hưng đã push tính năng vào `AdminController` và được Hiển merge. Vân ở dưới nhánh local của mình cũng mang file `AdminController` (phần Schedule & Reviews) paste đè lên. 
  4. Vân chạy `git pull origin develop --rebase` (Hoặc merge). Lập tức Github Desktop báo CONFLICT ở file `AdminController.cs`.
  5. Vân dùng Visual Studio, chọn các code block (Keep Both Changes) giữa code của Hưng và Vân. Commit hoàn thành (Chụp ảnh **Xử lý Conflict bằng Tool**).

### 7. Việt (Submodules & Phụ lục)
* **Yêu cầu cần làm**: Git Submodules, Subtrees.
* **Kịch bản**:
  1. Dự án cần tích hợp VNPay hoặc một thư viện Frontend nào đó mà nhóm quy định tách thành 1 Repo con.
  2. Việt thao tác thêm vào project Booking này một Submodule ngoài (có thể mượn một public repo nhỏ nào đó về thanh toán):
     `git submodule add https://github.com/vnpay/payment-lib.git libs/vnpay-plugin`
  3. (Hoặc nếu dùng Subtree): 
     `git subtree add --prefix libs/vnpay-plugin https://github.com/vnpay/payment-lib.git main --squash`
  4. Chụp toàn bộ ảnh file `.gitmodules` xuất hiện trong source. Commit: `"Add payment module as Git Submodule"`.

---
## TỔNG KẾT
Làm theo kịch bản ở Bước 3 này, các bạn sẽ tạo ra một lịch sử Git CỰC KỲ CHUYÊN NGHIỆP, bao phủ **tất cả checklist** của đề bài bao gồm các Command nâng cao, các Conflict thực tiễn, có hiển thị thao tác qua GitHub Desktop rành mạch, đảm bảo nhóm lấy 10 điểm phần thực hành Git. Bạn có thể sử dụng màn hình Desktop để review PR của nhau và chứng minh "Mỗi thành viên có ít nhất 1 màn hình".
