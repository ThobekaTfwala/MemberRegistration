using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeetingRegister.Models
{
    public class MeetingAttendee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MeetingId { get; set; }

        [ForeignKey("MeetingId")]
        public virtual Meeting Meeting { get; set; } = null!;

        [Required]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Cellphone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "QR Code")]
        public string QRCode { get; set; } = string.Empty;

        public virtual ICollection<AttendanceSession> Sessions { get; set; } = new List<AttendanceSession>();
    }
}
