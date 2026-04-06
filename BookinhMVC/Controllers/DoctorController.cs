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
    [HttpPost]
    public async Task<IActionResult> Answer(int questionId, string answer)
    {
        if (!IsDoctorLoggedIn()) return RedirectToAction("Login");

        var q = await _context.Questions.FindAsync(questionId);
        if (q != null)
        {
            q.Answer = answer;
            q.Status = "Đã trả lời";
            q.AnsweredAt = DateTime.Now; // Lưu thời gian trả lời

            _context.Questions.Update(q);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi câu trả lời thành công!";
        }
        else
        {
            TempData["Error"] = "Không tìm thấy câu hỏi.";
        }

        // Redirect về trang Question
        return RedirectToAction("Question");
    }
}
    