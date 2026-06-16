using System;
using System.ComponentModel.DataAnnotations;

namespace HomemadeCookie.Api.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }
        
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        public int CookieId { get; set; }
        
        [Required]
        public int CustomerId { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string? Comment { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}