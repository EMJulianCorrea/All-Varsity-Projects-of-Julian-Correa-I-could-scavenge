using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace App4Function.Models
{
    public class Product
    {
       [Key] public int ProductID { get; set; }
        [Required]
        public string ProductName { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        [Required]
        public double Price { get; set; }

    }
}
