using Depi_Project.Data;
using Depi_Project.Models;
using Depi_Project.Models.ViewModels;
using Depi_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;



namespace Depi_Project.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _paymentService;

        public BookingsController(ApplicationDbContext db, IPaymentService paymentService)
        {
            _db = db;
            _paymentService = paymentService;
        }


		[HttpGet]
		public async Task<IActionResult> Create(int gymId)
		{
			// 1) Load gym + trainers
			var gym = await _db.Gyms
				.Include(g => g.Trainers)
				.FirstOrDefaultAsync(g => g.Id == gymId);

			if (gym == null) return NotFound();

			// 2) Build session options dynamically 
			var sessionOptions = new List<GymSessionOption>();

			if (gym.PricePerDay.HasValue)
				sessionOptions.Add(new GymSessionOption
				{
					Type = BookingType.ByDay,
					Name = "Day Pass",
					Price = gym.PricePerDay.Value,
					Description = "Full access for 1 day"
				});

			if (gym.PricePerMonth.HasValue)
				sessionOptions.Add(new GymSessionOption
				{
					Type = BookingType.ByMonth,
					Name = "Monthly Membership",
					Price = gym.PricePerMonth.Value,
					Description = "Unlimited access for 30 days"
				});

			if (gym.PriceWithTrainer.HasValue)
				sessionOptions.Add(new GymSessionOption
				{
					Type = BookingType.WithTrainer,
					Name = "With Personal Trainer",
					Price = gym.PriceWithTrainer.Value,
					Description = "1 hour session with a trainer"
				});

			// 3) Build ViewModel
			var vm = new CreateBookingViewModel
			{
				GymId = gym.Id,
				GymName = gym.Name,
				SessionOptions = sessionOptions,
				Price = sessionOptions.First().Price,
				Type = sessionOptions.First().Type,
				SelectedDate = DateTime.Today,
				Trainers = gym.Trainers.ToList()   // لو هتختاري مدرب
			};

			return View(vm);
		}



		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(CreateBookingViewModel vm)
		{
			//if (!ModelState.IsValid)
			//	return View(vm);

			var booking = new Booking
			{
				GymId = vm.GymId,
				UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
				Type = vm.Type,
				StartDate = vm.SelectedDate.Date.Add(TimeSpan.Parse(vm.SelectedTime)),
				EndDate = vm.SelectedDate.Date.Add(TimeSpan.Parse(vm.SelectedTime)).AddHours(1),
				Amount = vm.Price
			};

			await _db.Bookings.AddAsync(booking);
			await _db.SaveChangesAsync();

			return RedirectToAction(nameof(Pay), new { id = booking.Id });
		}


		public async Task<IActionResult> Pay(int id)
		{
			var booking = await _db.Bookings
				.Include(b => b.Gym)
				.Include(b => b.Trainer)
				.FirstOrDefaultAsync(b => b.Id == id);

			if (booking == null)
				return NotFound();

			if (booking.IsPaid)
				return RedirectToAction(nameof(Details), new { id });

			// إنشاء عملية الدفع
			var paymentResult = await _paymentService.CreatePaymentIntentAsync(booking.Amount, booking.Id);

			// تمرير البيانات للـ View
			var vm = new PaymentViewModel
			{
				Booking = booking,
				ClientSecret = paymentResult.ClientSecret,
				PublishableKey = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Stripe:PublishableKey"]
			};

			return View(vm);
		}

		public async Task<IActionResult> PaymentSuccess(int id)
		{
			var booking = await _db.Bookings
				.Include(b => b.Gym)
				.Include(b => b.Trainer)
				.FirstOrDefaultAsync(b => b.Id == id);

			if (booking == null)
				return NotFound();

			return View(booking);
		}

		public async Task<IActionResult> ConfirmPayment(int id, string transactionId)
        {
            var booking = await _db.Bookings
                .Include(b => b.Gym)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound();

            // التحقق من الدفع
            var ok = await _paymentService.VerifyPaymentAsync(transactionId);
            if (!ok)
            {
                TempData["Error"] = "عملية الدفع لم تكتمل بنجاح.";
                return RedirectToAction(nameof(Pay), new { id });
            }

            // تحديث حالة الدفع
            booking.IsPaid = true;
            booking.PaymentReceiptUrl = $"receipts/{transactionId}";
            await _db.SaveChangesAsync();

            // بعد الدفع: رجّع صفحة Success فيها processing ثم redirect
            return View("PaymentSuccess", booking);
        }


        //  إضافة صفحة تفاصيل الحجز لتجنب الخطأ CS0103
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _db.Bookings
                .Include(b => b.Gym)
                .Include(b => b.Trainer)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound();

            return View(booking);
        }
    }
}

