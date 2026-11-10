using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using WebDoctor.Models;
using System.Security.Claims;

namespace WebDoctor.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("DoctorApi");
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return View(new DashboardViewModel());
            }

            var model = new DashboardViewModel
            {
                Username = User.Identity.Name,
                UserType = User.FindFirst("UserType")?.Value,
                DoctorId = int.TryParse(User.FindFirst("DoctorId")?.Value, out var dId) ? dId : null,
                PatientId = int.TryParse(User.FindFirst("PatientId")?.Value, out var pId) ? pId : null
            };

            try
            {
                var allAppointments = await _httpClient.GetFromJsonAsync<List<Appointments>>("api/Appointments") ?? new List<Appointments>();


                if (model.UserType == "1" && model.DoctorId.HasValue)
                {
                    allAppointments = allAppointments.Where(a => a.DoctorId == model.DoctorId.Value).ToList();
                }
                else if (model.UserType == "2" && model.PatientId.HasValue)
                {
                    allAppointments = allAppointments.Where(a => a.PatientId == model.PatientId.Value).ToList();
                }

                var today = DateTime.Today;
                var nextWeek = today.AddDays(7);


                model.TodayAppointments = allAppointments
                    .Where(a => a.AppointmentTime.Date == today)
                    .ToList();


                model.UpcomingAppointments = allAppointments
                    .Where(a => a.AppointmentTime.Date > today && a.AppointmentTime.Date <= nextWeek)
                    .OrderBy(a => a.AppointmentTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching appointments from the API.");
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
