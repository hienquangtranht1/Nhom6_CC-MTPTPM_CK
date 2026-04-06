using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BookinhMVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace BookinhMVC.Controllers
{
    public class AdminController : Controller
    {
        private readonly BookingContext _context;

        public AdminController(BookingContext context) => _context = context;

        // 1. MIDDLEWARE: KIỂM TRA QUYỀN ADMIN CHO MỌI ACTION
        
        // 3. DASHBOARD
        public IActionResult Index()
        {
            ViewBag.UserCount = _context.NguoiDungs.Count();
            ViewBag.DoctorCount = _context.BacSis.Count();
            ViewBag.PatientCount = _context.BenhNhans.Count();
            ViewBag.DepartmentCount = _context.Khoas.Count();
            ViewBag.AppointmentCount = _context.LichHens.Count();
            ViewBag.ReviewCount = _context.DanhGias.Count();
            // Thêm thống kê bài viết từ Demo 1
            ViewBag.ArticleCount = _context.Articles.Count();
            return View();
        }

        // =========================================================
        // CRUD KHOA (Departments)
        // =========================================================
       

        // =========================================================
        // CRUD NGƯỜI DÙNG (Users)
        // =========================================================
        public async Task<IActionResult> Users()
        {
            var users = await _context.NguoiDungs.AsNoTracking().ToListAsync();
            return View(users);
        }

        public IActionResult CreateUser() => View();

        [HttpPost]
        public async Task<IActionResult> CreateUser(string tenDangNhap, string matKhau, string vaiTro)
        {
            var model = new NguoiDung
            {
                TenDangNhap = tenDangNhap,
                VaiTro = vaiTro,
                NgayTao = DateTime.Now
            };
            var hasher = new PasswordHasher<NguoiDung>();
            model.MatKhau = hasher.HashPassword(model, matKhau);
            _context.NguoiDungs.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(int id, string tenDangNhap, string vaiTro, string newPassword)
        {
            var existing = await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(u => u.MaNguoiDung == id);
            if (existing == null) return NotFound();
            var model = new NguoiDung
            {
                MaNguoiDung = id,
                TenDangNhap = tenDangNhap,
                VaiTro = vaiTro,
                NgayTao = existing.NgayTao,
                MatKhau = existing.MatKhau
            };
            if (!string.IsNullOrEmpty(newPassword))
            {
                var hasher = new PasswordHasher<NguoiDung>();
                model.MatKhau = hasher.HashPassword(model, newPassword);
            }
            _context.NguoiDungs.Update(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user != null)
            {
                _context.NguoiDungs.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // =========================================================
        
        // =========================================================
        // CRUD BỆNH NHÂN (Patients)
        // =========================================================
        public async Task<IActionResult> Patients()
        {
            var patients = await _context.BenhNhans.AsNoTracking().ToListAsync();
            return View(patients);
        }

        public IActionResult CreatePatient() => View();

        [HttpPost]
        public async Task<IActionResult> CreatePatient(int maNguoiDung, string hoTen, DateTime ngaySinh, string gioiTinh, string soDienThoai, string email, string diaChi, string soBaoHiem, string hinhAnhBenhNhan)
        {
            var model = new BenhNhan
            {
                MaBenhNhan = maNguoiDung,
                HoTen = hoTen,
                NgaySinh = ngaySinh,
                GioiTinh = gioiTinh,
                SoDienThoai = soDienThoai,
                Email = email,
                DiaChi = diaChi,
                SoBaoHiem = soBaoHiem,
                HinhAnhBenhNhan = hinhAnhBenhNhan,
                NgayTao = DateTime.Now
            };
            _context.BenhNhans.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Patients");
        }

        public async Task<IActionResult> EditPatient(int id)
        {
            var patient = await _context.BenhNhans.FindAsync(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        [HttpPost]
        public async Task<IActionResult> EditPatient(IFormCollection form, IFormFile file)
        {
            var id = int.Parse(form["id"]);
            var patient = await _context.BenhNhans.FindAsync(id);
            if (patient == null) return NotFound();

            string fileName = form["hinhAnhBenhNhan"];
            if (file != null && file.Length > 0)
            {
                fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
                using (var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }

            patient.HinhAnhBenhNhan = fileName;
            patient.HoTen = form["hoTen"];
            patient.NgaySinh = DateTime.Parse(form["ngaySinh"]);
            patient.GioiTinh = form["gioiTinh"];
            patient.SoDienThoai = form["soDienThoai"];
            patient.Email = form["email"];
            patient.DiaChi = form["diaChi"];
            patient.SoBaoHiem = form["soBaoHiem"];

            _context.BenhNhans.Update(patient);
            await _context.SaveChangesAsync();
            return RedirectToAction("Patients");
        }

        public async Task<IActionResult> DeletePatient(int id)
        {
            var patient = await _context.BenhNhans.FindAsync(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        [HttpPost, ActionName("DeletePatient")]
        public async Task<IActionResult> DeletePatientConfirmed(int id)
        {
            var patient = await _context.BenhNhans.FindAsync(id);
            if (patient != null)
            {
                _context.BenhNhans.Remove(patient);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Patients");
        }

        public async Task<IActionResult> PatientDetails(int id)
        {
            var patient = await _context.BenhNhans.FindAsync(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        // =========================================================
        // CRUD LỊCH HẸN (Appointments)
        // =========================================================
        public async Task<IActionResult> Appointments()
        {
            var appts = await _context.LichHens
                .Include(l => l.BenhNhan)
                .Include(l => l.BacSi).ThenInclude(bs => bs.Khoa)
                .AsNoTracking()
                .ToListAsync();
            return View(appts);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAppointment()
        {
            ViewBag.Doctors = await _context.BacSis.Include(b => b.Khoa).ToListAsync();
            ViewBag.Patients = await _context.BenhNhans.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(LichHen appointment)
        {
            if (ModelState.IsValid)
            {
                var exists = await _context.LichHens.AnyAsync(l =>
                    l.MaBacSi == appointment.MaBacSi &&
                    l.NgayGio == appointment.NgayGio &&
                    l.TrangThai != "Đã hủy");

                if (exists)
                {
                    ModelState.AddModelError("", "Thời gian này đã có lịch hẹn.");
                }
                else
                {
                    if (string.IsNullOrEmpty(appointment.TrangThai)) appointment.TrangThai = "Chờ xác nhận";
                    _context.Add(appointment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Appointments");
                }
            }
            ViewBag.Doctors = await _context.BacSis.Include(b => b.Khoa).ToListAsync();
            ViewBag.Patients = await _context.BenhNhans.ToListAsync();
            return View(appointment);
        }

        public async Task<IActionResult> EditAppointment(int id)
        {
            var appt = await _context.LichHens.FindAsync(id);
            if (appt == null) return NotFound();
            ViewBag.Doctors = await _context.BacSis.Include(b => b.Khoa).ToListAsync();
            ViewBag.Patients = await _context.BenhNhans.ToListAsync();
            return View(appt);
        }

        [HttpPost]
        public async Task<IActionResult> EditAppointment(int id, int maBenhNhan, int maBacSi, DateTime ngayGio, string trieuChung, string trangThai)
        {
            var existing = await _context.LichHens.FindAsync(id);
            if (existing == null) return NotFound();

            existing.MaBenhNhan = maBenhNhan;
            existing.MaBacSi = maBacSi;
            existing.NgayGio = ngayGio;
            existing.TrieuChung = trieuChung;
            existing.TrangThai = trangThai;

            _context.LichHens.Update(existing);
            await _context.SaveChangesAsync();
            return RedirectToAction("Appointments");
        }

        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appt = await _context.LichHens.Include(l => l.BenhNhan).Include(l => l.BacSi).FirstOrDefaultAsync(l => l.MaLich == id);
            if (appt == null) return NotFound();
            return View(appt);
        }

        [HttpPost, ActionName("DeleteAppointment")]
        public async Task<IActionResult> DeleteAppointmentConfirmed(int id)
        {
            var appt = await _context.LichHens.FindAsync(id);
            if (appt != null)
            {
                _context.LichHens.Remove(appt);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Appointments");
        }

        public async Task<IActionResult> AppointmentDetails(int id)
        {
            var appt = await _context.LichHens.Include(l => l.BenhNhan).Include(l => l.BacSi).ThenInclude(bs => bs.Khoa).FirstOrDefaultAsync(l => l.MaLich == id);
            if (appt == null) return NotFound();
            return View(appt);
        }

        // =========================================================
        // CRUD ĐÁNH GIÁ (Reviews)
        // =========================================================
        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.DanhGias.Include(r => r.BacSi).Include(r => r.BenhNhan).AsNoTracking().ToListAsync();
            return View(reviews);
        }

        [HttpGet]
        public async Task<IActionResult> CreateReview()
        {
            ViewBag.Doctors = await _context.BacSis.Include(b => b.Khoa).ToListAsync();
            ViewBag.Patients = await _context.BenhNhans.ToListAsync();
            return View(new DanhGia());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReview(DanhGia model)
        {
            ModelState.Remove("BacSi");
            ModelState.Remove("BenhNhan");

            if (model.MaBacSi == 0 || model.MaBenhNhan == 0 || model.DiemDanhGia == 0)
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin.");

            if (ModelState.IsValid)
            {
                if (model.NgayDanhGia == default) model.NgayDanhGia = DateTime.Now;
                _context.DanhGias.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm đánh giá thành công!";
                return RedirectToAction("Reviews");
            }
            ViewBag.Doctors = await _context.BacSis.Include(b => b.Khoa).ToListAsync();
            ViewBag.Patients = await _context.BenhNhans.ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditReview(int id)
        {
            var review = await _context.DanhGias.FindAsync(id);
            if (review == null) return NotFound();
            ViewBag.Doctors = await _context.BacSis.Include(b => b.Khoa).ToListAsync();
            ViewBag.Patients = await _context.BenhNhans.ToListAsync();
            return View(review);
        }

        [HttpPost]
        public async Task<IActionResult> EditReview(int id, int maBacSi, int maBenhNhan, int diemDanhGia, string nhanXet, DateTime ngayDanhGia)
        {
            var model = new DanhGia { MaDanhGia = id, MaBacSi = maBacSi, MaBenhNhan = maBenhNhan, DiemDanhGia = diemDanhGia, NhanXet = nhanXet, NgayDanhGia = ngayDanhGia };
            _context.DanhGias.Update(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Reviews");
        }

        public async Task<IActionResult> ReviewDetails(int id)
        {
            var review = await _context.DanhGias.Include(r => r.BacSi).Include(r => r.BenhNhan).FirstOrDefaultAsync(r => r.MaDanhGia == id);
            if (review == null) return NotFound();
            return View(review);
        }

        [HttpPost, ActionName("DeleteReview")]
        public async Task<IActionResult> DeleteReviewConfirmed(int id)
        {
            var review = await _context.DanhGias.FindAsync(id);
            if (review != null)
            {
                _context.DanhGias.Remove(review);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Reviews");
        }

        // =========================================================
        // CRUD LỊCH LÀM VIỆC (Work Schedules)
        // =========================================================
        public IActionResult WorkSchedules(int? doctorId, DateTime? weekStart)
        {
            var query = _context.LichLamViecs.Include(l => l.BacSi).ThenInclude(bs => bs.NguoiDung).AsQueryable();
            if (doctorId.HasValue) query = query.Where(l => l.MaBacSi == doctorId.Value);

            DateTime start = weekStart ?? DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            var schedules = query.Where(l => l.NgayLamViec >= start && l.NgayLamViec <= start.AddDays(6)).OrderBy(l => l.MaBacSi).ThenBy(l => l.NgayLamViec).ToList();

            ViewBag.Doctors = _context.BacSis.Include(bs => bs.NguoiDung).ToList();
            ViewBag.SelectedDoctor = doctorId;
            ViewBag.WeekStart = start;
            return View("WorkSchedules", schedules);
        }

        [HttpPost]
        public IActionResult ConfirmWorkSchedule(int id)
        {
            var schedule = _context.LichLamViecs.FirstOrDefault(l => l.MaLich == id);
            if (schedule != null)
            {
                schedule.TrangThai = "Đã xác nhận";
                _context.SaveChanges();
            }
            return RedirectToAction("WorkSchedules");
        }

        public IActionResult CreateWorkSchedule(int? doctorId, string date)
        {
            ViewBag.Doctors = _context.BacSis.Include(b => b.Khoa).ToList();
            var model = new LichLamViec();
            if (doctorId.HasValue) model.MaBacSi = doctorId.Value;
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var d)) model.NgayLamViec = d;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkSchedule(int MaBacSi, DateTime NgayLamViec, string GioBatDau, string GioKetThuc, string TrangThai)
        {
            var lich = new LichLamViec
            {
                MaBacSi = MaBacSi,
                NgayLamViec = NgayLamViec,
                ThuTrongTuan = ((int)NgayLamViec.DayOfWeek == 0 ? "8" : ((int)NgayLamViec.DayOfWeek + 1).ToString()),
                GioBatDau = TimeSpan.Parse(GioBatDau),
                GioKetThuc = TimeSpan.Parse(GioKetThuc),
                TrangThai = TrangThai,
                NgayTao = DateTime.Now
            };
            _context.LichLamViecs.Add(lich);
            await _context.SaveChangesAsync();
            return RedirectToAction("WorkSchedules");
        }

        [HttpGet]
        public IActionResult EditWorkSchedule(int id)
        {
            ViewBag.Doctors = _context.BacSis.Include(b => b.Khoa).ToList();
            var model = _context.LichLamViecs.Include(l => l.BacSi).FirstOrDefault(l => l.MaLich == id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditWorkSchedule(int MaLich, int MaBacSi, DateTime NgayLamViec, string GioBatDau, string GioKetThuc, string TrangThai)
        {
            var lich = await _context.LichLamViecs.FindAsync(MaLich);
            if (lich == null) return NotFound();
            lich.MaBacSi = MaBacSi;
            lich.NgayLamViec = NgayLamViec;
            lich.ThuTrongTuan = ((int)NgayLamViec.DayOfWeek == 0 ? "8" : ((int)NgayLamViec.DayOfWeek + 1).ToString());
            lich.GioBatDau = TimeSpan.Parse(GioBatDau);
            lich.GioKetThuc = TimeSpan.Parse(GioKetThuc);
            lich.TrangThai = TrangThai;
            await _context.SaveChangesAsync();
            return RedirectToAction("WorkSchedules");
        }

        [HttpGet]
        public IActionResult DeleteWorkSchedule(int id)
        {
            var model = _context.LichLamViecs.Include(l => l.BacSi).ThenInclude(b => b.Khoa).FirstOrDefault(l => l.MaLich == id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost, ActionName("DeleteWorkSchedule")]
        public async Task<IActionResult> DeleteWorkScheduleConfirmed(int MaLich)
        {
            var lich = await _context.LichLamViecs.FindAsync(MaLich);
            if (lich != null)
            {
                _context.LichLamViecs.Remove(lich);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("WorkSchedules");
        }

        // =========================================================
        // ADMIN PROFILE & SETTINGS
        // =========================================================
        public async Task<IActionResult> Profile()
        {
            ViewBag.Title = "Thông tin cá nhân";
            var adminName = HttpContext.Session.GetString("AdminName");
            if (string.IsNullOrEmpty(adminName)) return RedirectToAction("Login");

            var admin = await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(u => u.TenDangNhap == adminName && u.VaiTro == "Admin");
            if (admin == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }
            return View(admin);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            ViewBag.Title = "Cập nhật hồ sơ";
            var adminName = HttpContext.Session.GetString("AdminName");
            var admin = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.TenDangNhap == adminName && u.VaiTro == "Admin");
            if (admin == null) return NotFound();
            return View(admin);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(int maNguoiDung, string tenDangNhap)
        {
            var existing = await _context.NguoiDungs.FindAsync(maNguoiDung);
            if (existing == null || existing.VaiTro != "Admin") return NotFound();

            existing.TenDangNhap = tenDangNhap;
            try
            {
                _context.NguoiDungs.Update(existing);
                await _context.SaveChangesAsync();
                HttpContext.Session.SetString("AdminName", existing.TenDangNhap);
                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
            }
            return View(existing);
        }

        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
        {
            var adminName = HttpContext.Session.GetString("AdminName");
            var admin = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.TenDangNhap == adminName && u.VaiTro == "Admin");
            if (admin == null) return RedirectToAction("Login");

            if (newPassword != confirmNewPassword)
            {
                ViewBag.Error = "Mật khẩu mới không khớp.";
                return View();
            }

            var hasher = new PasswordHasher<NguoiDung>();
            if (hasher.VerifyHashedPassword(admin, admin.MatKhau, currentPassword) == PasswordVerificationResult.Failed)
            {
                ViewBag.Error = "Mật khẩu hiện tại không đúng.";
                return View();
            }

            admin.MatKhau = hasher.HashPassword(admin, newPassword);
            _context.NguoiDungs.Update(admin);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        public IActionResult Settings() => View();

        // =========================================================
        // CRUD DANH MỤC BLOG (Categories) - TÍCH HỢP TỪ DEMO 1
        // =========================================================
        public async Task<IActionResult> BlogCategories()
        {
            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            return View(categories);
        }

        public IActionResult CreateBlogCategory() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBlogCategory(string categoryName, string slug)
        {
            var model = new Category
            {
                CategoryName = categoryName,
                Slug = string.IsNullOrEmpty(slug) ? categoryName.ToLower().Replace(" ", "-") : slug,
                NgayTao = DateTime.Now
            };
            _context.Categories.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm danh mục thành công!";
            return RedirectToAction("BlogCategories");
        }

        public async Task<IActionResult> EditBlogCategory(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBlogCategory(int id, string categoryName, string slug)
        {
            var existing = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == id);
            if (existing == null) return NotFound();

            var model = new Category
            {
                CategoryId = id,
                CategoryName = categoryName,
                Slug = string.IsNullOrEmpty(slug) ? categoryName.ToLower().Replace(" ", "-") : slug,
                NgayTao = existing.NgayTao
            };
            _context.Categories.Update(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
            return RedirectToAction("BlogCategories");
        }

        [HttpPost, ActionName("DeleteBlogCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlogCategoryConfirmed(int id)
        {
            var hasArticles = await _context.Articles.AnyAsync(a => a.CategoryId == id);
            if (hasArticles)
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục này vì còn bài viết.";
                return RedirectToAction("BlogCategories");
            }

            var cat = await _context.Categories.FindAsync(id);
            if (cat != null)
            {
                _context.Categories.Remove(cat);
                await _context.SaveChangesAsync();
            }
            TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            return RedirectToAction("BlogCategories");
        }

        
        // -- LIST CSKH --
        public async Task<IActionResult> CSKHs()
        {
            var list = await _context.CsKhs.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(list);
        }

        // -- SHOW CREATE FORM --
        [HttpGet]
        public IActionResult CreateCSKH()
        {
            return View();
        }

        // -- HANDLE CREATE POST --
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCSKH(string username, string password, string fullName, string email, string phone)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập không được để trống.");
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Password", "Mật khẩu không được để trống.");
            }
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Kiểm tra trùng username
            var exists = await _context.CsKhs.AnyAsync(c => c.Username == username);
            if (exists)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập CSKH đã tồn tại.");
                return View();
            }

            var cskh = new CsKh
            {
                Username = username.Trim(),
                Password = password, // Note: existing login compares plain text; keep same format as seed data.
                FullName = fullName?.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.CsKhs.Add(cskh);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tạo tài khoản CSKH thành công.";
            return RedirectToAction("CSKHs");
        }

        // -- DELETE CSKH (optional) --
        [HttpGet]
        public async Task<IActionResult> DeleteCSKH(int id)
        {
            var item = await _context.CsKhs.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("DeleteCSKH")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCSKHConfirmed(int id)
        {
            var item = await _context.CsKhs.FindAsync(id);
            if (item != null)
            {
                _context.CsKhs.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa CSKH thành công.";
            }
            return RedirectToAction("CSKHs");
        }

        [HttpGet]
        public async Task<IActionResult> EditCSKH(int id)
        {
            var item = await _context.CsKhs.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCSKH(int id, string username, string password, string fullName, string email, string phone)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập không được để trống.");
            }

            var conflict = await _context.CsKhs.AnyAsync(c => c.Username == username && c.Id != id);
            if (conflict)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập CSKH đã tồn tại.");
            }

            var existing = await _context.CsKhs.FindAsync(id);
            if (existing == null) return NotFound();

            if (!ModelState.IsValid)
            {
                // return view populated with existing values (so user doesn't lose data)
                existing.Username = username;
                existing.FullName = fullName;
                existing.Email = email;
                existing.Phone = phone;
                return View(existing);
            }

            existing.Username = username.Trim();
            // Keep current password if admin leaves the password field empty.
            if (!string.IsNullOrEmpty(password))
            {
                existing.Password = password;
            }
            existing.FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
            existing.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            existing.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

            _context.CsKhs.Update(existing);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật CSKH thành công.";
            return RedirectToAction("CSKHs");
        }
    }
}