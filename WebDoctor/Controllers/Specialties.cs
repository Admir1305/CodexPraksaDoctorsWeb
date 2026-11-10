using Microsoft.AspNetCore.Mvc;

namespace WebDoctor.Controllers
{
    public class Specialties : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
