using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebDoctor.Models;

namespace WebDoctor.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _httpClient;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("DoctorApi");
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View("~/Views/Users/login.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Users/login.cshtml", loginDto);
            }

            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/Auth/Login", loginDto);

            if (!response.IsSuccessStatusCode)
            {

                ModelState.AddModelError(string.Empty, "Invalid username/password or inactive profile.");
                return View("~/Views/Users/login.cshtml", loginDto);
            }

            var user = await response.Content.ReadFromJsonAsync<Users>();

            if (user == null)
            {

                ModelState.AddModelError(string.Empty, "Error processing login response.");
                return View("~/Views/Users/login.cshtml", loginDto);
            }


            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Korisnik),
                new Claim(ClaimTypes.Role, GetRoleName(user.TipKorisnik)),
                new Claim("UserType", user.TipKorisnik.ToString())
            };

            if (user.DoctorId.HasValue)
                claims.Add(new Claim("DoctorId", user.DoctorId.Value.ToString()));

            if (user.PatientId.HasValue)
                claims.Add(new Claim("PatientId", user.PatientId.Value.ToString()));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        private string GetRoleName(short tip)
        {
            return tip switch
            {
                0 => "Admin",
                1 => "Doctor",
                2 => "Patient",
                _ => "User"
            };
        }
    }
}
