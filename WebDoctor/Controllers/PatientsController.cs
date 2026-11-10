using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebDoctor.Models;

namespace WebDoctor.Controllers
{
    [Authorize(Roles = "Admin,Doctor")]
    public class PatientsController : Controller
    {
        private readonly HttpClient _httpClient;
        public PatientsController(IHttpClientFactory httpClientFactory) => _httpClient = httpClientFactory.CreateClient("DoctorApi");

        public async Task<IActionResult> Index()
        {
            var patients = await _httpClient.GetFromJsonAsync<List<Patients>>("api/Patients");
            return View(patients);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/Patients/{id}");
            return response.IsSuccessStatusCode ? Ok() : StatusCode((int)response.StatusCode);
        }
        [HttpGet]
        public IActionResult Create() => View("PatientsFormPartial", new Patients());
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Patients newPatient)
        {
            if (!ModelState.IsValid) return View("PatientsFormPartial", newPatient);
            var response = await _httpClient.PostAsJsonAsync("api/Patients", newPatient);
            if (response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));
            await AddApiErrorsAsync(response);
            return View("PatientsFormPartial", newPatient);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var patient = await _httpClient.GetFromJsonAsync<Patients>($"api/Patients/{id}");
            return patient == null ? NotFound() : View("PatientsFormPartial", patient);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Patients patient)
        {
            if (id != patient.Id) return BadRequest();
            if (!ModelState.IsValid) return View("PatientsFormPartial", patient);
            var response = await _httpClient.PutAsJsonAsync($"api/Patients/{id}", patient);
            if (response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));
            await AddApiErrorsAsync(response);
            return View("PatientsFormPartial", patient);
        }
        private async Task AddApiErrorsAsync(HttpResponseMessage response)
        {
            try
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("errors", out var errors))
                {
                    foreach (var field in errors.EnumerateObject())
                        foreach (var message in field.Value.EnumerateArray())
                            ModelState.AddModelError(field.Name, message.GetString() ?? "Invalid value.");
                }
                else ModelState.AddModelError(string.Empty, "Could not save patient.");
            }
            catch (JsonException) { ModelState.AddModelError(string.Empty, "Could not save patient."); }
        }
    }
}
