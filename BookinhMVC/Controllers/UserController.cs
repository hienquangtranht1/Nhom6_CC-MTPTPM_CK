using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MimeKit;
using BookinhMVC.Models;
using Microsoft.AspNetCore.SignalR;
using BookinhMVC.Hubs;
using BookinhMVC.Helpers;
// update UpdateProfile 
namespace BookinhMVC.Controllers
{
    

        [HttpGet("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var patient = await _context.BenhNhans.FindAsync(userId);
            return View(patient);
        }

        [HttpPost("UpdateProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string hoTen, DateTime ngaySinh, string gioiTinh, string soDienThoai, string email, string diaChi, string soBaoHiem, IFormFile hinhAnhBenhNhan)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var patient = await _context.BenhNhans.FindAsync(userId);

            patient.HoTen = hoTen;
            patient.NgaySinh = ngaySinh;
            patient.GioiTinh = gioiTinh;
            patient.SoDienThoai = soDienThoai;
            patient.Email = email;
            patient.DiaChi = diaChi;
            patient.SoBaoHiem = soBaoHiem;

            if (hinhAnhBenhNhan != null && hinhAnhBenhNhan.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{hinhAnhBenhNhan.FileName}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await hinhAnhBenhNhan.CopyToAsync(stream);
                }
                patient.HinhAnhBenhNhan = fileName;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

        
    }

    // DTOs bên ngoài Controller
    public class RegisterRequestDto { public string Username { get; set; } public string Password { get; set; } public string Fullname { get; set; } public DateTime Dob { get; set; } public string Gender { get; set; } public string Phone { get; set; } public string Email { get; set; } public string Address { get; set; } public string SoBaoHiem { get; set; } }
    public class VerifyOtpRequestDto { public string Otp { get; set; } }
    public class LoginRequestDto { public string Username { get; set; } public string Password { get; set; } }
    public class ForgotPasswordRequestDto { public string Email { get; set; } public string Step { get; set; } public string Otp { get; set; } }
    public class ResetPasswordRequestDto { public string NewPassword { get; set; } public string ConfirmPassword { get; set; } public string Otp { get; set; } }
    public class ChangePasswordDto { public string oldPassword { get; set; } public string newPassword { get; set; } public string confirmPassword { get; set; } public string verificationCode { get; set; } }
    public class ProfileDto { public int MaBenhNhan { get; set; } public string HoTen { get; set; } public DateTime? NgaySinh { get; set; } public string GioiTinh { get; set; } public string SoDienThoai { get; set; } public string Email { get; set; } public string DiaChi { get; set; } public string SoBaoHiem { get; set; } public string HinhAnhBenhNhan { get; set; } public decimal SoDu { get; set; } }
    public class AppointmentDto { public int MaLich { get; set; } public int MaBenhNhan { get; set; } public int MaBacSi { get; set; } public DateTime NgayGio { get; set; } public string BacSiHoTen { get; set; } public string TrangThai { get; set; } public bool IsPaid { get; set; } }
    public class NotificationDto { public int Id { get; set; } public string Title { get; set; } public string Content { get; set; } public DateTime CreatedAt { get; set; } public bool IsRead { get; set; } public int? RelatedAppointmentId { get; set; } }
    public class MarkReadRequest { public int Id { get; set; } }
}