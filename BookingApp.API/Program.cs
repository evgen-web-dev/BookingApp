using BookingApp.API;
using BookingApp.Application;
using BookingApp.Infrastructure.Seeders;
using BookingApp.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddControllers((options =>
{
    options.Filters.AddAppFilters();
}));

builder.Services.AddExceptionHandlersWithProblemDetails();

builder.Services.AddApplicationMapping();
builder.Services.AddApplicationServices();
builder.Services.AddTokenFamilyOptions(builder.Configuration);

builder.Services.AddInfrastructureServices();
builder.Services.AddInfrastructurePersistence(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddAppValidation();

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
    
    await ApartmentsWithBookingsSeeder.SeedApartmentsWithBookingsAsync(app.Services);
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();