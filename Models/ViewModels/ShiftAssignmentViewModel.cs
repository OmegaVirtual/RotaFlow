using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Rota.Models.ViewModels
{
    public class ShiftAssignmentViewModel
    {
        [Required(ErrorMessage = "Please select a shift.")]
        [Display(Name = "Shift")]
        public int ShiftId { get; set; }

        [Required(ErrorMessage = "Please select a user.")]
        [Display(Name = "User")]
        public string UserId { get; set; }

        // Populated lists for dropdowns
        public List<Shift> Shifts { get; set; } = new();
        public List<IdentityUser> Workers { get; set; } = new();
    }
}
