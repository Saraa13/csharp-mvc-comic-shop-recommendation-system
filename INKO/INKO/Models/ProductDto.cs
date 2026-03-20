
using System.ComponentModel.DataAnnotations;

namespace INKO.Models
{
    public class ProductDto
    {
        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Publisher { get; set; } = "";

        [Required]
        public List<string> Genres { get; set; } = new List<string>();


        [Required]
        public decimal Price { get; set; }

        [Required]
        public string Description { get; set; } = "";

        public IFormFile? ImageFile { get; set; }
    }
}
