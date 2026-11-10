using System.Collections.Generic;

namespace WebDoctor.Models
{
    public class DashboardViewModel
    {
        public string Username { get; set; }
        public string UserType { get; set; } // "0" (Admin), "1" (Doctor), "2" (Patient)
        public int? DoctorId { get; set; }
        public int? PatientId { get; set; }

        public List<Appointments> TodayAppointments { get; set; } = new();
        public List<Appointments> UpcomingAppointments { get; set; } = new();
    }
}