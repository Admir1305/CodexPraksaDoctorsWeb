namespace WebDoctor.Models
{
    public class Doctors
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Select a specialty.")]
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Select a valid specialty.")]
        public int? SpecialtyId { get; set; }

        public string? SpecialtyName { get; set; }

        public string Phone { get; set; }

        public bool IsActive { get; set; }

        public string IsActiveTemp => IsActive ? "Yes" : "No";

        public List<WebDoctor.Models.Specialties> Specialties { get; set; } = new List<WebDoctor.Models.Specialties>();

    }
}
