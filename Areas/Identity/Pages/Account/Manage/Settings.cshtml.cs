// FINAL CLEAN AND WORKING SettingsModel.cs — All Forms Fixed
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rota.Data;
using Rota.Models;

namespace Rota.Areas.Identity.Pages.Account.Manage
{
    public class SettingsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public SettingsModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [TempData] public string StatusMessage { get; set; }

        [BindProperty] public EmailModel EmailInput { get; set; }
        [BindProperty] public PasswordModel PasswordInput { get; set; }
        [BindProperty] public ProfileModel ProfileInput { get; set; }
        [BindProperty] public IFormFile ProfileImage { get; set; }

        public class EmailModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "New Email")]
            public string NewEmail { get; set; }
        }

        public class PasswordModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string OldPassword { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public class ProfileModel
        {
            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime DateOfBirth { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            EmailInput = new EmailModel { NewEmail = user.Email };

            var profile = _context.UserProfiles.FirstOrDefault(p => p.UserId == user.Id);
            if (profile != null)
            {
                ProfileInput = new ProfileModel { DateOfBirth = profile.DateOfBirth };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            ModelState.Remove("PasswordInput.OldPassword");
            ModelState.Remove("PasswordInput.NewPassword");
            ModelState.Remove("PasswordInput.ConfirmPassword");
            ModelState.Remove("ProfileInput.DateOfBirth");

            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(EmailInput.NewEmail) && EmailInput.NewEmail != user.Email)
            {
                user.Email = EmailInput.NewEmail;
                user.UserName = EmailInput.NewEmail;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return Page();
                }

                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Email updated successfully.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            ModelState.Remove("EmailInput.NewEmail");
            ModelState.Remove("ProfileInput.DateOfBirth");

            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, PasswordInput.OldPassword, PasswordInput.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Password changed.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUploadImageAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }

                var profile = _context.UserProfiles.FirstOrDefault(p => p.UserId == user.Id);
                if (profile != null)
                {
                    profile.ProfileImagePath = $"/images/profiles/{fileName}";
                    _context.Update(profile);
                    await _context.SaveChangesAsync();
                }

                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Profile picture updated.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateDobAsync()
        {
            ModelState.Remove("EmailInput.NewEmail");
            ModelState.Remove("PasswordInput.OldPassword");
            ModelState.Remove("PasswordInput.NewPassword");
            ModelState.Remove("PasswordInput.ConfirmPassword");

            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var profile = _context.UserProfiles.FirstOrDefault(p => p.UserId == user.Id);
            if (profile != null)
            {
                profile.DateOfBirth = ProfileInput.DateOfBirth;
                _context.Update(profile);
                await _context.SaveChangesAsync();

                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Date of birth updated.";
            }

            return RedirectToPage();
        }
    }
}
