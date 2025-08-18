using System;
using System.ComponentModel.DataAnnotations;

namespace PhotoGallery.Web.Models
{
    public class Photo
    {
        public int Id { get; set; }


        [Required]
        public int GalleryId { get; set; }
        public Gallery? Gallery { get; set; }

        [Required, StringLength(260)]
        public string OriginalPath { get; set; } = string.Empty;
        [StringLength(260)]
        public string? ThumbPath { get; set; }

        [StringLength(100)]
        public string? ContentType { get; set; }

        public long SizeBytes { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        
        [MaxLength(2048)]
        public string? StorageKey { get; set; }         

        [MaxLength(2048)]
        public string? ThumbStorageKey { get; set; }    

        [MaxLength(64)]
        public string? StorageProvider { get; set; }    
    }
}