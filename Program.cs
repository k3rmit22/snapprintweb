using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add session services
builder.Services.AddDistributedMemoryCache(); // This is necessary for session state
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout as needed
    options.Cookie.HttpOnly = true; // Make the session cookie HTTP only for security
    options.Cookie.IsEssential = true; // Mark the session cookie as essential
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/upload/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable session middleware - ensure this line is before `app.UseAuthorization()` and `app.MapControllerRoute()`
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Upload}/{action=Index}/{id?}");

// Run the application on the specified IP address and port
app.Run("http://192.168.137.1:5082");
