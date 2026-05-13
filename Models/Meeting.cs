using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MeetingRegister.Models
{
    public class Meeting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Venue { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "Expiry Time")]
        public DateTime ExpiryTime { get; set; }

        [Required]
        [StringLength(5)]
        [Display(Name = "Meeting Code")]
        public string MeetingCode { get; set; } = string.Empty;

        // Multi-Admin Ownership
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public virtual ICollection<MeetingAttendee> Attendees { get; set; } = new List<MeetingAttendee>();
    }
}
