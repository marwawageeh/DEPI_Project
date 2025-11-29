namespace Depi_Project.Models.ViewModels
{
    public class GymBookingsVM
    {
        public List<BookingInfoVM> Bookings { get; set; }
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Confirmed { get; set; }
        public int Cancelled { get; set; }

        public string Filter { get; set; }
        public string Search { get; set; }
    }
    public class BookingInfoVM
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public BookingType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
    }
}
