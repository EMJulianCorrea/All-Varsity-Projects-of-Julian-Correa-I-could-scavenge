using System.ComponentModel.DataAnnotations;

namespace App4Function.Models
{
    public class Customer
    {
        [Key] public int CustomerID { get; set; }
        [Required]
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        [Required]
        public int Age { get; set; }
        [Required]
        public string Address { get; set; }
    }
}
