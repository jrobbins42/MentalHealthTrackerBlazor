using MentalHealthTrackerBlazor.Components;
using MentalHealthTrackerBlazor.Data;
using MentalHealthTrackerBlazor.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.SqlServer;
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
        options.LoginPath = "/";
        options.Cookie.Name = "MHT.Auth";
        // The auth cookie is never readable by script.
        options.Cookie.HttpOnly = true;
        // Only send the cookie over HTTPS once deployed. Development keeps SameAsRequest so the
        // plain http launch profile still works locally.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        // Lax still sends the cookie on top-level navigations, but not on cross-site form posts.
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Health data sits behind a short session instead of the 14-day default.
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddCascadingAuthenticationState();
// Use the built-in ServerAuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = false;
    options.Preload = false;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
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
    // Only accept a return path inside this app: an absolute or protocol-relative URL here would
    // let an attacker bounce a freshly authenticated user to their own site.
    string returnUrl = LocalUrl.Sanitize(model.ReturnUrl);

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
        return Results.LocalRedirect(returnUrl);
    }

    // If failed, redirect back to login page with an error query param
    return Results.Redirect($"/Login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl)}");
});

// POST only, with an antiforgery token, so a third-party page cannot sign the user out.
app.MapPost("/logout-handler", async (HttpContext context, IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest("Invalid or missing antiforgery token.");
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/");
});


app.Run();
public record LoginFormModel(string Username, string Password, string ReturnUrl);
