using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDoctor.Models;
namespace WebDoctor.Controllers
{
    [Authorize(Roles = "Admin")] 
   public class DoctorsController : Controller
    {
        private readonly HttpClient _httpClient;

        public DoctorsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("DoctorApi");
        }
        public async Task<IActionResult> Index()
        {
            var doctors = await _httpClient.GetFromJsonAsync<List<WebDoctor.Models.Doctors>>("api/Doctors");

            return View(doctors);
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync($"api/Doctors/{id}");

            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }

            return StatusCode((int)response.StatusCode);

        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var doctorViewModel = new WebDoctor.Models.Doctors();

            var specialtiesResponse = await _httpClient.GetFromJsonAsync<List<WebDoctor.Models.Specialties>>("api/Specialties");

            if (specialtiesResponse != null)
            {
                doctorViewModel.Specialties = specialtiesResponse;
            }
            return View("DoctorsFormPartial",doctorViewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(WebDoctor.Models.Doctors newDoctor)
        {
            if (!ModelState.IsValid)
            {
                newDoctor.Specialties = await _httpClient.GetFromJsonAsync<List<WebDoctor.Models.Specialties>>("api/Specialties") ?? new();
                return View("DoctorsFormPartial", newDoctor);
            }

            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/Doctors", newDoctor);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            var specialtiesResponse = await _httpClient.GetFromJsonAsync<List<WebDoctor.Models.Specialties>>("api/Specialties");

            if (specialtiesResponse != null)
            {
                newDoctor.Specialties = specialtiesResponse;
            }

            ModelState.AddModelError(string.Empty, "Could not save doctor. Check the selected specialty and try again.");


            return View("DoctorsFormPartial",newDoctor);
        }

        [HttpGet]
        // zemanje podatoci od api, i isprakanje kon forma na view 
        public async Task<IActionResult> Edit(int id)
        {
            var doctor = await _httpClient.GetFromJsonAsync<WebDoctor.Models.Doctors>($"api/Doctors/{id}");

            if (doctor == null)
            {
                return NotFound();
            }

            var specialtiesResponse = await _httpClient.GetFromJsonAsync<List<WebDoctor.Models.Specialties>>("api/Specialties");

            if (specialtiesResponse != null)
            {
                doctor.Specialties = specialtiesResponse;
            }

            return View("DoctorsFormPartial", doctor);

        }
        [HttpPost]
        // zacuvuvanje na podatocite
        public async Task<IActionResult> Edit(int id, WebDoctor.Models.Doctors doctor)
        {
            if (id != doctor.Id)
            {
                return BadRequest();
            }
            if (ModelState.IsValid)
            {
                HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"api/Doctors/{id}", doctor);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Could not update doctor. Check the selected specialty and try again.");
                }
            }

            var specialtiesResponse = await _httpClient.GetFromJsonAsync<List<WebDoctor.Models.Specialties>>("api/Specialties");

            if (specialtiesResponse != null)
            {
                doctor.Specialties = specialtiesResponse;

            }
            return View("DoctorsFormPartial", doctor);
        }
    }
}
