using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using APIPSI16.Models.DTOs;
using APIPSI16.Services;
using XcelerateLinks.Models.ViewModels;

namespace XcelerateLinks.Mvc.Controllers
{
    public class UsersController : ApiControllerBase
    {
        private readonly ILogger<UsersController> _logger;

        public UsersController(IHttpClientFactory httpFactory, ILogger<UsersController> logger, ISessionService sessionService)
            : base(httpFactory, sessionService)
        {
            _logger = logger;
        }

        // LIST users
        public async Task<IActionResult> Index(int? jobPreference = null, int? nationality = null)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var model = new UserFilterViewModel
            {
                JobPreference = jobPreference,
                Nationality = nationality
            };

            var client = CreateAuthorizedClient();
            var query = new List<string>();
            if (jobPreference.HasValue) query.Add($"jobPreference={jobPreference.Value}");
            if (nationality.HasValue) query.Add($"nationality={nationality.Value}");
            var url = query.Count == 0 ? "api/users" : $"api/users?{string.Join("&", query)}";

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                model.ErrorMessage = await SafeReadStringAsync(resp) ?? "Unable to load users with the selected filters.";
                model.Users = Array.Empty<UserDTO>();
                return View(model);
            }

            model.Users = await resp.Content.ReadFromJsonAsync<IEnumerable<UserDTO>>() ?? Array.Empty<UserDTO>();
            return View(model);
        }

        // USER DETAILS
        public async Task<IActionResult> Details(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/users/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await resp.Content.ReadFromJsonAsync<UserDTO>();
            if (user == null) return RedirectToAction(nameof(Index));
            return View(user);
        }

        // CURRENT PROFILE REDIRECT
        public async Task<IActionResult> Profile()
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Edit), new { id = userId.Value });
        }

        // EDIT USER GET
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/users/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await resp.Content.ReadFromJsonAsync<UserDTO>();
            if (user == null) return RedirectToAction(nameof(Index));
            return View(user);
        }

        // EDIT USER POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserDTO model)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            if (id != model.UserId) return RedirectToAction(nameof(Index));
            if (!ModelState.IsValid) return View(model);

            var client = CreateAuthorizedClient();
            var resp = await client.PutAsJsonAsync($"api/users/{id}", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to update user.");
                return View(model);
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}