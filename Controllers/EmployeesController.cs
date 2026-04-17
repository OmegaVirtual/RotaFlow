using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rota.Data;
using Rota.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rota.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public EmployeesController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // 🔸 UNIVERSAL VIEW: My Schedule & Rota (for Worker or Manager)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            var assignedShiftIds = await _context.ShiftAssignments
                .Where(sa => sa.UserId == userId)
                .Select(sa => sa.ShiftId)
                .ToListAsync();

            var shifts = await _context.Shifts
                .Where(s => assignedShiftIds.Contains(s.Id))
                .ToListAsync();

            ViewBag.UserName = user?.UserName;

            // ✅ This enables the "Who’s working with me" section
            ViewBag.Assignments = await _context.ShiftAssignments
                .Include(sa => sa.User)
                .ToListAsync();

            return View(shifts);
        }

        // 🔸 MANAGER VIEW: See all employees
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Manage()
        {
            var users = _userManager.Users.ToList();

            var userRoles = new Dictionary<string, string>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "Worker";
            }

            var rotaShifts = await _context.Shifts
                .Where(s => s.Notes != null && s.Notes.ToLower().Contains("auto-generated"))
                .ToListAsync();

            ViewBag.UserRoles = userRoles;
            ViewBag.RotaShifts = rotaShifts;

            return View(users);
        }

        // 🔸 MANAGER ACTION: Delete user
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("Manage");
        }
    }
}
