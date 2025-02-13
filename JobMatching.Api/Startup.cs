using Microsoft.AspNetCore.Authentication.Google;
using AspNet.Security.OAuth.LinkedIn;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using JobMatching.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

public class Startup
{
    public IConfiguration Configuration { get; }
    public Startup(IConfiguration configuration) => Configuration = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        // Configure Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

        // Configure Identity
        services.AddIdentity<User, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Configure JWT Authentication
        var secretKey = Configuration["Jwt:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured.");
        }
        var key = Encoding.UTF8.GetBytes(secretKey);
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ExternalScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        })
        .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = Configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId is not configured.");
            options.ClientSecret = Configuration["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret is not configured.");
        })

        .AddLinkedIn(LinkedInAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = Configuration["Authentication:LinkedIn:ClientId"] ?? throw new InvalidOperationException("LinkedIn ClientId is not configured.");
            options.ClientSecret = Configuration["Authentication:LinkedIn:ClientSecret"] ?? throw new InvalidOperationException("LinkedIn ClientSecret is not configured.");
            options.Scope.Add("r_liteprofile");  // Basic Profile (Name, Picture)
            options.Scope.Add("r_emailaddress");  // Email
            options.Scope.Add("r_fullprofile");  // Full Profile Access (Skills, Work Experience)
            options.SaveTokens = true;
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("JobSeekerOnly", policy => policy.RequireRole("JobSeeker"));
            options.AddPolicy("RecruiterOnly", policy => policy.RequireRole("Recruiter"));
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

        });

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "JobMatching API",
                Version = "v1",
                Description = "API for Job Matching Platform, including payment processing with Stripe & M-Pesa."
            });

            // 🔥 Add Security Definitions for API Keys (Stripe & Auth)
            c.AddSecurityDefinition("stripe-signature", new OpenApiSecurityScheme
            {
                Name = "Stripe-Signature",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Stripe Webhook Signature Header"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "stripe-signature" }
                    },
                    Array.Empty<string>()
                }
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });
        });
    }
    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "JobMatching API v1");
                c.RoutePrefix = string.Empty; // Access Swagger at root: https://localhost:5000/
            });
        }
        app.UseRouting();
        app.UseCors("AllowAll");
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}
