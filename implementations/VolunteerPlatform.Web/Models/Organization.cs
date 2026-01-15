using System;
using System.Collections.Generic;

namespace VolunteerPlatform.Web.Models
{
    public class Organization
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Cause> Causes { get; set; } = new List<Cause>();
    }

}