using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rota.Data;
using Rota.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rota.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ReportsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            var shifts = _context.ShiftAssignments
                .Include(sa => sa.User)
                .Include(sa => sa.Shift)
                .ToList();

            var shiftGroups = shifts
                .GroupBy(s => s.User.UserName)
                .Select(g => new { User = g.Key, ShiftCount = g.Count() })
                .ToList();

            ViewBag.TotalShifts = shifts.Count;
            ViewBag.ShiftLabels = shiftGroups.Select(g => g.User).ToList();
            ViewBag.ShiftData = shiftGroups.Select(g => g.ShiftCount).ToList();

            var approvedHolidays = _context.HolidayRequests
                .Include(r => r.User)
                .Where(r => r.Status == HolidayStatus.Approved)
                .ToList();

            var holidayUsage = approvedHolidays
                .GroupBy(r => r.User.UserName)
                .Select(g => new
                {
                    User = g.Key,
                    Used = g.Sum(r => (r.EndDate - r.StartDate).Days + 1),
                    Remaining = 28 - g.Sum(r => (r.EndDate - r.StartDate).Days + 1),
                    LastDate = g.Max(r => r.EndDate)
                }).ToList();

            ViewBag.HolidayUsageLabels = holidayUsage.Select(h => h.User).ToList();
            ViewBag.HolidayUsedData = holidayUsage.Select(h => h.Used).ToList();
            ViewBag.HolidayRemainingData = holidayUsage.Select(h => h.Remaining).ToList();
            ViewBag.LastHolidayDate = holidayUsage.Select(h => h.LastDate.ToString("yyyy-MM-dd")).ToList();
            ViewBag.TotalHolidays = holidayUsage.Sum(h => h.Used);

            var weekdayData = shifts
                .GroupBy(s => s.Shift.StartTime.DayOfWeek)
                .OrderBy(g => g.Key)
                .Select(g => new { Day = g.Key.ToString(), Count = g.Count() })
                .ToList();

            ViewBag.WeekdayLabels = weekdayData.Select(d => d.Day).ToList();
            ViewBag.WeekdayCounts = weekdayData.Select(d => d.Count).ToList();

            int userCount = shifts.Select(s => s.User.Id).Distinct().Count();
            ViewBag.AvgShiftsPerUser = userCount > 0 ? Math.Round((double)shifts.Count / userCount, 1) : 0;

            var topUsers = shiftGroups
                .OrderByDescending(s => s.ShiftCount)
                .Take(3)
                .ToList();
            ViewBag.TopUsers = topUsers;

            var lowBalanceUsers = holidayUsage
                .Where(h => h.Remaining < 5)
                .ToList();
            ViewBag.LowBalanceUsers = lowBalanceUsers;

            var allUsers = _context.Users.ToList();
            var roles = _context.Roles.ToList();
            var userRoles = _context.UserRoles.ToList();

            var userRoleMap = userRoles
                .GroupBy(r => r.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(r => roles.FirstOrDefault(role => role.Id == r.RoleId)?.Name)
                          .Where(rn => rn != null)
                          .ToList()
                );

            var userRolesExpanded = allUsers.Select(u => new
            {
                u.UserName,
                u.Id,
                Roles = userRoleMap.ContainsKey(u.Id) ? userRoleMap[u.Id] : new List<string>()
            }).ToList();

            var shiftsPerRole = userRolesExpanded
                .SelectMany(u => u.Roles.Select(r => new
                {
                    Role = r,
                    Count = shifts.Count(s => s.User.Id == u.Id)
                }))
                .GroupBy(x => x.Role)
                .Select(g => new { Role = g.Key, Avg = g.Any() ? g.Average(x => x.Count) : 0 })
                .ToList();

            var holidaysPerRole = userRolesExpanded
                .SelectMany(u => u.Roles.Select(r => new
                {
                    Role = r,
                    Used = holidayUsage.FirstOrDefault(h => h.User == u.UserName)?.Used ?? 0
                }))
                .GroupBy(x => x.Role)
                .Select(g => new { Role = g.Key, AvgUsed = g.Any() ? g.Average(x => x.Used) : 0 })
                .ToList();

            ViewBag.ShiftsPerRoleLabels = shiftsPerRole.Select(r => r.Role).ToList();
            ViewBag.ShiftsPerRoleAverages = shiftsPerRole.Select(r => Math.Round(r.Avg, 1)).ToList();
            ViewBag.HolidaysPerRoleAverages = holidaysPerRole.Select(r => Math.Round(r.AvgUsed, 1)).ToList();

            // Under 18s Report
            var cutoffDate = DateTime.Today.AddYears(-18);
            var under18Profiles = _context.UserProfiles
                .Where(p => p.DateOfBirth > cutoffDate)
                .ToList();


            var under18Stats = under18Profiles
                .Select(p =>
                {
                    var userShifts = shifts.Where(s => s.User.Id == p.UserId).ToList();
                    double totalHours = userShifts.Sum(s => (s.Shift.EndTime - s.Shift.StartTime).TotalHours);

                    return new
                    {
                        User = p.FullName,
                        HoursWorked = Math.Round(totalHours, 1)
                    };
                }).ToList();

            ViewBag.Under18Count = under18Stats.Count;
            ViewBag.Under18Names = under18Stats.Select(u => u.User).ToList();
            ViewBag.Under18Hours = under18Stats.Select(u => u.HoursWorked).ToList();

            return View();
        }
    }
}
