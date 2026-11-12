using System.ComponentModel.DataAnnotations;

namespace App4Function.Models
{
    public class Order
    {
        [Key] public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public Product? Products { get; set; }
        public Customer? Customers { get; set; }
        public string ProductName { get; set; }
        public string CustomerName { get; set; }
    }
}
