using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rota.Data;
using Rota.Models.Notifications;
using Rota.Models.Requests;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rota.Controllers
{
    [Authorize]
    public class HolidayRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HolidayRequestController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: HolidayRequest/Create
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);

            var balanceEntry = await _context.UserHolidayBalances
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (balanceEntry == null)
            {
                balanceEntry = new UserHolidayBalance
                {
                    UserId = userId,
                    AnnualAllowance = 28,
                    UsedDays = 0
                };

                _context.UserHolidayBalances.Add(balanceEntry);
                await _context.SaveChangesAsync();
            }

            int remaining = balanceEntry.AnnualAllowance - balanceEntry.UsedDays;
            ViewBag.HolidayBalance = remaining;

            var today = DateTime.Today;
            var request = new HolidayRequest
            {
                StartDate = today,
                EndDate = today.AddDays(1)
            };

            return View(request);
        }

        // POST: HolidayRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HolidayRequest request)
        {
            request.UserId = _userManager.GetUserId(User);
            ModelState.Remove(nameof(request.UserId));
            ModelState.Remove(nameof(request.User));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Message"] = "❌ Submission failed: " + string.Join(" | ", errors);
                return View(request);
            }

            request.SubmittedAt = DateTime.UtcNow;
            request.Status = HolidayStatus.Pending;

            _context.HolidayRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Message"] = "✅ Holiday request submitted.";
            return RedirectToAction("Index", "Home");
        }

        // ✅ POST: HolidayRequest/UpdateStatus
        [Authorize(Roles = "Manager")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, HolidayStatus status, string? managerComment)
        {
            var request = await _context.HolidayRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            request.Status = status;

            if (!string.IsNullOrWhiteSpace(managerComment))
            {
                // Ensure ManagerComment field exists in the model & DB
                request.ManagerComment = managerComment;
            }

            await _context.SaveChangesAsync();

            // ✅ Build in-app notification
            var message = $"Your holiday request from {request.StartDate:dd MMM} to {request.EndDate:dd MMM} was {status.ToString().ToLower()}.";

            if (!string.IsNullOrWhiteSpace(managerComment))
            {
                message += $" Note: {managerComment}";
            }

            if (!string.IsNullOrEmpty(request.UserId))
            {
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

            // ✅ Optional email
            try
            {
                string subject = $"Your holiday request was {status}";
                string body = $"Hello,\n\nYour holiday request from {request.StartDate:dd MMM yyyy} to {request.EndDate:dd MMM yyyy} has been {status.ToString().ToLower()}.";

                if (!string.IsNullOrWhiteSpace(managerComment))
                {
                    body += $"\n\nManager note: {managerComment}";
                }

                body += "\n\nRegards,\nTimeNest System";

                await Rota.Helpers.EmailSender.SendAsync(request.User?.Email, subject, body);
            }
            catch
            {
                // Fail silently
            }

            TempData["Message"] = $"✅ Holiday request {status.ToString().ToLower()}.";
            return RedirectToAction("Index", "ShiftRequest");
        }
    }
}
