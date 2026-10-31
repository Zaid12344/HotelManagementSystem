using HotelManagement.Models.Entities;
using HotelManagement.Models.Enums;
using HotelManagement.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Models.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        // ---------- 1. Roles ----------
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // ---------- 2. Users (one per role) ----------
        // Format: mail = role@hotel.com | pass = Role@123 (uppercase first letter)
        var demoUsers = new (string Name, string Email, string Password, string Role)[]
        {
            ("System Administrator", "admin@hotel.com",        "Admin@123",        Roles.Admin),
            ("Ravi Menon",           "manager@hotel.com",      "Manager@123",      Roles.Manager),
            ("Priya Singh",          "frontdesk@hotel.com",    "Frontdesk@123",    Roles.FrontDesk),
            ("Anita Kaur",           "housekeeping@hotel.com", "Housekeeping@123", Roles.Housekeeping),
            ("Suresh Iyer",          "accountant@hotel.com",   "Accountant@123",   Roles.Accountant),
        };

        foreach (var (name, email, password, role) in demoUsers)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing == null)
            {
                var u = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(u, password);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(u, role);
            }
        }

        // ---------- 3. Rooms ----------
        if (!db.Rooms.Any())
        {
            var rooms = new List<Room>
            {
                // Floor 1 — Singles
                new() { RoomNumber = "101", Type = RoomType.Single, Rate = 1500, Floor = 1, Status = RoomStatus.Available, Amenities = "WiFi, AC, TV" },
                new() { RoomNumber = "102", Type = RoomType.Single, Rate = 1500, Floor = 1, Status = RoomStatus.Available, Amenities = "WiFi, AC, TV" },
                new() { RoomNumber = "103", Type = RoomType.Single, Rate = 1500, Floor = 1, Status = RoomStatus.Available, Amenities = "WiFi, AC, TV" },

                // Floor 2 — Doubles
                new() { RoomNumber = "201", Type = RoomType.Double, Rate = 2500, Floor = 2, Status = RoomStatus.Available, Amenities = "WiFi, AC, TV, Mini Bar" },
                new() { RoomNumber = "202", Type = RoomType.Double, Rate = 2500, Floor = 2, Status = RoomStatus.Available, Amenities = "WiFi, AC, TV, Mini Bar" },
                new() { RoomNumber = "203", Type = RoomType.Double, Rate = 2500, Floor = 2, Status = RoomStatus.Available, Amenities = "WiFi, AC, TV, Mini Bar" },

                // Floor 3 — Deluxe
                new() { RoomNumber = "301", Type = RoomType.Deluxe, Rate = 4000, Floor = 3, Status = RoomStatus.Available, Amenities = "WiFi, AC, Smart TV, Mini Bar, Balcony" },
                new() { RoomNumber = "302", Type = RoomType.Deluxe, Rate = 4000, Floor = 3, Status = RoomStatus.Available, Amenities = "WiFi, AC, Smart TV, Mini Bar, Balcony" },

                // Floor 4 — Suites
                new() { RoomNumber = "401", Type = RoomType.Suite, Rate = 7000, Floor = 4, Status = RoomStatus.Available, Amenities = "WiFi, AC, Smart TV, Kitchen, Jacuzzi, Balcony" },
                new() { RoomNumber = "402", Type = RoomType.Suite, Rate = 7000, Floor = 4, Status = RoomStatus.Available, Amenities = "WiFi, AC, Smart TV, Kitchen, Jacuzzi, Balcony" },
            };
            db.Rooms.AddRange(rooms);
            await db.SaveChangesAsync();
        }

        // ---------- 4. Services ----------
        if (!db.Services.Any())
        {
            var serviceItems = new List<Service>
            {
                new() { Name = "Room Service - Breakfast", Category = ServiceCategory.Food,        Price = 350,  Description = "Continental breakfast delivered to room" },
                new() { Name = "Room Service - Lunch",     Category = ServiceCategory.Food,        Price = 550,  Description = "Hot lunch platter" },
                new() { Name = "Room Service - Dinner",    Category = ServiceCategory.Food,        Price = 650,  Description = "Hot dinner platter" },
                new() { Name = "Laundry - Per Kg",         Category = ServiceCategory.Laundry,     Price = 200,  Description = "Wash & fold, per kg" },
                new() { Name = "Dry Cleaning - Shirt",     Category = ServiceCategory.Laundry,     Price = 150,  Description = "Per shirt" },
                new() { Name = "Spa - Full Body Massage",  Category = ServiceCategory.Spa,         Price = 2500, Description = "60-minute full body massage" },
                new() { Name = "Spa - Facial",             Category = ServiceCategory.Spa,         Price = 1800, Description = "45-minute facial" },
                new() { Name = "Airport Pickup",           Category = ServiceCategory.Transport,   Price = 1200, Description = "One-way airport to hotel" },
                new() { Name = "Airport Drop",             Category = ServiceCategory.Transport,   Price = 1200, Description = "One-way hotel to airport" },
                new() { Name = "Extra Bed",                Category = ServiceCategory.RoomService, Price = 800,  Description = "Per night" },
            };
            db.Services.AddRange(serviceItems);
            await db.SaveChangesAsync();
        }
    }
}