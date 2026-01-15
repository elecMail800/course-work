using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VolunteerPlatform.Web.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "Max Participants")]
        [Range(1, 1000, ErrorMessage = "Maximum participants must be between 1 and 1000")]
        public int MaxParticipants { get; set; }

        // Новое поле для картинки
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}