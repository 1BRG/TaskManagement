using System.ComponentModel.DataAnnotations;

namespace TaskManagement.ViewModels
{
    public class UserEditViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Prenumele este obligatoriu")]
        [Display(Name = "Prenume")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Numele este obligatoriu")]
        [Display(Name = "Nume")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } // Doar pentru afisare sau editare

        [Phone]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Must be only 10 digits.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Only numeric digits are allowed.")]
        public string? PhoneNumber { get; set; }
    }
}