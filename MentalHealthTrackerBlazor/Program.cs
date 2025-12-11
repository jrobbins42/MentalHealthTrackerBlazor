using MentalHealthTrackerBlazor.Components;
using MentalHealthTrackerBlazor.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRazorPages();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");


builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<EntryManager>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Define the path for the login page that handles the sign-in POST request
        options.LoginPath = "/Login";
    });
builder.Services.AddCascadingAuthenticationState();
// Use the built-in ServerAuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/login-handler", async (
    [Microsoft.AspNetCore.Mvc.FromForm] LoginFormModel model, // <-- Use the model bound from the form
    EntryManager entryManager,
    HttpContext context) =>
{
    // Access parameters via the model object now
    int userId = entryManager.ValidateUser(model.Username, model.Password);

    if (userId > 0)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, model.Username),
        };
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await context.SignInAsync(claimsPrincipal, new AuthenticationProperties { IsPersistent = true });

        // Redirect the browser to the original page
        return Results.Redirect(model.ReturnUrl ?? "/mht");
    }

    // If failed, redirect back to login page with an error query param
    return Results.Redirect($"/Login?error=invalid&returnUrl={Uri.EscapeDataString(model.ReturnUrl ?? "/")}");
});


app.Run();
public record LoginFormModel(string Username, string Password, string ReturnUrl);