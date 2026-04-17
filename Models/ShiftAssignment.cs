using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Rota.Models
{
    public class ShiftAssignment
    {
        public int Id { get; set; }

        [Required]
        public int ShiftId { get; set; }

        [ForeignKey(nameof(ShiftId))]
        public Shift Shift { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; }

        [Display(Name = "Assigned On")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
