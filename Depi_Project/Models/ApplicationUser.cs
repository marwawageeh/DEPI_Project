using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Depi_Project.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string Role { get; set; } // "User", "GymOwner", "Admin"
                                         // Profile settings
        public bool IsProfilePublic { get; set; } = true;
		public DateTime? CreatedAt { get; set; }
		public DateTime? LastLoginDate { get; set; }


		// Navigation
		public virtual ICollection<Booking> Bookings { get; set; }
        public virtual Gym Gym { get; set; } // if GymOwner
    }
}
