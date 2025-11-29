using Depi_Project.Data;
using Depi_Project.Models;
using Depi_Project.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



namespace Depi_Project.Controllers
{
	public class UserController : Controller
	{

		private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
		{
			_context = context;
            _userManager = userManager;
        }


		[HttpGet]
		public IActionResult Search(string name, string address, string priceRange, string rating, double? lat, double? lng)
		{
			var gyms = _context.Gyms
						.Include(g => g.Reviews)
						.AsQueryable();

			if (!string.IsNullOrEmpty(name))
				gyms = gyms.Where(g => g.Name.Contains(name));

			if (!string.IsNullOrEmpty(address))
				gyms = gyms.Where(g => g.Address.Contains(address));

			// Price filter with null-check
			if (!string.IsNullOrEmpty(priceRange))
			{
				switch (priceRange)
				{
					case "under30":
						gyms = gyms.Where(g => g.PricePerMonth.HasValue && g.PricePerMonth.Value <= 30m);
						break;
					case "30to50":
						gyms = gyms.Where(g => g.PricePerMonth.HasValue && g.PricePerMonth.Value >= 30m && g.PricePerMonth.Value <= 50m);
						break;
					case "50to80":
						gyms = gyms.Where(g => g.PricePerMonth.HasValue && g.PricePerMonth.Value >= 50m && g.PricePerMonth.Value <= 80m);
						break;
					case "over80":
						gyms = gyms.Where(g => g.PricePerMonth.HasValue && g.PricePerMonth.Value >= 80m);
						break;
				}
			}

			// Rating filter - parse safely
			if (!string.IsNullOrEmpty(rating))
			{
				if (double.TryParse(rating, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double minRating))
				{
					gyms = gyms.Where(g => g.Reviews.Any() && g.Reviews.Average(r => r.Rating) >= minRating);
				}
			}

			//// Optional: order by distance if lat/lng provided
			//if (lat.HasValue && lng.HasValue)
			//{
			//	double latv = lat.Value, lngv = lng.Value;
			//	gyms = gyms.OrderBy(g =>
			//		Math.Sqrt(Math.Pow(g.Latitude - latv, 2) + Math.Pow(g.Longitude - lngv, 2))
			//	);
			//}

			var results = gyms.Take(50).ToList();
			return View(results);
		}

		[HttpPost]
        public IActionResult Search(GymSearchViewModel model)
        {
            var query = _context.Gyms.AsQueryable();

            if (!string.IsNullOrEmpty(model.Name))
                query = query.Where(g => g.Name.Contains(model.Name));

            if (!string.IsNullOrEmpty(model.Address))
                query = query.Where(g => g.Address.Contains(model.Address));

            //if (model.MinPrice.HasValue)
            //    query = query.Where(g => g.Media.Any(m => m.Price >= model.MinPrice.Value));

            //if (model.MaxPrice.HasValue)
            //    query = query.Where(g => g.Media.Any(m => m.Price <= model.MaxPrice.Value));

            if (model.MinRating.HasValue)
                query = query.Where(g => g.Reviews.Any() && g.Reviews.Average(r => r.Rating) >= model.MinRating.Value);

            model.Results = query
                .Include(g => g.Reviews)
                .Include(g => g.Media)
                .Take(50) // تحديد أعلى 50 نتيجة
                .ToList();

            return View(model);
        }


		public IActionResult Details(int id)
		{
			// نجيب الجيم مع الـ related collections اللي نحتاجها
			var gym = _context.Gyms
				.Include(g => g.Media)
				.Include(g => g.Reviews)
				.Include(g => g.Trainers)
				.FirstOrDefault(g => g.Id == id);

			if (gym == null)
				return NotFound();

			// نحوّل البيانات إلى ViewModel آمن (نتعامل مع null collections)
			var model = new GymDetailsViewModel
			{
				Id = gym.Id,
				Name = gym.Name,
				Address = gym.Address,
				Description = gym.Description,
				Latitude = gym.Latitude,
				Longitude = gym.Longitude,
				OpeningHours = gym.OpeningHours,
				Email = gym.Email,
				Phone = gym.Phone,

				Images = (gym.Media ?? Enumerable.Empty<GymMedia>())
							.Where(m => string.Equals(m.Type, "Image", StringComparison.OrdinalIgnoreCase))
							.Select(m => m.Url)
							.ToList(),
				Rating = (gym.Reviews != null && gym.Reviews.Any()) ? gym.Reviews.Average(r => r.Rating) : 0,
				ReviewsCount = gym.Reviews?.Count ?? 0,
				Reviews = gym.Reviews?.ToList() ?? new List<Review>(),
				Trainers = gym.Trainers?.ToList() ?? new List<Trainer>()
			};

			return View(model);
		}


        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = _userManager.GetUserId(User);

            // 1) Total Spent
            var totalSpent = await _context.Bookings
                .Where(b => b.UserId == userId && b.IsPaid == true)
                .SumAsync(b => (decimal?)b.Amount ?? 0);

            // 2) Upcoming Bookings
            var upcomingBookings = await _context.Bookings
                .Where(b => b.UserId == userId && b.StartDate >= DateTime.Now)
                .CountAsync();

            // 3) Favorite gyms list
            var favoriteGyms = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Gym)
                    .ThenInclude(g => g.Media)
                .Include(f => f.Gym)
                    .ThenInclude(g => g.Reviews)
                .Select(f => f.Gym)
                .ToListAsync();

            // 4) Gyms
            var gyms = await _context.Gyms
                .Include(g => g.Media)
                .Include(g => g.Reviews)
                .Take(6)
                .ToListAsync();

            // View Model
            var model = new UserDashboardViewModel
            {
                UserName = user.FullName,
                TotalSpent = totalSpent,
                UpcomingBookings = upcomingBookings,
                FavoriteGyms = favoriteGyms.Count,
                Gyms = gyms,
                Favorite = favoriteGyms
            };

            return View(model);
        }

		//[HttpPost]
		//public async Task<IActionResult> ToggleFavorite(int gymId)
		//{
		//	var userId = _userManager.GetUserId(User);

		//	var existing = await _context.Favorites
		//		.FirstOrDefaultAsync(f => f.UserId == userId && f.GymId == gymId);

		//	if (existing != null)
		//	{
		//		_context.Favorites.Remove(existing);
		//	}
		//	else
		//	{
		//		_context.Favorites.Add(new Favorite
		//		{
		//			UserId = userId,
		//			GymId = gymId
		//		});
		//	}

		//	await _context.SaveChangesAsync();
		//	return RedirectToAction("Details", "Gym", new { id = gymId });
		//}



		// GET: /User/Profile
		public async Task<IActionResult> Profile()
        {
            // جلب المستخدم الحالي من الـ Claims (بعد login)
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            // جلب عدد الحجوزات لهذا المستخدم
            var totalBookings = await _context.Bookings.CountAsync(b => b.UserId == currentUser.Id);

            // جلب آخر 5 حجوزات للعرض 
            var bookings = await _context.Bookings
                                .Where(b => b.UserId == currentUser.Id)
                                .OrderByDescending(b => b.CreatedAt)
                                .Take(5)
                                .ToListAsync();

            var model = new ProfileViewModel
            {
                Id = currentUser.Id,
                FullName = currentUser.FullName,
                Email = currentUser.Email,
                PhoneNumber = currentUser.PhoneNumber,
                IsProfilePublic = currentUser.IsProfilePublic,
                TotalBookings = totalBookings,
                Bookings = bookings
            };

            return View(model);
        }

        // POST: /User/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // لو فيه أخطاء في الفاليديشن، رجّع الصفحة بنفس الموديل
                return View("Profile", model);
            }

            // تأكّد إن المستخدم الحالي هو نفس صاحب البروفايل (حماية)
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.Id != model.Id)
                return Forbid();

            currentUser.FullName = model.FullName;
            currentUser.IsProfilePublic = model.IsProfilePublic;

            if (model.PhoneNumber != currentUser.PhoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(currentUser, model.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    ModelState.AddModelError("", "Failed to update phone number.");
                    return View("Profile", model);
                }
            }

            
            if (!string.Equals(model.Email, currentUser.Email, StringComparison.OrdinalIgnoreCase))
            {
                
                var token = await _userManager.GenerateChangeEmailTokenAsync(currentUser, model.Email);
                var changeEmailResult = await _userManager.ChangeEmailAsync(currentUser, model.Email, token);
                if (!changeEmailResult.Succeeded)
                {
                    ModelState.AddModelError("", "Failed to change email.");
                    return View("Profile", model);
                }

                // أيضاً نحدّث UserName إذا كنتِ تستخدمين البريد كـ UserName:
                currentUser.UserName = model.Email;
            }

            //  نحدّث السجل في الـ DB
            var updateResult = await _userManager.UpdateAsync(currentUser);
            if (!updateResult.Succeeded)
            {
                ModelState.AddModelError("", "Failed to update profile.");
                return View("Profile", model);
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddReview(int gymId, int rating, string comment)
		{
			// التأكد من تسجيل دخول المستخدم
			var userId = _userManager.GetUserId(User);
			if (string.IsNullOrEmpty(userId))
			{
				return RedirectToAction("Login", "Account");
			}

			// التحقق من صحة البيانات
			if (rating < 1 || rating > 5)
			{
				TempData["ErrorMessage"] = "Rating must be between 1 and 5.";
				return RedirectToAction("Details", new { id = gymId });
			}

			if (string.IsNullOrWhiteSpace(comment))
			{
				TempData["ErrorMessage"] = "Comment cannot be empty.";
				return RedirectToAction("Details", new { id = gymId });
			}

			// التحقق من وجود الجيم
			var gymExists = await _context.Gyms.AnyAsync(g => g.Id == gymId);
			if (!gymExists)
			{
				return NotFound();
			}

			// إنشاء المراجعة الجديدة
			var review = new Review
			{
				GymId = gymId,
				UserId = userId,
				Rating = rating,
				Comment = comment,
				CreatedAt = DateTime.Now
			};

			_context.Reviews.Add(review);
			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Your review has been added successfully!";
			return RedirectToAction("Details", new { id = gymId });
		}
	}
}
