using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rota.Models.Requests
{
    public class UserHolidayBalance
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        [Range(0, 365)]
        public int AnnualAllowance { get; set; } = 28;

        [Range(0, 365)]
        public int UsedDays { get; set; } = 0;
    }
}
