using ABCRetailers.Services;
using System.Globalization;

namespace ABCRetailers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register FunctionsApiClient with HttpClient configuration
            builder.Services.AddHttpClient<IFunctionsApi, FunctionsApiClient>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:7167/api/"); 
                client.Timeout = TimeSpan.FromMinutes(5);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            // Register StorageInitializationService with HttpClient configuration
            builder.Services.AddHttpClient<IStorageInitializationService, StorageInitializationService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:7167/api/"); // Functions base URL
            });

            // Register as hosted service for auto-initialization
            builder.Services.AddHostedService(provider =>
                (StorageInitializationService)provider.GetRequiredService<IStorageInitializationService>());

            // Add logging
            builder.Services.AddLogging();

            var app = builder.Build();

            // Set culture for decimal handling (fixes price issue)
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}