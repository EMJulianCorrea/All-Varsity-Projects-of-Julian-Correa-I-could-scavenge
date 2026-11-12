using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using App4Function.Data;
using Azure.Storage.Blobs;
using Azure.Storage;
using Azure.Storage.Queues;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Azure.Identity;
using Azure.Data.Tables;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using System.Collections.Concurrent;
using Azure.Identity;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using App4Function.Service;
namespace App4Function
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Operation for Table storage
            TableClient customerClient = new TableClient("DefaultEndpointsProtocol = https; AccountName = newnewnewbull; AccountKey = zPW8uCsjDA0lL2Samq8mW3xnFln8i4 + TzW0 / 93l88rjcOW46pvey8dICqhC9ZdTRzu3MvR2uOdmB + ASts741yg ==; EndpointSuffix = core.windows.net", "Customers");
            var response = customerClient.GetEntityIfExists<TableEntity>("Male", "9");

            if (!response.HasValue)
            {
                var entity = new TableEntity("Male", "9")
                {
                    { "Name", "Gary" },
                    { "PhoneNumber", "058493058349583" },
                    { "Age", 33 },
                    { "Address", "Westville"}
                };
                customerClient.AddEntity(entity);
            }


            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<App4FunctionContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'App4FunctionContext' not found.")));
            //Blob Client
            builder.Services.AddSingleton(provider =>
     new BlobService(
         builder.Configuration["AzureBlobStorage:ConnectionString"],
         builder.Configuration["AzureBlobStorage:ContainerName"]
     ));
            //File Share Client
            builder.Services.AddSingleton<AzureFileShareService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
        
    }
}
