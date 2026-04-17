using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rota.Data;
using Rota.Models;
using Rota.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rota.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ShiftAssignmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ShiftAssignmentController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ShiftAssignment/Create
        public async Task<IActionResult> Create()
        {
            var workers = await _userManager.GetUsersInRoleAsync("Worker");
            var viewModel = new ShiftAssignmentViewModel
            {
                Shifts = await _context.Shifts.OrderBy(s => s.StartTime).ToListAsync(),
                Workers = workers.ToList()
            };

            return View(viewModel);
        }

        // POST: ShiftAssignment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShiftAssignmentViewModel model)
        {
            Console.WriteLine("🟢 POST Create HIT: ShiftId = " + model.ShiftId + ", UserId = " + model.UserId);

            if (!ModelState.IsValid)
            {
                Console.WriteLine("❌ ModelState invalid");
                await ReloadDropdowns(model);
                return View(model);
            }

            var exists = await _context.ShiftAssignments
                .AnyAsync(sa => sa.ShiftId == model.ShiftId && sa.UserId == model.UserId);

            if (exists)
            {
                ModelState.AddModelError("", "This user is already assigned to the selected shift.");
                await ReloadDropdowns(model);
                return View(model);
            }

            var assignment = new ShiftAssignment
            {
                ShiftId = model.ShiftId,
                UserId = model.UserId,
                AssignedAt = DateTime.Now
            };

            try
            {
                _context.ShiftAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Assignment inserted");
                TempData["Success"] = "User successfully assigned to shift.";
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 ERROR saving to database: " + ex.Message);
                ModelState.AddModelError("", "Database error. Please try again.");
                await ReloadDropdowns(model);
                return View(model);
            }

            return RedirectToAction("Index", "Shift");
        }

        private async Task ReloadDropdowns(ShiftAssignmentViewModel model)
        {
            model.Shifts = await _context.Shifts.OrderBy(s => s.StartTime).ToListAsync();
            model.Workers = (await _userManager.GetUsersInRoleAsync("Worker")).ToList();
        }
    }
}
