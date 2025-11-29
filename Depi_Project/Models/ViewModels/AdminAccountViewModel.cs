namespace Depi_Project.Models.ViewModels
{
	public class AdminAccountViewModel
	{
		public string Id { get; set; }
		public string FullName { get; set; }
		public string Email { get; set; }
		public string Role { get; set; }
		public DateTime CreatedAt { get; set; }
		public bool IsActive { get; set; }
		public DateTime? LastLogin { get; set; }
	}
}
