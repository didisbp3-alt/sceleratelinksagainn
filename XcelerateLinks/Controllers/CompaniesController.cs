using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using APIPSI16.Models;
using APIPSI16.Services;

namespace XcelerateLinks.Mvc.Controllers
{
    public class CompaniesController : ApiControllerBase
    {
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(IHttpClientFactory httpFactory, ILogger<CompaniesController> logger, ISessionService sessionService)
            : base(httpFactory, sessionService)
        {
            _logger = logger;
        }

        // Role-dispatched: admin → Index (table), user → UserIndex (company explorer)
        public async Task<IActionResult> Index(string? search = null)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync("api/companies");
            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = await SafeReadStringAsync(resp) ?? "Unable to load companies.";
                return View(IsAdmin() ? "Index" : "UserIndex", Array.Empty<Company>());
            }

            IEnumerable<Company> companies = await resp.Content.ReadFromJsonAsync<IEnumerable<Company>>()
                                              ?? Array.Empty<Company>();

            if (IsAdmin())
                return View(companies);

            // User: optionally filter by search
            if (!string.IsNullOrWhiteSpace(search))
                companies = companies.Where(c =>
                    (c.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (c.Industry ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (c.Location ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));

            ViewBag.Search = search;
            return View("UserIndex", companies);
        }

        public async Task<IActionResult> Details(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/companies/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var company = await resp.Content.ReadFromJsonAsync<Company>();
            if (company == null) return RedirectToAction(nameof(Index));

            // Load company's open opportunities to show on page
            var oppsResp = await client.GetAsync("api/opportunities");
            if (oppsResp.IsSuccessStatusCode)
            {
                var allOpps = await oppsResp.Content.ReadFromJsonAsync<IEnumerable<Opportunity>>();
                ViewBag.CompanyOpportunities = allOpps?.Where(o => o.CompanyId == id).ToList();
            }

            return View(company);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");
            return View(new Company());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Company model)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View(model);

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("api/companies", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to create company.");
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
            var resp = await client.GetAsync($"api/companies/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var company = await resp.Content.ReadFromJsonAsync<Company>();
            if (company == null) return RedirectToAction(nameof(Index));
            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Company model)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            if (id != model.CompanyId) return RedirectToAction(nameof(Index));
            if (!ModelState.IsValid) return View(model);

            var client = CreateAuthorizedClient();
            var resp = await client.PutAsJsonAsync($"api/companies/{id}", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to update company.");
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
            var resp = await client.GetAsync($"api/companies/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var company = await resp.Content.ReadFromJsonAsync<Company>();
            if (company == null) return RedirectToAction(nameof(Index));
            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.DeleteAsync($"api/companies/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Delete), new { id });

            return RedirectToAction(nameof(Index));
        }
    }
}
