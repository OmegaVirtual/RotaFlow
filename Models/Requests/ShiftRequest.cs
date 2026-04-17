using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Rota.Models.Requests
{
    public enum RequestType
    {
        TimeOff,
        ShiftSwap,
        ExtraShift,
        AvailabilityUpdate
    }

    public enum RequestStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class ShiftRequest
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "User")]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; }

        [Required(ErrorMessage = "Request type is required.")]
        [Display(Name = "Request Type")]
        public RequestType Type { get; set; }

        [Display(Name = "Details")]
        public string? Details { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Shift Date")]
        public DateTime? ShiftDate { get; set; }

        [Display(Name = "Swap With User ID")]
        public string? SwapWithUserId { get; set; }

        [Display(Name = "Availability Note")]
        public string? AvailabilityNote { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
}
