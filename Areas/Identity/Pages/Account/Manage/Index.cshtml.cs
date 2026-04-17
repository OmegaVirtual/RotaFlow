#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rota.Data;
using Rota.Models;

namespace Rota.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public string Username { get; set; }
        public string Email { get; set; }
        public UserProfile Profile { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [Display(Name = "Date of Birth")]
            [DataType(DataType.Date)]
            public DateTime DateOfBirth { get; set; }

            [Display(Name = "Restaurant Name")]
            public string RestaurantName { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            Username = await _userManager.GetUserNameAsync(user);
            Email = await _userManager.GetEmailAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Profile = _context.UserProfiles.FirstOrDefault(p => p.UserId == user.Id);

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FullName = Profile?.FullName,
                DateOfBirth = Profile?.DateOfBirth ?? default,
                RestaurantName = Profile?.RestaurantName
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "❌ Failed to update phone number.";
                    return RedirectToPage();
                }
            }

            var profile = _context.UserProfiles.FirstOrDefault(p => p.UserId == user.Id);
            if (profile != null)
            {
                // Update DOB if changed
                if (Input.DateOfBirth != default && Input.DateOfBirth != profile.DateOfBirth)
                {
                    profile.DateOfBirth = Input.DateOfBirth;
                }

                // Restore Restaurant Name update support (even if it's readonly, we sync it here)
                if (!string.IsNullOrEmpty(Input.RestaurantName) && Input.RestaurantName != profile.RestaurantName)
                {
                    profile.RestaurantName = Input.RestaurantName;
                }

                _context.Update(profile);
                await _context.SaveChangesAsync();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "✅ Your profile has been updated.";
            return RedirectToPage();
        }
    }
}
