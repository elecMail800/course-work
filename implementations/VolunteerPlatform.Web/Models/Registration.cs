using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VolunteerPlatform.Web.Models
{
    public class Registration
    {
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public Event Event { get; set; } = null!;

        [Required]
        public string ApplicationUserId { get; set; } = null!;  // ← ИЗМЕНИТЬ на ApplicationUserId!

        [ForeignKey("ApplicationUserId")]
        public ApplicationUser User { get; set; } = null!;

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}