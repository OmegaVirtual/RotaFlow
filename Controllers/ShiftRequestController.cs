using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rota.Data;
using Rota.Models.Requests;
using System;
using System.Linq;
using System.Threading.Tasks;
using Rota.Helpers;
using Rota.Models.Notifications;

namespace Rota.Controllers
{
    [Authorize]
    public class ShiftRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ShiftRequestController> _logger;

        public ShiftRequestController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<ShiftRequestController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /ShiftRequest
        public async Task<IActionResult> Index(string filter = "pending")
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                _logger.LogWarning("User ID is null during Index action.");
                return Challenge();
            }

            bool isManager = User.IsInRole("Manager");

            var shiftQuery = _context.ShiftRequests.Include(r => r.User).AsQueryable();
            var holidayQuery = _context.HolidayRequests.Include(r => r.User).AsQueryable();

            if (filter == "pending")
            {
                shiftQuery = shiftQuery.Where(r =>
                    (isManager || r.UserId == userId) &&
                    r.Status == RequestStatus.Pending);

                holidayQuery = holidayQuery.Where(r =>
                    (isManager || r.UserId == userId) &&
                    r.Status == HolidayStatus.Pending);
            }
            else
            {
                shiftQuery = shiftQuery.Where(r =>
                    isManager || r.UserId == userId);

                holidayQuery = holidayQuery.Where(r =>
                    isManager || r.UserId == userId);
            }

            var shiftRequests = await shiftQuery
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            var holidayRequests = await holidayQuery
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            // 🔔 Unread notification count
            var unreadCount = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
            ViewBag.UnreadCount = unreadCount;

            ViewBag.HolidayRequests = holidayRequests;
            ViewBag.Message = TempData["Message"];
            ViewBag.CurrentFilter = filter;

            return View(shiftRequests);
        }



        // GET: /ShiftRequest/Create
        public IActionResult Create()
        {
            _logger.LogInformation("➡️ GET ShiftRequest/Create hit.");
            return View();
        }

        // POST: /ShiftRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShiftRequest request)
        {
            _logger.LogInformation("==> POST ShiftRequest/Create triggered.");

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                _logger.LogWarning("User ID is null during ShiftRequest submission.");
                return Challenge();
            }

            request.UserId = userId;
            request.Status = RequestStatus.Pending;
            request.SubmittedAt = DateTime.Now;

            try
            {
                _context.ShiftRequests.Add(request);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Shift request saved for user {UserId}", userId);
                TempData["Message"] = "✅ Shift request submitted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error saving shift request.");
                TempData["Message"] = "❌ Failed to submit shift request.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ✅ POST: /ShiftRequest/UpdateStatus
        [Authorize(Roles = "Manager")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, RequestStatus status)
        {
            var request = await _context.ShiftRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            request.Status = status;
            await _context.SaveChangesAsync();

            TempData["Message"] = $"✅ Shift request {status.ToString().ToLower()}.";

            // ✅ Try email (safe)
            try
            {
                if (!string.IsNullOrEmpty(request.User?.Email))
                {
                    string subject = $"Your shift request was {status}";
                    string body = $"Hello,\n\nYour shift request ({request.Type}) on {request.ShiftDate:dd MMM yyyy HH:mm} has been {status.ToString().ToLower()}.\n\nRegards,\nRota System";
                    await EmailSender.SendAsync(request.User.Email, subject, body);
                }
            }
            catch
            {
                // Skip email errors silently
            }

            // ✅ In-app notification
            if (!string.IsNullOrEmpty(request.UserId))
            {
                var message = $"Your shift request on {request.ShiftDate:dd MMM yyyy HH:mm} was {status.ToString().ToLower()}.";
                var notification = new Notification
                {
                    UserId = request.UserId,
                    Message = message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
