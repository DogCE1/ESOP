using Microsoft.AspNetCore.Authentication.Cookies;
using ESOP.Services;

namespace ESOP
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            
            // Add services to the container.
            //Deliverable 1 + 4
            builder.Services.AddControllersWithViews(); //Register MVC controllers and views
            builder.Services.AddSingleton<StaffService>();  //one shared instance so all requests read/ write the same in-memory staff list
            
            //Deliverable 3 + 4
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>  //Configs cookie-based auth and where to send unauth users
            {
                options.LoginPath = "/Access/Login";
            });

            
            var app = builder.Build();
            
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Access/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            
            app.UseHttpsRedirection();
            app.UseRouting();
            
            //Deliverable 3
            //This Auth order matter, must first know who the user is before deciding what they are allowed to do
            app.UseAuthentication(); //Comes first
            app.UseAuthorization();  //Comes second

            app.MapStaticAssets();
            
            //Deliverable 1
            app.MapControllerRoute(  //Default route pattern, falls back to the login page
                name: "default",
                pattern: "{controller=Access}/{action=Login}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
