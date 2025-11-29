using Depi_Project.Data;
using Depi_Project.Models;
using Depi_Project.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Depi_Project.Models.ViewModels;


namespace Depi_Project.Controllers
{
    [Authorize(Roles = "GymOwner")]
    public class GymOwnerController : Controller
    {
		private readonly ApplicationDbContext _db;
		public GymOwnerController(ApplicationDbContext db)
		{
			_db = db;
		}

		public async Task<IActionResult> Dashboard()
		{
			var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var gym = await _db.Gyms
				.Include(g => g.Bookings)
				.Include(g => g.Reviews)
				.Include(g => g.Media)
				.FirstOrDefaultAsync(g => g.OwnerId == ownerId);

			//if (gym == null)
			//	return RedirectToAction("Create", "Gym");

			var recentBookings = gym.Bookings
				.OrderByDescending(b => b.CreatedAt)
				.Take(5)
				.ToList();

			var totalBookings = gym.Bookings.Count();
			var totalRevenue = gym.Bookings.Where(b => b.IsPaid).Sum(b => b.Amount);
			var avgRating = gym.Reviews.Any() ? gym.Reviews.Average(r => r.Rating) : 0;

			var model = new GymDashboardVM
			{
				Gym = gym,
				RecentBookings = recentBookings,
				TotalBookings = totalBookings,
				TotalRevenue = totalRevenue,
				AvgRating = avgRating,
				ReviewsCount = gym.Reviews.Count()
			};

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> UploadMedia(int gymId, IFormFile file, string type)
		{
			if (file == null || file.Length == 0)
				return RedirectToAction("Media");

			// Folder path
			var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/assets");

			if (!Directory.Exists(folderPath))
				Directory.CreateDirectory(folderPath);

			// Unique file name
			var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
			var filePath = Path.Combine(folderPath, fileName);

			using (var stream = new FileStream(filePath, FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}

			var url = "/assets/" + fileName;

			// Save in DB
			var media = new GymMedia
			{
				GymId = gymId,
				Url = url,
				Type = type
			};

			_db.GymMedias.Add(media);
			await _db.SaveChangesAsync();

			return RedirectToAction("Media");
		}
		[HttpPost]
		public async Task<IActionResult> DeleteMedia(int id)
		{
			var media = await _db.GymMedias.FindAsync(id);

			if (media == null)
				return RedirectToAction("Media");

			// Delete file
			var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", media.Url.TrimStart('/'));

			if (System.IO.File.Exists(filePath))
				System.IO.File.Delete(filePath);

			_db.GymMedias.Remove(media);
			await _db.SaveChangesAsync();

			return RedirectToAction("Media");
		}

		public async Task<IActionResult> Media()
		{
			var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var gym = await _db.Gyms
				.Include(g => g.Media)
				.FirstOrDefaultAsync(g => g.OwnerId == ownerId);

			return View(gym);
		}


        public async Task<IActionResult> Bookings(string filter = "All", string search = "")
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var gym = await _db.Gyms
                .Include(g => g.Bookings)
                .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(g => g.OwnerId == ownerId);

            if (gym == null)
                return RedirectToAction("Dashboard");

            var bookings = gym.Bookings.AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                bookings = bookings.Where(b =>
                    b.User.FullName.Contains(search) ||
                    b.User.Email.Contains(search)
                );
            }

            // Filter
            bookings = filter switch
            {
                "Pending" => bookings.Where(b => !b.IsConfirmedByOwner && !b.IsCancelled),
                "Confirmed" => bookings.Where(b => b.IsConfirmedByOwner),
                "Cancelled" => bookings.Where(b => b.IsCancelled),
                _ => bookings
            };

            var model = new GymBookingsVM
            {
                Total = gym.Bookings.Count,
                Pending = gym.Bookings.Count(b => !b.IsConfirmedByOwner && !b.IsCancelled),
                Confirmed = gym.Bookings.Count(b => b.IsConfirmedByOwner),
                Cancelled = gym.Bookings.Count(b => b.IsCancelled),

                Filter = filter,
                Search = search,

                Bookings = bookings.Select(b => new BookingInfoVM
                {
                    Id = b.Id,
                    UserName = b.User.FullName,
                    UserEmail = b.User.Email,
                    Type = b.Type,
                    Amount = b.Amount,
                    Date = b.StartDate,
                    Status = b.IsCancelled ? "Cancelled" :
                             b.IsConfirmedByOwner ? "Confirmed" : "Pending"
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveBooking(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            booking.IsConfirmedByOwner = true;
            await _db.SaveChangesAsync();
            return RedirectToAction("Bookings");
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            booking.IsCancelled = true;
            await _db.SaveChangesAsync();
            return RedirectToAction("Bookings");
        }

    }
}
