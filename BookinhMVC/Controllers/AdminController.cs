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


//update login
namespace BookinhMVC.Controllers
{
    public class AdminController : Controller
    {
        private readonly BookingContext _context;

        public AdminController(BookingContext context) => _context = context;

        // 1. MIDDLEWARE: KIỂM TRA QUYỀN ADMIN CHO MỌI ACTION
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var actionName = context.ActionDescriptor.RouteValues["action"]?.ToString();
            if (actionName == "Login" || actionName == "Logout")
            {
                base.OnActionExecuting(context);
                return;
            }

            var role = HttpContext.Session.GetString("UserRole");
            // Kiểm tra: Phải có Role và Role phải là "Admin"
            if (string.IsNullOrEmpty(role) || role != "Admin")
            {
                context.Result = RedirectToAction("Login", "Admin");
                return;
            }
            base.OnActionExecuting(context);
        }

        // 2. AUTHENTICATION (LOGIN / LOGOUT)
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var admin = await _context.NguoiDungs
                .FirstOrDefaultAsync(u => u.TenDangNhap == username && u.VaiTro == "Admin");

            var hasher = new PasswordHasher<NguoiDung>();
            if (admin != null && hasher.VerifyHashedPassword(admin, admin.MatKhau, password) == PasswordVerificationResult.Success)
            {
                // Lưu session cho Admin
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("AdminName", admin.TenDangNhap);
                HttpContext.Session.SetInt32("AdminId", admin.MaNguoiDung);
                return RedirectToAction("Index");
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
            return View();
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Admin");
        }

        

        
    }