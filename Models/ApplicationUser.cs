using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace Rota.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string FullName { get; set; }

        public int AnnualHolidayAllowance { get; set; } = 28;
        public int HolidaysTaken { get; set; } = 0;

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [MaxLength(100)]
        public string RestaurantName { get; set; }
    }
}
