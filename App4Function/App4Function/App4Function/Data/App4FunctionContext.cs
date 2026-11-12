using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using App4Function.Models;

namespace App4Function.Data
{
    public class App4FunctionContext : DbContext
    {
        public App4FunctionContext(DbContextOptions<App4FunctionContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
    }
}
