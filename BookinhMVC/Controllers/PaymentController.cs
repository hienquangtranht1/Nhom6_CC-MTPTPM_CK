using BookinhMVC.Models;
using BookinhMVC.Helpers; // Thư viện VNPAY
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using BookinhMVC.Hubs; // Import Hub

namespace BookinhMVC.Controllers
{
    [Route("[controller]")]
    public class PaymentController : Controller
    {
        private readonly BookingContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<BookingHub> _hubContext; // Inject Hub

        public PaymentController(BookingContext context, IConfiguration configuration, IHubContext<BookingHub> hubContext)
        {
            _context = context;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        // ============================================================
        // 1. TRANG QUẢN LÝ VÍ (HIỂN THỊ SỐ DƯ)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "User");

            // Lấy hoặc tạo mới ví
            var wallet = await _context.TaiKhoanBenhNhan.FirstOrDefaultAsync(x => x.MaBenhNhan == userId);
            if (wallet == null)
            {
                wallet = new TaiKhoanBenhNhan { MaBenhNhan = userId.Value, SoDuHienTai = 0, NgayCapNhatCuoi = DateTime.Now };
                _context.TaiKhoanBenhNhan.Add(wallet);
                await _context.SaveChangesAsync();
            }

            ViewBag.CurrentBalance = wallet.SoDuHienTai;
            return View();
        }

        // ============================================================
        // 2. TRANG LỊCH SỬ GIAO DỊCH
        // ============================================================

        [HttpGet("History")]
        public async Task<IActionResult> History()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "User");

            var history = await _context.GiaoDichThanhToan
                                        .Where(x => x.MaBenhNhan == userId)
                                        .OrderByDescending(x => x.NgayGiaoDich)
                                        .ToListAsync();

            return View(history);
        }
        // 7. CALLBACK VNPAY (CẬP NHẬT TRẠNG THÁI + SIGNALR)
        //    => Redirect về trang Lịch sử giao dịch (History)
        // ============================================================
        [HttpGet("PaymentCallback")]
        public async Task<IActionResult> PaymentCallback()
        {
            if (Request.Query.Count == 0) return RedirectToAction("Index");

            string vnp_HashSecret = _configuration["VnPay:HashSecret"];
            var vnpayData = Request.Query;
            VnPayLibrary vnpay = new VnPayLibrary();

            foreach (var s in vnpayData)
            {
                if (!string.IsNullOrEmpty(s.Key) && s.Key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(s.Key, s.Value);
                }
            }

            long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
            long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_SecureHash = vnpay.GetResponseData("vnp_SecureHash");

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (!checkSignature)
            {
                TempData["Error"] = "Xác thực chữ ký VNPAY thất bại.";
                return RedirectToAction("History");
            }

            var trans = await _context.GiaoDichThanhToan.FirstOrDefaultAsync(x => x.MaThamChieu == orderId.ToString());
            if (trans == null)
            {
                TempData["Error"] = "Không tìm thấy giao dịch.";
                return RedirectToAction("History");
            }

            if (vnp_ResponseCode == "00")
            {
                // Cập nhật trạng thái trước, sau đó cộng tiền (idempotent)
                trans.TrangThai = "Thành công";
                trans.NgayCapNhat = DateTime.Now;
                await _context.SaveChangesAsync();

                await ProcessAddMoneyToWallet(trans);

                // Gửi SignalR thông báo realtime cho user
                await _hubContext.Clients.Group($"User_{trans.MaBenhNhan}").SendAsync("ReceiveStatusChange", new
                {
                    Type = "BalanceUpdate",
                    Message = "Nạp tiền thành công! Số dư đã cập nhật."
                });

                TempData["Success"] = $"Nạp tiền thành công: {vnp_Amount:N0}đ.";
            }
            else
            {
                trans.TrangThai = "Thất bại";
                trans.NgayCapNhat = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["Error"] = $"Giao dịch thất bại (code: {vnp_ResponseCode}).";
            }

            // Redirect về trang Lịch sử giao dịch để người dùng xem kết quả
            return RedirectToAction("History");
        }

            // 2. Tạo URL VNPAY
            string vnp_Returnurl = _configuration["VnPay:ReturnUrl"];
            string vnp_Url = _configuration["VnPay:BaseUrl"];
            string vnp_TmnCode = _configuration["VnPay:TmnCode"];
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];

            VnPayLibrary vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)req.Amount * 100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Nap tien App " + uniqueCode);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", uniqueCode);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            return Ok(new { success = true, paymentUrl = paymentUrl });
        }

        public class DepositRequest { public decimal Amount { get; set; } }
    }
}