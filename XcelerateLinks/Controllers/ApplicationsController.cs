using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using APIPSI16.Models;
using APIPSI16.Services;
using XcelerateLinks.Models.ViewModels;

namespace XcelerateLinks.Mvc.Controllers
{
    public class ApplicationsController : ApiControllerBase
    {
        private readonly ILogger<ApplicationsController> _logger;

        public ApplicationsController(IHttpClientFactory httpFactory, ILogger<ApplicationsController> logger, ISessionService sessionService)
            : base(httpFactory, sessionService)
        {
            _logger = logger;
        }

        // Role-dispatched: admin → Index (all apps table), user → UserIndex (my jobs tracker)
        public async Task<IActionResult> Index()
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var uid = GetCurrentUserId();
            if (uid == null) return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();

            if (IsAdmin())
            {
                // Admin: load all applications
                var allResp = await client.GetAsync("api/jobapplications");
                if (!allResp.IsSuccessStatusCode)
                {
                    ViewBag.Error = await SafeReadStringAsync(allResp) ?? "Unable to load applications.";
                    return View(Array.Empty<JobApplication>());
                }
                var allApps = await allResp.Content.ReadFromJsonAsync<IEnumerable<JobApplication>>();
                return View(allApps ?? Array.Empty<JobApplication>());
            }

            // Regular user: load their own applications
            var resp = await client.GetAsync($"api/jobapplications/user/{uid}");
            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = await SafeReadStringAsync(resp) ?? "Unable to load applications.";
                return View("UserIndex", Array.Empty<JobApplication>());
            }

            var applications = await resp.Content.ReadFromJsonAsync<IEnumerable<JobApplication>>();
            return View("UserIndex", applications ?? Array.Empty<JobApplication>());
        }

        public async Task<IActionResult> Details(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/jobapplications/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var application = await resp.Content.ReadFromJsonAsync<JobApplication>();
            if (application == null) return RedirectToAction(nameof(Index));
            return View(application);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? opportunityId = null)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var model = new JobApplicationCreateViewModel
            {
                Application = new JobApplication { OpportunityId = opportunityId ?? 0 }
            };

            if (opportunityId.HasValue)
            {
                // Pre-load the specific opportunity if known
                var client = CreateAuthorizedClient();
                var oppResp = await client.GetAsync($"api/opportunities/{opportunityId}");
                if (oppResp.IsSuccessStatusCode)
                {
                    var opp = await oppResp.Content.ReadFromJsonAsync<Opportunity>();
                    ViewBag.Opportunity = opp;
                }
            }
            else
            {
                model.Opportunities = await LoadOpportunitiesAsync();
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobApplicationCreateViewModel model)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                model.Opportunities = await LoadOpportunitiesAsync();
                return View(model);
            }

            var payload = new { OpportunityId = model.Application.OpportunityId, Name = model.Application.Name };

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("api/jobapplications/apply", payload);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to submit application.");
                model.Opportunities = await LoadOpportunitiesAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = "A sua candidatura foi submetida com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/jobapplications/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var application = await resp.Content.ReadFromJsonAsync<JobApplication>();
            if (application == null) return RedirectToAction(nameof(Index));
            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.DeleteAsync($"api/jobapplications/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Delete), new { id });

            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<Opportunity>> LoadOpportunitiesAsync()
        {
            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync("api/opportunities");
            if (!resp.IsSuccessStatusCode)
                return Array.Empty<Opportunity>();
            return await resp.Content.ReadFromJsonAsync<IEnumerable<Opportunity>>() ?? Array.Empty<Opportunity>();
        }

        public class JobApplicationCreateViewModel
        {
            public JobApplication Application { get; set; } = new JobApplication();
            public IEnumerable<Opportunity> Opportunities { get; set; } = Array.Empty<Opportunity>();
        }
    }
}
