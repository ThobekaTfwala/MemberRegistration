using System.ComponentModel.DataAnnotations;

namespace MeetingRegister.Models
{
    public class RegistrationViewModel
    {
        [StringLength(5, ErrorMessage = "Meeting Code must be exactly 5 characters.")]
        [Display(Name = "Meeting Code")]
        public string? MeetingCode { get; set; }

        [Display(Name = "Full Name")]
        public string? Name { get; set; }

        [Phone]
        public string? Cellphone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Required(ErrorMessage = "QR Code is required.")]
        [Display(Name = "QR Code")]
        public string QRCode { get; set; } = string.Empty;
    }
}
