using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using WebDoctor.Models;

namespace WebDoctor.Controllers
{
    [Authorize(Roles = "Admin,Doctor,Patient")]
    public class AppointmentsController : Controller
    {
        private readonly HttpClient _httpClient;

        public AppointmentsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("DoctorApi");
        }
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo, int? statusId, int? doctorId, int? patientId)
        {
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.StatusId = statusId;
            ViewBag.DoctorId = doctorId;
            ViewBag.PatientId = patientId;
            await PopulateAppointmentOptionsAsync(new Appointments
            {
                Status_id = statusId,
                DoctorId = doctorId ?? 0,
                PatientId = patientId ?? 0
            }, useFilterSelections: true, filterDoctorId: doctorId, filterPatientId: patientId);
            return View(new List<Appointments>());
        }
        [HttpGet]
        public async Task<IActionResult> Data(DateTime? dateFrom, DateTime? dateTo, int? statusId, int? doctorId, int? patientId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out int userId))
                return Unauthorized();

            var query = new List<string>();
            if (dateFrom.HasValue) query.Add($"dateFrom={Uri.EscapeDataString(dateFrom.Value.ToString("yyyy-MM-dd"))}");
            if (dateTo.HasValue) query.Add($"dateTo={Uri.EscapeDataString(dateTo.Value.ToString("yyyy-MM-dd"))}");
            if (statusId.HasValue) query.Add($"statusId={statusId.Value}");
            if (doctorId.HasValue) query.Add($"doctorId={doctorId.Value}");
            if (patientId.HasValue) query.Add($"patientId={patientId.Value}");

            string url = $"api/Appointment/user/{userId}" + (query.Count == 0 ? "" : "?" + string.Join("&", query));
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Could not load appointments.");

            var items = await response.Content.ReadFromJsonAsync<List<Appointments>>() ?? new List<Appointments>();
            return Json(items);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new Appointments { AppointmentTime = DateTime.Now.AddHours(1) };
            await PopulateAppointmentOptionsAsync(model, setDefaultStatus: true);
            return View("AppointmentsFormPartial", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointments model)
        {
            if (model.AppointmentTime <= DateTime.Now)
                ModelState.AddModelError(nameof(model.AppointmentTime), "Appointment must be in the future.");

            if (ModelState.IsValid)
            {
                var response = await _httpClient.PostAsJsonAsync("api/Appointment", model);
                if (response.IsSuccessStatusCode)
                    return RedirectToAction(nameof(Index));
                await AddApiErrorsAsync(response);
            }

            await PopulateAppointmentOptionsAsync(model);
            return View("AppointmentsFormPartial", model);
        }
        private async Task PopulateAppointmentOptionsAsync(
            Appointments model,
            bool setDefaultStatus = false,
            bool useFilterSelections = false,
            int? filterDoctorId = null,
            int? filterPatientId = null)
        {
            var selectedDoctorId = useFilterSelections ? filterDoctorId : model.DoctorId;
            var selectedPatientId = useFilterSelections ? filterPatientId : model.PatientId;
            var statuses = new List<AppointmentStatus>();
            try
            {
                statuses = await _httpClient.GetFromJsonAsync<List<AppointmentStatus>>("api/Appointment/statuses") ?? new();
                statuses = statuses
                    .Where(IsSelectableStatus)
                    .OrderBy(s => StatusOrder(s.Name))
                    .ToList();
                ViewBag.Statuses = new SelectList(statuses, "Id", "Name", model.Status_id);
                if (setDefaultStatus && (!model.Status_id.HasValue || model.Status_id.Value <= 0))
                {
                    var scheduled = statuses.FirstOrDefault(s =>
                        s.Name.Contains("schedul", StringComparison.OrdinalIgnoreCase) ||
                        s.Name.Contains("закаж", StringComparison.OrdinalIgnoreCase) ||
                        s.Name.Contains("zakaz", StringComparison.OrdinalIgnoreCase));
                    model.Status_id = scheduled?.Id ?? statuses.FirstOrDefault()?.Id;
                    ViewBag.Statuses = new SelectList(statuses, "Id", "Name", model.Status_id);
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.Statuses = new SelectList(Array.Empty<AppointmentStatus>(), "Id", "Name");
            }

            try
            {
                var doctors = await _httpClient.GetFromJsonAsync<List<Doctor>>("api/Doctors") ?? new();
                var specialties = await _httpClient.GetFromJsonAsync<List<Specialty>>("api/Specialties") ?? new();
                var specialtyNames = specialties.ToDictionary(s => s.Id, s => s.Name);
                ViewBag.Doctors = new SelectList(doctors.Select(d => new
                {
                    d.Id,
                    Name = $"{d.FirstName} {d.LastName}" + (d.SpecialtyId.HasValue && specialtyNames.TryGetValue(d.SpecialtyId.Value, out var specialty) ? $" — {specialty}" : "")
                }), "Id", "Name", selectedDoctorId);
            }
            catch (HttpRequestException) { ViewBag.Doctors = new SelectList(Array.Empty<object>()); }

            try
            {
                var patients = await _httpClient.GetFromJsonAsync<List<Patients>>("api/Patients") ?? new();
                ViewBag.Patients = new SelectList(patients.Select(p => new
                {
                    p.Id,
                    Name = $"{p.FirstName} {p.LastName} ({p.EMBG})"
                }), "Id", "Name", selectedPatientId);
            }
            catch (HttpRequestException) { ViewBag.Patients = new SelectList(Array.Empty<object>()); }
        }

        private static bool IsSelectableStatus(AppointmentStatus status)
        {
            var name = status.Name.Trim();
            return name.Contains("schedul", StringComparison.OrdinalIgnoreCase)
                || name.Contains("закаж", StringComparison.OrdinalIgnoreCase)
                || name.Contains("zakaz", StringComparison.OrdinalIgnoreCase)
                || name.Contains("progress", StringComparison.OrdinalIgnoreCase)
                || name.Contains("во тек", StringComparison.OrdinalIgnoreCase)
                || name.Contains("tek", StringComparison.OrdinalIgnoreCase)
                || name.Contains("complete", StringComparison.OrdinalIgnoreCase)
                || name.Contains("заврш", StringComparison.OrdinalIgnoreCase)
                || name.Contains("zavr", StringComparison.OrdinalIgnoreCase);
        }

        private static int StatusOrder(string name)
        {
            if (name.Contains("schedul", StringComparison.OrdinalIgnoreCase)
                || name.Contains("закаж", StringComparison.OrdinalIgnoreCase)
                || name.Contains("zakaz", StringComparison.OrdinalIgnoreCase)) return 0;
            if (name.Contains("progress", StringComparison.OrdinalIgnoreCase)
                || name.Contains("во тек", StringComparison.OrdinalIgnoreCase)
                || name.Contains("tek", StringComparison.OrdinalIgnoreCase)) return 1;
            return 2;
        }
        private async Task AddApiErrorsAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(nameof(Appointments.AppointmentTime), "The doctor already has an appointment in this time slot.");
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("errors", out var errors))
                {
                    foreach (var field in errors.EnumerateObject())
                        foreach (var message in field.Value.EnumerateArray())
                            ModelState.AddModelError(field.Name, message.GetString() ?? "Invalid value.");
                    return;
                }
                if (document.RootElement.TryGetProperty("detail", out var detail))
                    ModelState.AddModelError(string.Empty, detail.GetString() ?? "Could not save appointment.");
            }
            catch (JsonException) { ModelState.AddModelError(string.Empty, "Could not save appointment."); }
        }
    }
    public sealed class Doctor
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? SpecialtyId { get; set; }
    }
public sealed class Specialty
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class AppointmentStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

}
