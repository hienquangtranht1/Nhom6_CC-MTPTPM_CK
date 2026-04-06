using Microsoft.AspNetCore.Mvc;
using BookinhMVC.Models;
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using QRCoder;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR; // Import SignalR
using BookinhMVC.Hubs; // Import Hub

namespace BookinhMVC.Controllers
{
    public class DoctorController : Controller
    {
        private readonly BookingContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<BookingHub> _hubContext; // Declare Hub Context

        // Inject Hub into Constructor
        public DoctorController(BookingContext context, IWebHostEnvironment env, IHubContext<BookingHub> hubContext)
        {
            _context = context;
            _env = env;
            _hubContext = hubContext;
        }

        // ===================================================================
        // 1. MIDDLEWARE & AUTHENTICATION
        // ===================================================================
        private bool IsDoctorLoggedIn()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var id = HttpContext.Session.GetInt32("DoctorId");

            if (role != "BacSi" || id == null) return false;

            var user = _context.Set<NguoiDung>().FirstOrDefault(u => u.MaNguoiDung == id && u.VaiTro == "BacSi");
            return user != null;
        }

        [HttpGet]
        public IActionResult Login() => View("Login");

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.NguoiDungs.FirstOrDefault(u => u.TenDangNhap == username && u.VaiTro == "BacSi");
            if (user != null)
            {
                var hasher = new PasswordHasher<NguoiDung>();
                var result = hasher.VerifyHashedPassword(user, user.MatKhau, password);
                if (result == PasswordVerificationResult.Success)
                {
                    HttpContext.Session.SetInt32("DoctorId", user.MaNguoiDung);
                    HttpContext.Session.SetString("UserRole", "BacSi");
                    HttpContext.Session.SetString("DoctorName", user.TenDangNhap);

                    var bacSi = _context.BacSis.FirstOrDefault(b => b.MaNguoiDung == user.MaNguoiDung);
                    if (bacSi != null)
                    {
                        HttpContext.Session.SetInt32("MaBacSi", bacSi.MaBacSi);
                        HttpContext.Session.SetString("DoctorImage", bacSi.HinhAnhBacSi ?? "default.jpg");
                    }
                    return RedirectToAction("Appointments");
                }
            }
            ViewBag.Error = "Sai tài khoản hoặc không phải bác sĩ.";
            return View("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("DoctorId");
            HttpContext.Session.Remove("UserRole");
            HttpContext.Session.Remove("DoctorName");
            HttpContext.Session.Remove("MaBacSi");
            HttpContext.Session.Remove("DoctorImage");
            return RedirectToAction("Login", "Doctor");
        }

        [HttpGet]
        public IActionResult Login() => View("Login");

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.NguoiDungs.FirstOrDefault(u => u.TenDangNhap == username && u.VaiTro == "BacSi");
            if (user != null)
            {
                var hasher = new PasswordHasher<NguoiDung>();
                var result = hasher.VerifyHashedPassword(user, user.MatKhau, password);
                if (result == PasswordVerificationResult.Success)
                {
                    HttpContext.Session.SetInt32("DoctorId", user.MaNguoiDung);
                    HttpContext.Session.SetString("UserRole", "BacSi");
                    HttpContext.Session.SetString("DoctorName", user.TenDangNhap);

                    var bacSi = _context.BacSis.FirstOrDefault(b => b.MaNguoiDung == user.MaNguoiDung);
                    if (bacSi != null)
                    {
                        HttpContext.Session.SetInt32("MaBacSi", bacSi.MaBacSi);
                        HttpContext.Session.SetString("DoctorImage", bacSi.HinhAnhBacSi ?? "default.jpg");
                    }
                    return RedirectToAction("Appointments");
                }
            }
            ViewBag.Error = "Sai tài khoản hoặc không phải bác sĩ.";
            return View("Login");
        }
        // 2. PROFILE MANAGEMENT
        // ===================================================================
        [HttpGet]
        public IActionResult Profile()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var maNguoiDung = HttpContext.Session.GetInt32("DoctorId");

            var bacSi = _context.Set<BacSi>()
                .Include(b => b.Khoa)
                .FirstOrDefault(b => b.MaNguoiDung == maNguoiDung);

            if (bacSi == null) return RedirectToAction("Login");
            return View(bacSi);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(int MaBacSi, string HoTen, string SoDienThoai, string Email, string MoTa, IFormFile profileImage)
        {
            var doctor = await _context.BacSis.FindAsync(MaBacSi);
            if (doctor == null) return RedirectToAction("Profile");

            doctor.HoTen = HoTen;
            doctor.SoDienThoai = SoDienThoai;
            doctor.Email = Email;
            doctor.MoTa = MoTa;

            if (profileImage != null && profileImage.Length > 0)
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadPath = Path.Combine(webRoot, "uploads");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(profileImage.FileName)}";
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(doctor.HinhAnhBacSi) && doctor.HinhAnhBacSi != "default.jpg")
                {
                    var oldPath = Path.Combine(uploadPath, doctor.HinhAnhBacSi);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                doctor.HinhAnhBacSi = fileName;
                HttpContext.Session.SetString("DoctorImage", fileName);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }
        // 4. WORK SCHEDULE & MEDICAL RECORDS
        // ===================================================================
        public IActionResult WorkSchedule(DateTime? weekStart)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var maBacSi = HttpContext.Session.GetInt32("MaBacSi");
            var start = weekStart ?? DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            var end = start.AddDays(6);

            var schedules = _context.Set<LichLamViec>()
                .Where(l => l.MaBacSi == maBacSi && l.NgayLamViec >= start && l.NgayLamViec <= end)
                .ToList();

            ViewBag.WeekStart = start;
            ViewBag.WeekEnd = end;
            return View("WorkSchedule", schedules);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateWorkSchedule(int Id, string Ngay, string GioBatDau, string GioKetThuc, string actionType)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var maBacSi = HttpContext.Session.GetInt32("MaBacSi");
            DateTime date = DateTime.Parse(Ngay);
            TimeSpan start = TimeSpan.Parse(GioBatDau);
            TimeSpan end = TimeSpan.Parse(GioKetThuc);

            if (actionType == "delete")
            {
                var s = await _context.Set<LichLamViec>().FindAsync(Id);
                if (s != null) _context.Remove(s);
            }
            else if (actionType == "add")
            {
                var lich = new LichLamViec { MaBacSi = maBacSi.Value, NgayLamViec = date, GioBatDau = start, GioKetThuc = end, TrangThai = "Chờ xác nhận", NgayTao = DateTime.Now, ThuTrongTuan = ((int)date.DayOfWeek == 0 ? "8" : ((int)date.DayOfWeek + 1).ToString()) };
                _context.Add(lich);
            }
            else if (actionType == "update")
            {
                var s = await _context.Set<LichLamViec>().FindAsync(Id);
                if (s != null) { s.GioBatDau = start; s.GioKetThuc = end; s.TrangThai = "Chờ xác nhận"; }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("WorkSchedule", new { weekStart = date.AddDays(-(int)date.DayOfWeek + 1).ToString("yyyy-MM-dd") });
        }

        public IActionResult MedicalRecords(string search)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var maBacSi = HttpContext.Session.GetInt32("MaBacSi");

            var query = _context.Set<HoSoBenhAn>()
                .Include(h => h.BenhNhan)
                .Include(h => h.BacSi)
                .Where(h => h.MaBacSi == maBacSi);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(h => h.BenhNhan.HoTen.Contains(search));

            var records = query.Select(h => new MedicalRecordViewModel
            {
                MaHoSo = h.MaHoSo,
                TenBenhNhan = h.BenhNhan.HoTen,
                TenBacSi = h.BacSi.HoTen,
                NgayKham = h.NgayKham,
                ChanDoan = h.ChanDoan,
                PhuongAnDieuTri = h.PhuongAnDieuTri
            }).ToList();

            return View("MedicalRecords", records);
        }
    }
    }
}