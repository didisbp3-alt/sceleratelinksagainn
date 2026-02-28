using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using APIPSI16.Models;
using APIPSI16.Services;

namespace XcelerateLinks.Mvc.Controllers
{
    public class OpportunitiesController : ApiControllerBase
    {
        private readonly ILogger<OpportunitiesController> _logger;

        public OpportunitiesController(IHttpClientFactory httpFactory, ILogger<OpportunitiesController> logger, ISessionService sessionService)
            : base(httpFactory, sessionService)
        {
            _logger = logger;
        }

        // Role-dispatched: admin → Index (table), user → UserIndex (job search)
        public async Task<IActionResult> Index(string? q = null, string? location = null, byte? employmentType = null, byte? remoteOption = null)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync("api/opportunities");
            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = await SafeReadStringAsync(resp) ?? "Unable to load opportunities.";
                return View(IsAdmin() ? "Index" : "UserIndex", Array.Empty<Opportunity>());
            }

            IEnumerable<Opportunity> opportunities = await resp.Content.ReadFromJsonAsync<IEnumerable<Opportunity>>()
                                                      ?? Array.Empty<Opportunity>();

            if (IsAdmin())
                return View(opportunities);

            // User: apply filters
            if (!string.IsNullOrWhiteSpace(q))
                opportunities = opportunities.Where(o =>
                    (o.Title ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (o.Location ?? "").Contains(q, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(location))
                opportunities = opportunities.Where(o =>
                    (o.Location ?? "").Contains(location, StringComparison.OrdinalIgnoreCase));

            if (employmentType.HasValue)
                opportunities = opportunities.Where(o => o.EmploymentType == employmentType.Value);

            if (remoteOption.HasValue)
                opportunities = opportunities.Where(o => o.RemoteOption == remoteOption.Value);

            ViewBag.Q = q;
            ViewBag.Location = location;
            ViewBag.EmploymentType = employmentType;
            ViewBag.RemoteOption = remoteOption;

            return View("UserIndex", opportunities);
        }

        // Keep Browse as alias (used in existing nav links)
        public async Task<IActionResult> Browse(string? q = null, string? location = null, byte? employmentType = null, byte? remoteOption = null)
            => await Index(q, location, employmentType, remoteOption);

        public async Task<IActionResult> Details(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/opportunities/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var opportunity = await resp.Content.ReadFromJsonAsync<Opportunity>();
            if (opportunity == null) return RedirectToAction(nameof(Index));
            return View(opportunity);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var model = new Opportunity();
            var userId = GetCurrentUserId();
            if (userId.HasValue)
                model.CreatorId = userId.Value;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Opportunity model)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View(model);
            model.CreatorId ??= GetCurrentUserId();

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("api/opportunities", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to create opportunity.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/opportunities/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var opportunity = await resp.Content.ReadFromJsonAsync<Opportunity>();
            if (opportunity == null) return RedirectToAction(nameof(Index));
            return View(opportunity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Opportunity model)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            if (id != model.Id) return RedirectToAction(nameof(Index));
            if (!ModelState.IsValid) return View(model);

            var client = CreateAuthorizedClient();
            var resp = await client.PutAsJsonAsync($"api/opportunities/{id}", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to update opportunity.");
                return View(model);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/opportunities/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var opportunity = await resp.Content.ReadFromJsonAsync<Opportunity>();
            if (opportunity == null) return RedirectToAction(nameof(Index));
            return View(opportunity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.DeleteAsync($"api/opportunities/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Delete), new { id });

            return RedirectToAction(nameof(Index));
        }
    }
}
