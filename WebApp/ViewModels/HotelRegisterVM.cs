using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels
{
    public class HotelRegisterVM
    {
        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string Address { get; set; }
    }
}