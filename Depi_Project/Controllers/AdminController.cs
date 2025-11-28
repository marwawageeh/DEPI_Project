using Depi_Project.Data;
using Depi_Project.Models;
using Depi_Project.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Depi_Project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager; _db = db;
        }

        public async Task< IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalGyms = await _db.Gyms.CountAsync(),
                TotalBookings = await _db.Bookings.CountAsync(),
                TotalRevenue = await _db.Bookings.SumAsync(b => b.Amount)

            };

            return View(model);
        }

        public async Task<IActionResult> Accounts()
        {
            var users = await _userManager.Users.ToListAsync();

            var model = new List<AdminAccountViewModel>();


            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new AdminAccountViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "User",
                    IsActive = user.LockoutEnd == null || user.LockoutEnd < DateTime.UtcNow
                });
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAccount(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // لو Active → Suspend
            if (user.LockoutEnd == null || user.LockoutEnd < DateTime.UtcNow)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);//ازوده 100 سنه عشان الحساب يتقفل
            }
            else // لو Suspended → Activate
            {
                user.LockoutEnd = null;
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction("Accounts");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            return RedirectToAction("Accounts");
        }
    }
}
