using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using APIPSI16.Models;
using APIPSI16.Services;

namespace XcelerateLinks.Mvc.Controllers
{
    public class ChatsController : ApiControllerBase
    {
        private readonly ILogger<ChatsController> _logger;

        public ChatsController(IHttpClientFactory httpFactory, ILogger<ChatsController> logger, ISessionService sessionService)
            : base(httpFactory, sessionService)
        {
            _logger = logger;
        }

        // ADMIN index - table view
        public async Task<IActionResult> Index()
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync("api/chat");
            if (!resp.IsSuccessStatusCode)
            {
                ViewBag.Error = await SafeReadStringAsync(resp) ?? "Unable to load chats.";
                return View(Array.Empty<Chat>());
            }

            var chats = await resp.Content.ReadFromJsonAsync<IEnumerable<Chat>>();
            return View(chats ?? Array.Empty<Chat>());
        }

        // USER-FACING: full messaging page (conversations list + chat window with React)
        public async Task<IActionResult> Messages(int? chatId = null)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            ViewBag.InitialChatId = chatId;
            ViewBag.CurrentUserId = GetCurrentUserId();
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/chat/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var chat = await resp.Content.ReadFromJsonAsync<Chat>();
            if (chat == null) return RedirectToAction(nameof(Index));
            return View(chat);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return RedirectToAction(nameof(Messages));

            return View(new Chat
            {
                CreatedByUserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chat model)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                ModelState.AddModelError("", "Unable to identify current user.");
                return View(model);
            }

            model.CreatedByUserId = userId.Value;
            model.CreatedAt ??= DateTime.UtcNow;

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("api/chat", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", await SafeReadStringAsync(resp) ?? "Unable to create chat.");
                return View(model);
            }

            var created = await resp.Content.ReadFromJsonAsync<Chat>();
            return RedirectToAction(nameof(Messages), new { chatId = created?.ChatId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.GetAsync($"api/chat/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            var chat = await resp.Content.ReadFromJsonAsync<Chat>();
            if (chat == null) return RedirectToAction(nameof(Index));
            return View(chat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await ValidateSessionAsync())
                return RedirectToAction("Login", "Account");

            var client = CreateAuthorizedClient();
            var resp = await client.DeleteAsync($"api/chat/{id}");
            if (!resp.IsSuccessStatusCode)
                return RedirectToAction(nameof(Delete), new { id });

            return RedirectToAction(nameof(Index));
        }
    }
}
