using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerPlatform.Web.Data;
using VolunteerPlatform.Web.Models;

namespace VolunteerPlatform.Web.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public EventsController(ApplicationDbContext context,
                              UserManager<ApplicationUser> userManager,
                              IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [AllowAnonymous]
        
        public async Task<IActionResult> Index(
    string sortOrder,
    string currentFilter,
    string searchString,
    string locationFilter,
    int? pageNumber)
        {
            // Сохраняем параметры сортировки в ViewData
            ViewData["CurrentSort"] = sortOrder;
            ViewData["TitleSortParm"] = string.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["LocationSortParm"] = sortOrder == "Location" ? "location_desc" : "Location";
            ViewData["ParticipantsSortParm"] = sortOrder == "Participants" ? "participants_desc" : "Participants";

            // Сброс номера страницы при поиске
            if (searchString != null || locationFilter != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["LocationFilter"] = locationFilter;

            // Получаем события с регистрациями
            var events = _context.Events
                .Include(e => e.Registrations)
                .AsQueryable();

            // Фильтрация по названию (поиск)
            if (!string.IsNullOrEmpty(searchString))
            {
                events = events.Where(e => e.Title.Contains(searchString)
                                        || e.Description.Contains(searchString));
            }

            // Фильтрация по местоположению
            if (!string.IsNullOrEmpty(locationFilter))
            {
                events = events.Where(e => e.Location.Contains(locationFilter));
            }

            // Сортировка
            events = sortOrder switch
            {
                "title_desc" => events.OrderByDescending(e => e.Title),
                "Date" => events.OrderBy(e => e.EventDate),
                "date_desc" => events.OrderByDescending(e => e.EventDate),
                "Location" => events.OrderBy(e => e.Location),
                "location_desc" => events.OrderByDescending(e => e.Location),
                "Participants" => events.OrderBy(e => e.Registrations.Count),
                "participants_desc" => events.OrderByDescending(e => e.Registrations.Count),
                _ => events.OrderBy(e => e.EventDate) // по умолчанию сортировка по дате
            };

            // Пагинация - 6 событий на страницу
            int pageSize = 6;
            return View(await PaginatedList<Event>.CreateAsync(events.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events
                .Include(e => e.Registrations)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            if (User.Identity.IsAuthenticated)
            {
                var currentUserId = _userManager.GetUserId(User);
                ViewBag.IsRegistered = await _context.Registrations
                    .AnyAsync(r => r.EventId == id && r.ApplicationUserId == currentUserId);
                ViewBag.CurrentUserId = currentUserId;
            }
            else
            {
                ViewBag.IsRegistered = false;
                ViewBag.CurrentUserId = null;
            }

            return View(ev);
        }

        [Authorize(Policy = "UserPolicy")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int id)
        {
            var eventItem = await _context.Events.FindAsync(id);
            if (eventItem == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            var existingRegistration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == id && r.ApplicationUserId == currentUserId);

            if (existingRegistration != null)
            {
                TempData["Error"] = "You are already registered for this event.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var currentRegistrations = await _context.Registrations
                .CountAsync(r => r.EventId == id);

            if (currentRegistrations >= eventItem.MaxParticipants)
            {
                TempData["Error"] = "This event is already full.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var registration = new Registration
            {
                EventId = id,
                ApplicationUserId = currentUserId!,
                RegistrationDate = DateTime.UtcNow
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Successfully registered for the event!";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Policy = "UserPolicy")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unregister(int id)
        {
            var currentUserId = _userManager.GetUserId(User);

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == id && r.ApplicationUserId == currentUserId);

            if (registration != null)
            {
                _context.Registrations.Remove(registration);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Successfully unregistered from the event.";
            }
            else
            {
                TempData["Error"] = "You are not registered for this event.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Policy = "AdminPolicy")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event ev, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    ev.ImageUrl = await SaveImageAsync(imageFile);
                }

                ev.CreatedAt = DateTime.UtcNow;
                _context.Add(ev);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ev);
        }

        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            return View(ev);
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event ev, IFormFile? imageFile)
        {
            if (id != ev.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingEvent = await _context.Events.FindAsync(id);
                    if (existingEvent == null) return NotFound();

                    existingEvent.Title = ev.Title;
                    existingEvent.Description = ev.Description;
                    existingEvent.EventDate = ev.EventDate;
                    existingEvent.Location = ev.Location;
                    existingEvent.MaxParticipants = ev.MaxParticipants;
                    existingEvent.UpdatedAt = DateTime.UtcNow;

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(existingEvent.ImageUrl))
                        {
                            DeleteImage(existingEvent.ImageUrl);
                        }
                        existingEvent.ImageUrl = await SaveImageAsync(imageFile);
                    }

                    _context.Update(existingEvent);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(ev.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(ev);
        }

        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();

            return View(ev);
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev != null)
            {
                if (!string.IsNullOrEmpty(ev.ImageUrl))
                {
                    DeleteImage(ev.ImageUrl);
                }
                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "events");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return $"/images/events/{uniqueFileName}";
        }

        private void DeleteImage(string imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var filePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }
    }
}