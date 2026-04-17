using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rota.Data;
using Rota.Models;
using Rota.Models.ViewModels;
using Rota.Models.Notifications;
using Rota.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rota.Controllers
{
    [Authorize]
    public class ShiftController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ShiftController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var shifts = await _context.Shifts.ToListAsync();
            var assignments = await _context.ShiftAssignments.Include(sa => sa.User).ToListAsync();
            ViewBag.Assignments = assignments;
            return View(shifts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var shift = await _context.Shifts.FirstOrDefaultAsync(m => m.Id == id);
            if (shift == null) return NotFound();
            return View(shift);
        }

        [Authorize(Roles = "Manager")]
        public IActionResult Create() => View();

        [Authorize(Roles = "Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,StartTime,EndTime,Location,RequiredStaff,Notes")] Shift shift)
        {
            if (ModelState.IsValid)
            {
                _context.Add(shift);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(shift);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null) return NotFound();
            return View(shift);
        }

        [Authorize(Roles = "Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,StartTime,EndTime,Location,RequiredStaff,Notes")] Shift shift)
        {
            if (id != shift.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shift);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Shifts.Any(e => e.Id == shift.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(shift);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var shift = await _context.Shifts.FirstOrDefaultAsync(m => m.Id == id);
            if (shift == null) return NotFound();
            return View(shift);
        }

        [Authorize(Roles = "Manager")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRota()
        {
            var rotaShifts = await _context.Shifts
                .Where(s => s.Notes != null && s.Notes.ToLower().Contains("auto-generated"))
                .ToListAsync();

            if (rotaShifts.Any())
            {
                var shiftIds = rotaShifts.Select(s => s.Id).ToList();
                var relatedAssignments = _context.ShiftAssignments.Where(a => shiftIds.Contains(a.ShiftId));
                _context.ShiftAssignments.RemoveRange(relatedAssignments);
                _context.Shifts.RemoveRange(rotaShifts);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "✅ Weekly rota deleted successfully.";
            return RedirectToAction(nameof(WeeklyRota));
        }

        public IActionResult Calendar() => View();

        [HttpGet]
        public async Task<IActionResult> GetCalendarShifts()
        {
            var shifts = await _context.Shifts.ToListAsync();
            var events = shifts.Select(s => new
            {
                id = s.Id,
                title = s.Name,
                start = s.StartTime.ToString("s"),
                end = s.EndTime.ToString("s"),
                url = Url.Action("Details", "Shift", new { id = s.Id })
            });
            return Json(events);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateWeekly()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, string>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "Worker";
            }

            ViewBag.Users = users;
            ViewBag.UserRoles = userRoles;
            return View(new List<WeeklyShiftInputModel>());
        }

        [Authorize(Roles = "Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWeeklyConfirmed(List<WeeklyShiftInputModel> shifts, DateTime WeekStartDate)
        {
            if (shifts == null || !shifts.Any())
            {
                TempData["Error"] = "No shifts were submitted.";
                return RedirectToAction(nameof(CreateWeekly));
            }

            // ✅ Snap WeekStartDate to Monday
            WeekStartDate = WeekStartDate.AddDays(-(int)WeekStartDate.DayOfWeek + (int)DayOfWeek.Monday);

            var approvedHolidays = await _context.HolidayRequests
                .Where(h => h.Status == HolidayStatus.Approved)
                .ToListAsync();

            var profiles = await _context.UserProfiles.ToDictionaryAsync(p => p.UserId, p => p.FullName);
            var skippedUsers = new HashSet<string>();

            foreach (var shiftInput in shifts)
            {
                if (!DateTime.TryParse(shiftInput.StartTimeDateOnly, out var datePart) ||
                    !TimeSpan.TryParse(shiftInput.StartTimeTimeOnly, out var startTime) ||
                    !TimeSpan.TryParse(shiftInput.EndTimeTimeOnly, out var endTime))
                {
                    continue;
                }

                var start = datePart.Add(startTime);
                var end = datePart.Add(endTime);
                if (end <= start) end = end.AddDays(1);

                var validUserIds = new List<string>();

                foreach (var userId in shiftInput.UserIds ?? new List<string>())
                {
                    var hasConflict = approvedHolidays.Any(h =>
                        h.UserId == userId &&
                        h.StartDate.Date <= end.Date &&
                        h.EndDate.Date >= start.Date);

                    if (!hasConflict)
                    {
                        validUserIds.Add(userId);
                    }
                    else
                    {
                        skippedUsers.Add(profiles.ContainsKey(userId) ? profiles[userId] : userId);
                    }
                }

                var shift = new Shift
                {
                    Name = shiftInput.Name,
                    StartTime = start,
                    EndTime = end,
                    Location = shiftInput.Location,
                    RequiredStaff = validUserIds.Count,
                    Notes = (shiftInput.Notes ?? "") + " (auto-generated)"
                };

                _context.Shifts.Add(shift);
                await _context.SaveChangesAsync();

                foreach (var userId in validUserIds)
                {
                    _context.ShiftAssignments.Add(new ShiftAssignment
                    {
                        ShiftId = shift.Id,
                        UserId = userId,
                        AssignedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"✅ Weekly rota starting {WeekStartDate:dd MMM yyyy} created successfully.";
            if (skippedUsers.Any())
            {
                TempData["HolidayConflicts"] = "⚠️ The following users were not assigned due to holidays: " + string.Join(", ", skippedUsers);
            }

            return RedirectToAction(nameof(WeeklyRota));
        }

        [Authorize(Roles = "Manager,Worker")]
        public async Task<IActionResult> WeeklyRota()
        {
            var profiles = await _context.UserProfiles.ToDictionaryAsync(p => p.UserId, p => p.FullName);
            ViewBag.Profiles = profiles;

            var shifts = await _context.Shifts
                .Where(s => s.Notes != null && s.Notes.ToLower().Contains("auto-generated"))
                .ToListAsync();

            var assignments = await _context.ShiftAssignments
                .Include(sa => sa.User)
                .ToListAsync();

            ViewBag.Assignments = assignments;
            return View(shifts);
        }

        [HttpPost]
        [Authorize(Roles = "Worker,Manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestShift(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            var user = await _userManager.GetUserAsync(User);
            if (shift == null || user == null) return NotFound();

            var managers = await _userManager.GetUsersInRoleAsync("Manager");
            var message = $"{user.UserName} requested to work shift \"{shift.Name}\" on {shift.StartTime:f}.";

            foreach (var manager in managers)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = manager.Id,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            TempData["RequestMessage"] = "✅ Your shift request has been sent to the manager.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
