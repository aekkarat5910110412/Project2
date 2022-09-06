using System.ComponentModel.DataAnnotations;

namespace BlazorWebApp.Models
{
    public class Menu
    {
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        public float Price { get; set; }

        [Required(ErrorMessage = "Namemenu: Cannot be empty.")]
        public string Name { get; set; }

       
        public string? Tags { get; set; }

        [Required(ErrorMessage = "Size: Cannot be empty.")]
 
        public string? Size { get; set; }

        [Required(ErrorMessage = "Description: Cannot be empty.")]
        public string? Description { get; set; }


    }
}
