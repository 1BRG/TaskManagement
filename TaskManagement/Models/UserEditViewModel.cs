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
        public string? PhoneNumber { get; set; }
    }
}