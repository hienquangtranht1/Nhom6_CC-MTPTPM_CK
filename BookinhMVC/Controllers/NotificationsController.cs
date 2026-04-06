using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookinhMVC.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace BookinhMVC.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(BookingContext db) : ControllerBase
{
    // GET: api/notifications/list?take=10
    [HttpGet("list")]
    public async Task<IActionResult> List(int take = 20, CancellationToken ct = default)
    {
        /* 
         * LỖI CODE CŨ: Không kiểm tra null một cách tối ưu, code rườm rà.
         * KHẮC PHỤC: Sử dụng Pattern Matching (is not { } userId) gọn gàng hơn.
         */
        if (HttpContext.Session.GetInt32("UserId") is not { } userId)
            return Unauthorized();

        /*
         * LỖI CODE CŨ: 
         * 1. Thiếu AsNoTracking() khiến API GET chậm đi vì Entity Framework phải theo dõi (track) sự thay đổi của các đối tượng lấy lên bộ nhớ.
         * 2. Thiếu CancellationToken, khi người dùng huỷ request (đóng tab/chuyển trang), server vẫn đang query vào database.
         * 3. Trả về Anonymous object (new { id = ..., title = ... }) gây khó khăn cho việc maintain và sử dụng mạnh kiểu (strong-typed).
         * 
         * KHẮC PHỤC: 
         * - Thêm AsNoTracking().
         * - Truyền `ct` (CancellationToken) vào ToListAsync.
         * - Sử dụng record NotificationDto.
         */
        var list = await db.ThongBaos
            .AsNoTracking()
            .Where(t => t.MaNguoiDung == userId)
            .OrderByDescending(t => t.NgayTao)
            .Take(take)
            .Select(t => new NotificationDto(
                t.MaThongBao,
                t.TieuDe,
                t.NoiDung,
                t.NgayTao,
                t.DaXem,
                t.MaLichHen))
            .ToListAsync(ct);

        return Ok(new { success = true, data = list });
    }

    // GET: api/notifications/count
    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct = default)
    {
        if (HttpContext.Session.GetInt32("UserId") is not { } userId)
            return Ok(new { success = false, unread = 0 });

        /*
         * LỖI CODE CŨ: Tương tự hàm List, CountAsync thiếu AsNoTracking và CancellationToken.
         */
        var unread = await db.ThongBaos
            .AsNoTracking()
            .CountAsync(t => t.MaNguoiDung == userId && !t.DaXem, ct);

        return Ok(new { success = true, unread });
    }

    // POST: api/notifications/markread
    [HttpPost("markread")]
    public async Task<IActionResult> MarkRead([FromBody] int id, CancellationToken ct = default)
    {
        if (HttpContext.Session.GetInt32("UserId") is not { } userId)
            return Unauthorized();

        /*
         * LỖI CODE CŨ: 
         * Dùng FirstOrDefaultAsync lấy toàn bộ đối tượng về RAM, gán bằng code C# `t.DaXem = true`, 
         * sau đó gọi SaveChangesAsync() để update => Tốn bộ nhớ và 2 lần vòng lặp giao tiếp (round-trip) với Database.
         * 
         * KHẮC PHỤC: Dùng ExecuteUpdateAsync (của EF Core 7+) sinh ra trực tiếp câu lệnh SQL "UPDATE ... SET DaXem = 1 WHERE ...", hiệu năng cực cao.
         */
        var updatedCount = await db.ThongBaos
            .Where(t => t.MaThongBao == id && t.MaNguoiDung == userId && !t.DaXem)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.DaXem, true), ct);

        if (updatedCount == 0)
        {
            var exists = await db.ThongBaos.AnyAsync(t => t.MaThongBao == id && t.MaNguoiDung == userId, ct);
            if (!exists) return NotFound();
        }

        return Ok(new { success = true });
    }

    // POST: api/notifications/markall
    [HttpPost("markall")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
    {
        if (HttpContext.Session.GetInt32("UserId") is not { } userId)
            return Unauthorized();

        /*
         * LỖI CODE CŨ ĐẶC BIỆT NGHIÊM TRỌNG (Về mặt hiệu năng): 
         * Dùng ToListAsync lấy tệp dữ liệu về rồi chạy vòng lặp foreach (foreach t in list) gán t.DaXem = true,
         * dẫn đến việc nếu một user có 1000 thông báo, Entity Framework sẽ load 1000 records lên RAM và update từng record một. 
         * Quá sức tốn kém băng thông/bộ nhớ và làm nghẽn DB nếu scale.
         * 
         * KHẮC PHỤC: Tương tự, sử dụng ExecuteUpdateAsync. Tương đương "UPDATE ThongBaos SET DaXem = 1 WHERE MaNguoiDung = UserId" trong 1 query duy nhất.
         */
        await db.ThongBaos
            .Where(t => t.MaNguoiDung == userId && !t.DaXem)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.DaXem, true), ct);

        return Ok(new { success = true });
    }
}

// Khai báo Data Transfer Object (DTO) chuẩn chỉnh, thay thế cho loại dữ liệu vô danh (anonymous type) ở bản cũ.
public record NotificationDto(
    int Id,
    string Title,
    string Content,
    DateTime? CreatedAt,
    bool IsRead,
    int? AppointmentId
);

// --- DƯỚI ĐÂY LÀ CÁC ĐOẠN CODE TEST ĐỂ LÀM BÀI TẬP BÁO CÁO GIT ---
public class GitTestAssignment 
{
    // BƯỚC 1: Sẽ dùng hàm này để tạo Commit Số 1
    public void FunctionForFirstCommit()
    {
        Console.WriteLine("Day la noi dung cua commit so 1");
    }

    // BƯỚC 2: Sẽ dùng hàm này để tạo Commit Số 2
    public void FunctionForSecondCommit()
    {
        Console.WriteLine("Day la noi dung cua commit so 2");
    }

    // BƯỚC 3: Sẽ dùng hàm này để tạo Commit Số 3
    public void FunctionForThirdCommit()
    {
        Console.WriteLine("Day la noi dung cua commit so 3");
    }
}