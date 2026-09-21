using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TextEditor.Data;
using TextEditor.Models;

namespace TextEditor.Controllers
{
    [Authorize]
    public class DocsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DocsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Docs 
        /// Admins and Moderators see every document, while regular users see only theirs.
        
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var query = _context.Docs.Include(d => d.User).AsQueryable()
                        .Where(d => d.UserId == userId);

            return View(await query.ToListAsync());
        }

        // GET: Docs/Create 
        public IActionResult Create() => View();

        // POST: Docs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Content")] Doc doc)
        {
            // Always stamp the current user; never trust the form field.
            doc.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? throw new InvalidOperationException("User not authenticated.");

            // Remove UserId from ModelState so validation passes.
            ModelState.Remove(nameof(doc.UserId));

            if (ModelState.IsValid)
            {
                _context.Add(doc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(doc);
        }

        // GET: Docs/Edit/5 
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var doc = await _context.Docs.FindAsync(id);
            if (doc == null) return NotFound();

            if (!CanModify(doc)) return Forbid();

            return View(doc);
        }

        // POST: Docs/Edit/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,UserId")] Doc doc)
        {
            if (id != doc.Id) return NotFound();

            var existing = await _context.Docs.AsNoTracking()
                               .FirstOrDefaultAsync(d => d.Id == id);
            if (existing == null) return NotFound();
            if (!CanModify(existing)) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    // Preserve the original owner even if form is tampered.
                    doc.UserId = existing.UserId;
                    _context.Update(doc);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DocExists(doc.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(doc);
        }

        // GET: Docs/Delete/5 
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var doc = await _context.Docs
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doc == null) return NotFound();
            if (!CanModify(doc)) return Forbid();

            return View(doc);
        }

        // ── POST: Docs/Delete/5 
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doc = await _context.Docs.FindAsync(id);
            if (doc == null) return NotFound();
            if (!CanModify(doc)) return Forbid();

            _context.Docs.Remove(doc);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helpers
        private bool DocExists(int id) =>
            _context.Docs.Any(e => e.Id == id);

       
        private bool CanModify(Doc doc)
        {
            if (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Moderator))
                return true;

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return doc.UserId == currentUserId;
        }
    }
}