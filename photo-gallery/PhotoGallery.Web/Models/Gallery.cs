using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PhotoGallery.Web.Models
{
    public class Gallery
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // The FK to AspNetUsers.Id
        [Required]
        public string OwnerId { get; set; } = string.Empty;

        public IdentityUser? Owner { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}