using System.ComponentModel.DataAnnotations;

namespace BlazorWebApp.Models
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        public float TemperatureC { get; set; }

        [Required(ErrorMessage = "Summary: Cannot be empty.")]
        public string? Summary { get; set; }

        public float TemperatureF => 32.0F + (TemperatureC / 0.5556F);
    }
}
