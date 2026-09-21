using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TextEditor.Data;
using TextEditor.Models;

namespace TextEditor.Controllers
{

    /// A regular user who navigates here is redirected to Access Denied.
    
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET /Admin
       
        public async Task<IActionResult> Index(string? statusMessage = null)
        {
            // Load all users together with their doc counts in one query.
            var users = await _userManager.Users
                .Include(u => u.Docs)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            var summaries = new List<UserSummaryViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLocked = await _userManager.IsLockedOutAsync(user);

                summaries.Add(new UserSummaryViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    DisplayName = user.FriendlyName,
                    CreatedAt = user.CreatedAt,
                    DocumentCount = user.Docs.Count,
                    Roles = roles,
                    IsLockedOut = isLocked
                });
            }

            var vm = new AdminUsersViewModel
            {
                Users = summaries,
                StatusMessage = statusMessage
            };

            return View(vm);
        }

        // POST /Admin/SetRole 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRole(string userId, string role)
        {
            if (!AppRoles.All.Contains(role))
                return BadRequest("Invalid role.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Prevent an admin from demoting themselves.
            if (user.Id == _userManager.GetUserId(User))
                return RedirectToAction(nameof(Index),
                    new { statusMessage = "You cannot change your own role." });

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            return RedirectToAction(nameof(Index),
                new { statusMessage = $"Role updated to '{role}' for {user.FriendlyName}." });
        }

        // POST /Admin/ToggleLock 
        //Locks a user out until year 3000, or removes an existing lockout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (user.Id == _userManager.GetUserId(User))
                return RedirectToAction(nameof(Index),
                    new { statusMessage = "You cannot lock your own account." });

            string message;
            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                message = $"{user.FriendlyName} has been unlocked.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                message = $"{user.FriendlyName} has been locked.";
            }

            return RedirectToAction(nameof(Index), new { statusMessage = message });
        }

        // ── POST /Admin/DeleteUser 
        ///Permanently deletes a user and all their documents.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (user.Id == _userManager.GetUserId(User))
                return RedirectToAction(nameof(Index),
                    new { statusMessage = "You cannot delete your own account." });

            var name = user.FriendlyName;
            await _userManager.DeleteAsync(user);

            return RedirectToAction(nameof(Index),
                new { statusMessage = $"User '{name}' has been deleted." });
        }
    }
}