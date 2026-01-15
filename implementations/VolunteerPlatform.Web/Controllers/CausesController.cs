using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VolunteerPlatform.Web.Data;
using VolunteerPlatform.Web.Models;

namespace VolunteerPlatform.Web.Controllers
{
    [Authorize]
    public class CausesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CausesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var causes = await _context.Causes
                .Include(c => c.Organization)
                .ToListAsync();

            return View(causes);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cause = await _context.Causes
                .Include(c => c.Organization)
                .Include(c => c.Events)
                .Include(c => c.Feedbacks)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cause == null) return NotFound();

            return View(cause);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            // «агружаем список организаций дл€ выпадающего списка
            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Cause cause)
        {
            // ѕроверка: дата начала должна быть раньше даты окончани€
            if (cause.StartDate >= cause.EndDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(cause);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // ≈сли ошибка валидации, заново загружаем список организаций
            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "Id", "Name", cause.OrganizationId);
            return View(cause);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cause = await _context.Causes.FindAsync(id);
            if (cause == null) return NotFound();

            // «агружаем список организаций дл€ выпадающего списка
            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "Id", "Name", cause.OrganizationId);
            return View(cause);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Cause cause)
        {
            if (id != cause.Id) return NotFound();

            // ѕроверка: дата начала должна быть раньше даты окончани€
            if (cause.StartDate >= cause.EndDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cause);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CauseExists(cause.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // ≈сли ошибка валидации, заново загружаем список организаций
            ViewData["OrganizationId"] = new SelectList(_context.Organizations, "Id", "Name", cause.OrganizationId);
            return View(cause);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cause = await _context.Causes
                .Include(c => c.Organization)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cause == null) return NotFound();

            return View(cause);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cause = await _context.Causes.FindAsync(id);
            if (cause != null)
            {
                _context.Causes.Remove(cause);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CauseExists(int id)
        {
            return _context.Causes.Any(e => e.Id == id);
        }
    }
}