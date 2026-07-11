using BookingApp.Application;
using BookingApp.Application.Interfaces;
using BookingApp.Domain;
using BookingApp.Infrastructure;
using BookingApp.Infrastructure.Seeders;
using BookingApp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<User>()
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddApplicationMapping();

builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

await IdentitySeeder.SeedRolesAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.DisableAgent();
        options.DisableDefaultFonts();
        options.DisableTelemetry();
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();