using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Rota.Models.Requests
{
    public enum HolidayStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class HolidayRequest : IValidatableObject
    {
        public int Id { get; set; }

        [BindNever]
        [Display(Name = "User")]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        [BindNever]
        public IdentityUser User { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public string? Reason { get; set; }

        public HolidayStatus Status { get; set; } = HolidayStatus.Pending;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Manager Comment")]
        public string? ManagerComment { get; set; }

        // ✅ Helper: Check if a date overlaps this approved request
        public bool Overlaps(DateTime date)
        {
            return Status == HolidayStatus.Approved &&
                   date.Date >= StartDate.Date &&
                   date.Date <= EndDate.Date;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate < StartDate)
            {
                yield return new ValidationResult("End date must be after the start date.", new[] { nameof(EndDate) });
            }
        }
    }
}
