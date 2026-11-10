namespace WebDoctor.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string Korisnik { get; set; }
        public string Lozinka { get; set; }
        public short Aktiven { get; set; } = 1;
        public short TipKorisnik { get; set; } // 0-administrator, 1-doctor, 2-pacient
        public int? DoctorId { get; set; }
        public int? PatientId { get; set; }

    }

    public class LoginDto
    {
        public string Korisnik { get; set; } = string.Empty;
        public string Lozinka { get; set; } = string.Empty;
    }
}
