using BookingApp.Application.DTOs.Auth;
using BookingApp.Domain;
using BookingApp.Domain.Entities;
using BookingApp.Infrastructure.Persistence;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingApp.Infrastructure.Seeders;

public static class ApartmentsWithBookingsSeeder
{
    public static async Task SeedApartmentsWithBookingsAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await appDbContext.Set<Apartment>().AnyAsync())
        {
            return; // data is already seeded, nothing to do
        }
        
        var hostUserId = await SeedHostUser(userManager, mapper);
        var clientUserId = await SeedClientUser(userManager, mapper);

        var apartmentsToSeed = PrepareApartments(hostUserId);
        
        appDbContext.Set<Apartment>().AddRange(apartmentsToSeed);

        PrepareBookingsForApartmentsInPlace(apartmentsToSeed, clientUserId);

        await appDbContext.SaveChangesAsync();
    }

    private static async Task<int> SeedHostUser(UserManager<User> userManager, IMapper mapper)
    {
        var seededHostUserPayload = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1999, 10, 10),
            Email = "host_seeded_user_john_doe@example.com",
            Password = "Pa$$word1",
            Role = Roles.Host
        };

        var seededHostUser = await userManager.FindByEmailAsync(seededHostUserPayload.Email);

        if (seededHostUser is not null)
        {
            return seededHostUser.Id;
        }
        
        seededHostUser = mapper.Map<User>(seededHostUserPayload);
        
        var createHostUserResult = await userManager.CreateAsync(seededHostUser, seededHostUserPayload.Password);
        if (!createHostUserResult.Succeeded)
        {
            throw new InvalidOperationException($"Could not create Host user in {nameof(ApartmentsWithBookingsSeeder)}.{nameof(SeedHostUser)}");
        }
        
        var addToRoleResult = await userManager.AddToRoleAsync(seededHostUser, seededHostUserPayload.Role);
        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException($"Could not add proper role for Host user in {nameof(ApartmentsWithBookingsSeeder)}.{nameof(SeedHostUser)}");
        }
        
        return seededHostUser.Id;
    }
    
    private static async Task<int> SeedClientUser(UserManager<User> userManager, IMapper mapper)
    {
        var seededClientUserPayload = new RegisterRequest
        {
            FirstName = "Bob",
            LastName = "Smith",
            DateOfBirth = new DateOnly(1995, 2, 2),
            Email = "client_seeded_user_bob_smith@example.com",
            Password = "Pa$$word1",
            Role = Roles.Client
        };
        
        var seededClientUser = await userManager.FindByEmailAsync(seededClientUserPayload.Email);

        if (seededClientUser is not null)
        {
            return seededClientUser.Id;
        }
        
        seededClientUser = mapper.Map<User>(seededClientUserPayload);
        
        var createClientUserResult = await userManager.CreateAsync(seededClientUser, seededClientUserPayload.Password);
        if (!createClientUserResult.Succeeded)
        {
            throw new InvalidOperationException($"Could not create Client user in {nameof(ApartmentsWithBookingsSeeder)}.{nameof(SeedClientUser)}");    
        }
        
        var addToRoleResult = await userManager.AddToRoleAsync(seededClientUser, seededClientUserPayload.Role);
        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException($"Could not add proper role for Client user in {nameof(ApartmentsWithBookingsSeeder)}.{nameof(SeedClientUser)}");
        }
        
        return seededClientUser.Id;
    }

    private static List<Apartment> PrepareApartments(int hostUserId)
    {
        const int seededApartmentCount = 10;
        
        var apartments = new List<Apartment>(seededApartmentCount);
        
        for (var i = 0; i < seededApartmentCount; i++)
        {
            var capacity = Random.Shared.Next(4, 30);
            
            apartments.Add(new Apartment
            {
                Location = $"New York, {Random.Shared.Next(4, 50)}th Avenue",
                Capacity = capacity,
                Description = $"Apartment to rent with capacity for {capacity} persons",
                Title = $"Apartment #{i + 1}",
                Price = Random.Shared.Next(50, 400),
                OwnerId = hostUserId,
            });
        }
        
        return apartments;
    }

    private static Booking BookingFactory(DateTime checkInDate, DateTime checkOutDate, int clientUserId)
    {
        return new Booking
        {
            ClientId = clientUserId,
            CheckIn = checkInDate,
            CheckOut = checkOutDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    /*
    Seeded booking data for apartments (bookings are in the range of Feb-Mar 2028):
    
    Apartment | Booking | CheckIn    | CheckOut   | Nights | Gap to next      | Purpose
    ----------+---------+------------+------------+--------+------------------+-----------------------------------------------------------------
    A1        | —       | —          | —          | —      | —                | Zero bookings — always available
    A2        | —       | —          | —          | —      | —                | Zero bookings — always available
    A3        | B1      | 2028-02-10 | 2028-02-14 | 4      | —                | Single short booking, simplest overlap case
    A4        | B1      | 2028-02-05 | 2028-02-25 | 20     | —                | Single long booking
    A5        | B1      | 2028-02-05 | 2028-02-08 | 3      | 0 (back-to-back) | Adjacent pair — B1 checkout = B2 checkin
    A5        | B2      | 2028-02-08 | 2028-02-14 | 6      | 11 days          | 
    A5        | B3      | 2028-02-25 | 2028-03-02 | 6      | —                | Isolated after gap
    A6        | B1      | 2028-02-02 | 2028-02-06 | 4      | 20 days          | Long gap
    A6        | B2      | 2028-02-26 | 2028-03-02 | 5      | 7 days           | Crosses Feb→Mar boundary
    A6        | B3      | 2028-03-09 | 2028-03-15 | 6      | —                | 
    A7        | B1      | 2028-02-01 | 2028-02-05 | 4      | 0 (back-to-back) | Adjacent pair
    A7        | B2      | 2028-02-05 | 2028-02-08 | 3      | 10 days          | 
    A7        | B3      | 2028-02-18 | 2028-03-09 | 20     | 6 days           | Long booking mid-sequence
    A7        | B4      | 2028-03-15 | 2028-03-19 | 4      | —                | 
    A8        | B1      | 2028-02-01 | 2028-02-05 | 4      | 2 days           | Densely packed, uniform length
    A8        | B2      | 2028-02-07 | 2028-02-11 | 4      | 2 days           | 
    A8        | B3      | 2028-02-13 | 2028-02-17 | 4      | 3 days           | 
    A8        | B4      | 2028-02-20 | 2028-02-24 | 4      | 4 days           | 
    A8        | B5      | 2028-02-28 | 2028-03-03 | 4      | —                | 
    A9        | B1      | 2028-02-15 | 2028-02-19 | 4      | 6 days           | 
    A9        | B2      | 2028-02-25 | 2028-03-04 | 8      | —                | 
    A10       | B1      | 2028-02-03 | 2028-03-25 | 51     | —                | Spans almost the entire window — obvious "always booked" control
    
    ===
    
    A1 | B1 in "Booking" column means it's the 1st booking for apartment #1,
    A7 | B3 in "Booking" column means it's the 3rd booking for apartment #7,
    etc
    
    */
    private static void PrepareBookingsForApartmentsInPlace(List<Apartment> trackedApartments, int clientUserId)
    {
        // Skipping seeding for first 2 apartments, so they are always available for booking

        // Apartment #3
        trackedApartments[2].Bookings = new List<Booking>([
            BookingFactory(new DateTime(2028, 02, 10), new DateTime(2028, 02, 14), clientUserId)
        ]);
        
        // Apartment #4
        trackedApartments[3].Bookings = new List<Booking>([
            BookingFactory(new DateTime(2028, 02, 05), new DateTime(2028, 02, 25), clientUserId)
        ]);
        
        // Apartment #5
        trackedApartments[4].Bookings = new List<Booking>([
            BookingFactory(new DateTime(2028, 02, 05), new DateTime(2028, 02, 08), clientUserId),
            BookingFactory(new DateTime(2028, 02, 08), new DateTime(2028, 02, 14), clientUserId),
            BookingFactory(new DateTime(2028, 02, 25), new DateTime(2028, 03, 02), clientUserId),
        ]);
        
        // Apartment #6
        trackedApartments[5].Bookings = new List<Booking>([
            BookingFactory(new DateTime(2028, 02, 02), new DateTime(2028, 02, 06), clientUserId),
            BookingFactory(new DateTime(2028, 02, 26), new DateTime(2028, 03, 02), clientUserId),
            BookingFactory(new DateTime(2028, 03, 09), new DateTime(2028, 03, 15), clientUserId),
        ]);
        
        // Apartment #7
        trackedApartments[6].Bookings = new List<Booking>([
            BookingFactory(new DateTime(2028, 02, 01), new DateTime(2028, 02, 05), clientUserId),
            BookingFactory(new DateTime(2028, 02, 05), new DateTime(2028, 02, 08), clientUserId),
            BookingFactory(new DateTime(2028, 02, 18), new DateTime(2028, 03, 09), clientUserId),
            BookingFactory(new DateTime(2028, 03, 15), new DateTime(2028, 03, 19), clientUserId),
        ]);
        
        // Apartment #8
        trackedApartments[7].Bookings = new List<Booking>([
            BookingFactory(new DateTime(2028, 02, 01), new DateTime(2028, 02, 05), clientUserId),
            BookingFactory(new DateTime(2028, 02, 07), new DateTime(2028, 02, 11), clientUserId),
            BookingFactory(new DateTime(2028, 02, 13), new DateTime(2028, 02, 17), clientUserId),
            BookingFactory(new DateTime(2028, 02, 20), new DateTime(2028, 02, 24), clientUserId),
            BookingFactory(new DateTime(2028, 02, 28), new DateTime(2028, 03, 03), clientUserId),
        ]);
        
        // Apartment #9
        trackedApartments[8].Bookings = new List<Booking>([
            BookingFactory(new DateTime(2028, 02, 15), new DateTime(2028, 02, 19), clientUserId),
            BookingFactory(new DateTime(2028, 02, 25), new DateTime(2028, 03, 04), clientUserId),
        ]);
        
        // Apartment #10
        trackedApartments[9].Bookings = new List<Booking>([
            BookingFactory(new DateTime(2028, 02, 03), new DateTime(2028, 03, 25), clientUserId),
        ]);
    }
}