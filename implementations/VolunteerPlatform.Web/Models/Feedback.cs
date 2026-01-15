using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VolunteerPlatform.Web.Models
{
    public class Feedback
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = null!; // ← ИЗМЕНИТЬ на string

        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? User { get; set; } // ← ИЗМЕНИТЬ на ApplicationUser

        public int CauseId { get; set; }

        [ForeignKey("CauseId")]
        public Cause? Cause { get; set; }

        [Required]
        [StringLength(2000)]
        public string Comment { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}