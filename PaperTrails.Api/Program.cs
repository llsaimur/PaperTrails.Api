using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaperTrails.Api.Data;
using PaperTrails.Api.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddHttpClient<PaperlessService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Authentication:ValidIssuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Authentication:ValidAudience"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Authentication:JwtSecret"]))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    var sub = claimsIdentity.FindFirst("sub")?.Value;
                    var email = claimsIdentity.FindFirst("email")?.Value;
                    var role = claimsIdentity.FindFirst("role")?.Value;

                    if (!string.IsNullOrEmpty(sub))
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, sub));

                    if (!string.IsNullOrEmpty(email))
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, email));

                    if (!string.IsNullOrEmpty(role))
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                }

                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
