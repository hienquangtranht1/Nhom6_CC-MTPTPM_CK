using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using BookinhMVC.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookinhMVC.Controllers
{
    [Route("[controller]/[action]")]
    public class CSKHController : Controller
    {
        private readonly BookingContext _context;

        public CSKHController(BookingContext context)
        {
            _context = context;
        }

        

        // --- 2. Dashboard Chính ---
        [HttpGet]
        [Route("/CSKH/Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var cskhId = HttpContext.Session.GetInt32("CSKHId");
            if (cskhId == null) return RedirectToAction("Login");

            var cskh = await _context.CsKhs.FindAsync(cskhId);
            return View(cskh);
        }

        // --- 3. API Lấy lịch sử chat (Dành riêng cho CSKH) ---
        [HttpGet]
        [Route("/CSKH/GetHistory")]
        public async Task<IActionResult> GetHistory(int customerId)
        {
            var myId = HttpContext.Session.GetInt32("CSKHId");
            if (myId == null) return Unauthorized();

            var messages = await _context.ChatMessages
                .Where(m =>
                    // Tin mình gửi đi
                    (m.SenderId == myId && m.SenderRole == "CSKH" && m.ReceiverId == customerId) ||
                    // Tin khách gửi đến
                    (m.SenderId == customerId && m.SenderRole == "KhachHang" && m.ReceiverId == myId)
                )
                .OrderBy(m => m.CreatedAt)
                .Select(m => new {
                    message = m.Message,
                    isMe = (m.SenderId == myId && m.SenderRole == "CSKH"), // Xác định chủ sở hữu
                    time = m.CreatedAt.ToString("HH:mm dd/MM")
                })
                .ToListAsync();

            return Json(messages);
        }
        [HttpGet]
        public IActionResult GetListCSKHJson()
        {
            // Lấy danh sách ID đang online từ OnlineUserMap (thread-safe snapshot)
            var onlineIds = Helpers.OnlineUserMap.Snapshot().ToHashSet();

            // Chỉ lấy nhân viên có trong danh sách Online
            var list = _context.CsKhs
                .Where(c => onlineIds.Contains(c.Id))
                .Select(c => new {
                    id = c.Id,
                    fullName = c.FullName
                })
                .ToList();

            return Json(list);
        }

        [HttpGet]
        public IActionResult Ping()
        {
            return Json(new
            {
                status = "ok",
                message = "CSKHController is running",
                serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
    }
}