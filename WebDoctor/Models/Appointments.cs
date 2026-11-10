using System.ComponentModel.DataAnnotations;

namespace WebDoctor.Models
{
    public class Appointments
    {
        public int Id { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Select a doctor.")]
        public int DoctorId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Select a patient.")]
        public int PatientId { get; set; }
        [Required(ErrorMessage = "Appointment date and time are required.")]
        public DateTime AppointmentTime { get; set; }

        public string Status_name { get; set; } = string.Empty;
        [Range(1, int.MaxValue, ErrorMessage = "Select a status.")]
        public int? Status_id { get; set; }

        public string? Notes { get; set; }
        public string PatientFirstName { get; set; } = string.Empty;
        public string PatientLastName { get; set; } = string.Empty;
        public string PatientEmbg { get; set; } = string.Empty;
        public string DoctorFirstName { get; set; } = string.Empty;
        public string DoctorLastName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
    }
}
