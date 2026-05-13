using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeetingRegister.Models
{
    public class AttendanceSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AttendeeId { get; set; }

        [ForeignKey("AttendeeId")]
        public virtual MeetingAttendee Attendee { get; set; } = null!;

        [Required]
        [Display(Name = "In Time")]
        public DateTime InTime { get; set; }

        [Display(Name = "Out Time")]
        public DateTime? OutTime { get; set; }

        [Display(Name = "Duration")]
        public TimeSpan? Duration { get; set; }
    }
}
