using System.ComponentModel.DataAnnotations;

namespace JustMart.Models
{
    public class Coupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Coupon Code")]
        [StringLength(20)]
        public string Code { get; set; }

        [Required]
        [Display(Name = "Discount Percentage")]
        [Range(1, 100)]
        public double DiscountPercent { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
