using BookingApp.Application;
using BookingApp.Application.Interfaces;
using BookingApp.Domain;
using BookingApp.Infrastructure.Persistence;
using BookingApp.Infrastructure.Seeders;
using BookingApp.Application.Services;
using BookingApp.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddControllers();

builder.Services.AddApplicationMapping();
builder.Services.AddApplicationServices();

builder.Services.AddInfrastructureServices();
builder.Services.AddInfrastructurePersistence(builder.Configuration);

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