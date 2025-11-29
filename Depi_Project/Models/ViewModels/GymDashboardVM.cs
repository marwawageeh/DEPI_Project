namespace Depi_Project.Models.ViewModels
{
	public class GymDashboardVM
	{
		public Gym Gym { get; set; }
		public List<Booking> RecentBookings { get; set; }

		public int TotalBookings { get; set; }
		public decimal TotalRevenue { get; set; }
		public double AvgRating { get; set; }
		public int ReviewsCount { get; set; }
	}
}
