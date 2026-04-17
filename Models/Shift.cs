using System;
using System.ComponentModel.DataAnnotations;

namespace Rota.Models
{
    public class Shift
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Shift Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; }

        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Display(Name = "Required Staff")]
        public int RequiredStaff { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}
