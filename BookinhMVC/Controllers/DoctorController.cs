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

        
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("DoctorId");
            HttpContext.Session.Remove("UserRole");
            HttpContext.Session.Remove("DoctorName");
            HttpContext.Session.Remove("MaBacSi");
            HttpContext.Session.Remove("DoctorImage");
            return RedirectToAction("Login", "Doctor");
        }

        // ===================================================================
       // 2. PROFILE MANAGEMENT
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var maNguoiDung = HttpContext.Session.GetInt32("DoctorId");
            var maBacSi = HttpContext.Session.GetInt32("MaBacSi");

            var bacSi = _context.Set<BacSi>()
                .Include(b => b.Khoa)
                .FirstOrDefault(b => b.MaNguoiDung == maNguoiDung);

            if (bacSi == null) return RedirectToAction("Login");

            // Load statistics
            if (maBacSi.HasValue)
            {
                var today = DateTime.Today;
                var appointmentsToday = await _context.LichHens
                    .CountAsync(l => l.MaBacSi == maBacSi.Value && l.NgayGio.Date == today);
                
                var pendingQuestions = await _context.Questions
                    .CountAsync(q => q.DoctorId == maBacSi.Value && string.IsNullOrEmpty(q.Answer));
                
                var totalAnswered = await _context.Questions
                    .CountAsync(q => q.DoctorId == maBacSi.Value && !string.IsNullOrEmpty(q.Answer));

                ViewBag.AppointmentsToday = appointmentsToday;
                ViewBag.PendingQuestions = pendingQuestions;
                ViewBag.TotalAnswered = totalAnswered;
            }

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

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestChangePasswordOtp()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (!IsDoctorLoggedIn()) return Json(new { success = false, message = "Chưa đăng nhập." });

            var bacSi = await _context.BacSis.FirstOrDefaultAsync(b => b.MaNguoiDung == doctorId);

            if (bacSi == null || string.IsNullOrEmpty(bacSi.Email)) return Json(new { success = false, message = "Không tìm thấy email." });

            string otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("ChangePasswordOtp", otp);
            HttpContext.Session.SetString("ChangePasswordOtpTime", DateTime.Now.ToString());

            try
            {
                var smtpUser = "hienquangtranht1@gmail.com";
                var smtpPass = "aigh nsyp dgyu emhc";
                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };
                var mail = new MailMessage
                {
                    From = new MailAddress(smtpUser, "BỆNH VIỆN FOUR_ROCK"),
                    Subject = "OTP Đổi mật khẩu",
                    Body = $"Mã OTP của bạn là: {otp}"
                };
                mail.To.Add(bacSi.Email);
                await smtp.SendMailAsync(mail);
                return Json(new { success = true, message = "Đã gửi OTP." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi gửi mail: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword, string verificationCode)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            var doctor = await _context.NguoiDungs.FindAsync(doctorId);

            var hasher = new PasswordHasher<NguoiDung>();
            if (hasher.VerifyHashedPassword(doctor, doctor.MatKhau, oldPassword) != PasswordVerificationResult.Success)
            {
                TempData["cp_message"] = "Mật khẩu cũ sai.";
                return RedirectToAction("ChangePassword");
            }

            var sessionOtp = HttpContext.Session.GetString("ChangePasswordOtp");
            if (verificationCode != sessionOtp)
            {
                TempData["cp_message"] = "Mã OTP sai.";
                return RedirectToAction("ChangePassword");
            }

            doctor.MatKhau = hasher.HashPassword(doctor, newPassword);
            await _context.SaveChangesAsync();
            TempData["cp_message"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("ChangePassword");
        }

        

        // ===================================================================
        // 3. APPOINTMENTS (MVC) - TÍCH HỢP SIGNALR
        // ===================================================================
       

        [HttpPost]
        public async Task<IActionResult> UpdateAppointment(int id, string status)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");

            var appointment = await _context.Set<LichHen>()
                .Include(l => l.BenhNhan)
                .Include(l => l.BacSi)
                .FirstOrDefaultAsync(l => l.MaLich == id);

            if (appointment != null)
            {
                // Prevent cancellation when appointment already paid
                if (string.Equals(status?.Trim(), "Đã hủy", StringComparison.OrdinalIgnoreCase))
                {
                    var hasPaid = await _context.GiaoDichThanhToan
                        .AnyAsync(g => g.MaLich == appointment.MaLich
                                       && g.MaBenhNhan == appointment.MaBenhNhan
                                       && g.LoaiGiaoDich == "Thanh toán lịch hẹn"
                                       && g.TrangThai == "Thành công");

                    if (hasPaid)
                    {
                        TempData["Error"] = "Lịch này đã được thanh toán nên không thể hủy.";

                        // Notify the doctor (target the doctor's user account) so their other devices / sessions also get a realtime notification
                        var doctorUserId = appointment.BacSi?.MaNguoiDung;
                        if (doctorUserId != null)
                        {
                            var notifDoctor = new ThongBao
                            {
                                MaNguoiDung = doctorUserId.Value,
                                TieuDe = "Không thể hủy - Đã thanh toán",
                                NoiDung = $"Lịch #{appointment.MaLich} đã được thanh toán và không thể hủy.",
                                NgayTao = DateTime.Now,
                                MaLichHen = appointment.MaLich,
                                DaXem = false
                            };
                            _context.Add(notifDoctor);
                            await _context.SaveChangesAsync();

                            await _hubContext.Clients.Group($"User_{doctorUserId.Value}").SendAsync("NewNotification", new
                            {
                                id = notifDoctor.MaThongBao,
                                title = notifDoctor.TieuDe,
                                content = notifDoctor.NoiDung,
                                createdAt = notifDoctor.NgayTao,
                                appointmentId = notifDoctor.MaLichHen
                            });
                        }

                        return RedirectToAction("Appointments", new { date = appointment.NgayGio.ToString("yyyy-MM-dd") });
                    }
                }

                bool isConfirmedNow = (appointment.TrangThai != "Đã xác nhận" && status == "Đã xác nhận");
                appointment.TrangThai = status;

                // 1. Lưu trạng thái lịch hẹn
                // 2. Tạo thông báo (ThongBao) để lưu vào Database
                var notif = new ThongBao
                {
                    MaNguoiDung = appointment.MaBenhNhan,
                    TieuDe = isConfirmedNow ? "Lịch hẹn đã được xác nhận" : $"Trạng thái lịch hẹn: {status}",
                    NoiDung = $"BS {appointment.BacSi.HoTen} đã chuyển trạng thái lịch khám {appointment.NgayGio:HH:mm dd/MM} sang: {status}.",
                    NgayTao = DateTime.Now,
                    DaXem = false,
                    MaLichHen = appointment.MaLich
                };
                _context.Add(notif);

                await _context.SaveChangesAsync();

                // 3. 🚀 BẮN SIGNALR REAL-TIME CHO BỆNH NHÂN
                // Gửi sự kiện 'ReceiveStatusChange' tới nhóm User_{MaBenhNhan}
                await _hubContext.Clients.Group($"User_{appointment.MaBenhNhan}").SendAsync("ReceiveStatusChange", new
                {
                    maLich = appointment.MaLich,
                    trangThaiMoi = status,
                    tieuDe = notif.TieuDe,
                    noiDung = notif.NoiDung
                });

                // Gửi email
                string patientEmail = appointment.BenhNhan.Email;
                if (!string.IsNullOrEmpty(patientEmail))
                {
                    try
                    {
                        if (isConfirmedNow)
                        {
                            string qrData = $"LichHen:{appointment.MaLich}|BN:{appointment.BenhNhan.HoTen}|BS:{appointment.BacSi.HoTen}|Gio:{appointment.NgayGio}";
                            string qrBase64 = GenerateQrCodeAsBase64(qrData);
                            await SendConfirmationEmailAsync(patientEmail, appointment.BenhNhan.HoTen, appointment.BacSi.HoTen, appointment.NgayGio.ToString(), qrBase64);
                        }
                        else
                        {
                            await SendStatusUpdateEmailAsync(patientEmail, appointment.BenhNhan.HoTen, appointment.BacSi.HoTen, appointment.NgayGio.ToString(), status);
                        }
                    }
                    catch { /* Bỏ qua lỗi email để không crash */ }
                }

                // Send NewNotification (so web clients update their notification list)
                await _hubContext.Clients.Group($"User_{appointment.MaBenhNhan}").SendAsync("NewNotification", new {
                    id = notif.MaThongBao,
                    title = notif.TieuDe,
                    content = notif.NoiDung,
                    createdAt = notif.NgayTao,
                    appointmentId = notif.MaLichHen
                });
            }
            return RedirectToAction("Appointments", new { date = appointment?.NgayGio.ToString("yyyy-MM-dd") });
        }

        // ===================================================================
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

       

        // ===================================================================
        // 5. API ACTIONS
        // ===================================================================

        // GET: api/doctors?page=1&pageSize=10&search=abc
        [HttpGet]
        [Route("api/doctors")]
        public async Task<IActionResult> GetDoctors(int page = 1, int pageSize = 10, string search = "")
        {
            var query = _context.BacSis
                .Include(d => d.Khoa)
                .Where(d => !string.IsNullOrEmpty(d.HoTen));

            // 1. Filter by Name or Specialty
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower().Trim();
                query = query.Where(d => d.HoTen.ToLower().Contains(search) ||
                                         (d.Khoa != null && d.Khoa.TenKhoa.ToLower().Contains(search)));
            }

            // 2. Pagination
            var totalItems = await query.CountAsync();
            var doctors = await query
                .OrderByDescending(d => d.MaBacSi) // Sort new doctors first
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 3. Map Data
            var result = doctors.Select(d => new
            {
                id = d.MaBacSi,
                name = d.HoTen,
                specialty = d.Khoa?.TenKhoa ?? "Chuyên khoa khác",
                imageUrl = BuildRelativeImagePathIfExists(d.MaBacSi, d.HinhAnhBacSi),
                phone = d.SoDienThoai
            }).ToList();

            return Ok(new
            {
                data = result,
                total = totalItems,
                page = page,
                hasNext = (page * pageSize) < totalItems
            });
        }

        [HttpGet]
        [Route("api/doctors/featured")]
        public async Task<IActionResult> GetFeaturedDoctors()
        {
            var list = await _context.BacSis.Include(d => d.Khoa).Where(d => !string.IsNullOrEmpty(d.HoTen)).OrderBy(d => Guid.NewGuid()).Take(6).ToListAsync();
            var result = list.Select(d =>
            {
                var imgPath = BuildRelativeImagePathIfExists(d.MaBacSi, d.HinhAnhBacSi);
                return new { id = d.MaBacSi, name = d.HoTen, specialty = d.Khoa?.TenKhoa ?? "", phone = d.SoDienThoai, email = d.Email, description = d.MoTa, imageUrl = imgPath };
            }).ToList();
            return Ok(result);
        }

        [HttpGet]
        [Route("api/doctors/{id}")]
        public async Task<IActionResult> GetDoctor(int id)
        {
            var d = await _context.BacSis.Include(b => b.Khoa).FirstOrDefaultAsync(b => b.MaBacSi == id);
            if (d == null) return NotFound();
            var imgPath = BuildRelativeImagePathIfExists(d.MaBacSi, d.HinhAnhBacSi);
            var result = new { id = d.MaBacSi, name = d.HoTen, specialty = d.Khoa?.TenKhoa ?? "", phone = d.SoDienThoai, email = d.Email, description = d.MoTa, imageUrl = imgPath };
            return Ok(result);
        }

        private string? BuildRelativeImagePathIfExists(int maBacSi, string? storedFileName)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads");
            if (!Directory.Exists(uploadsFolder)) return null;
            if (!string.IsNullOrEmpty(storedFileName))
            {
                var path = Path.Combine(uploadsFolder, storedFileName);
                if (System.IO.File.Exists(path)) return $"/uploads/{storedFileName}";
            }
            var exts = new[] { ".png", ".jpg", ".jpeg" };
            foreach (var ext in exts)
            {
                var file = Path.Combine(uploadsFolder, $"{maBacSi}{ext}");
                if (System.IO.File.Exists(file)) return $"/uploads/{maBacSi}{ext}";
            }
            return null;
        }

        // ===================================================================
        // 6. QA & HELPERS (EMAIL, QR)
        // ===================================================================
        private string GenerateQrCodeAsBase64(string text)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeBytes);
        }

        private async Task SendConfirmationEmailAsync(string toEmail, string patientName, string doctorName, string time, string qrBase64)
        {
            var smtpUser = "hienquangtranht1@gmail.com";
            var smtpPass = "aigh nsyp dgyu emhc";
            var mail = new MailMessage();
            mail.From = new MailAddress(smtpUser, "Four Rock Hospital");
            mail.To.Add(toEmail);
            mail.Subject = "Xác nhận lịch hẹn";
            mail.IsBodyHtml = true;
            string htmlBody = $"<h3>Xin chào {patientName},</h3><p>Lịch hẹn với BS {doctorName} lúc {time} đã được xác nhận.</p><img src='cid:qrImage' style='width:200px;'/>";
            var view = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");
            var qrBytes = Convert.FromBase64String(qrBase64);
            var linked = new LinkedResource(new MemoryStream(qrBytes), "image/png") { ContentId = "qrImage" };
            view.LinkedResources.Add(linked);
            mail.AlternateViews.Add(view);
            using var smtp = new SmtpClient("smtp.gmail.com", 587) { Credentials = new NetworkCredential(smtpUser, smtpPass), EnableSsl = true };
            await smtp.SendMailAsync(mail);
        }

        private async Task SendStatusUpdateEmailAsync(string toEmail, string patientName, string doctorName, string time, string status)
        {
            var smtpUser = "hienquangtranht1@gmail.com";
            var smtpPass = "aigh nsyp dgyu emhc";
            using var smtp = new SmtpClient("smtp.gmail.com", 587) { Credentials = new NetworkCredential(smtpUser, smtpPass), EnableSsl = true };
            var mail = new MailMessage { From = new MailAddress(smtpUser, "Four Rock Hospital"), Subject = $"Cập nhật lịch hẹn: {status}", Body = $"Xin chào {patientName},\nLịch hẹn với BS {doctorName} lúc {time} đã chuyển sang trạng thái: {status}.", IsBodyHtml = false };
            mail.To.Add(toEmail);
            await smtp.SendMailAsync(mail);
        }

        [HttpGet]
        public async Task<IActionResult> Question()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");

            var maBacSi = HttpContext.Session.GetInt32("MaBacSi");

            // Lấy danh sách câu hỏi gửi đến bác sĩ này, bao gồm cả thông tin người hỏi
            var questions = await _context.Questions
                .Include(q => q.User) // Include User để lấy tên bệnh nhân
                .Where(q => q.DoctorId == maBacSi)
                .OrderByDescending(q => q.CreatedAt) // Mới nhất lên đầu
                .ToListAsync();

            return View(questions);
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

        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("Login");

            var question = await _context.Questions.FindAsync(questionId);
            if (question != null && question.DoctorId == HttpContext.Session.GetInt32("MaBacSi"))
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa câu hỏi thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể xóa câu hỏi này.";
            }

            return RedirectToAction("Question");
        }
    }
}