using INKO.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace INKO.Services
{
    public class DatabaseInitializer
    {
        public static async Task SeedDataAsync(UserManager<ApplicationUser>? userManager,
            RoleManager<IdentityRole>? roleManager)
        {
            if(userManager == null || roleManager==null)
            {
                Console.WriteLine("userManager or roleManager is null => exit");
                return;
            }

            // proveravamo da li imamo admin rolu ili ne, a zatim ako ne postoji kreiramo admin rolu
            var exists = await roleManager.RoleExistsAsync("admin");
            if(!exists)
            {
                Console.WriteLine("Admin role is not defined and it will be created");
                await roleManager.CreateAsync(new IdentityRole("admin"));
            }


            // proveravamo da li imamo seller rolu ili ne, a zatim ako ne postoji kreiramo seller rolu
            exists = await roleManager.RoleExistsAsync("seller");
            if (!exists)
            {
                Console.WriteLine("Seller role is not defined and it will be created");
                await roleManager.CreateAsync(new IdentityRole("seller"));
            }


            // proveravamo da li imamo client rolu ili ne, a zatim ako ne postoji kreiramo client rolu
            exists = await roleManager.RoleExistsAsync("client");
            if (!exists)
            {
                Console.WriteLine("Client role is not defined and it will be created");
                await roleManager.CreateAsync(new IdentityRole("client"));
            }

            // proveravamo da li imamo bar jednog admina ili ne
            var adminUsers = await userManager.GetUsersInRoleAsync("admin");
            if (adminUsers.Any())
            {
                // izlazimo ako vec postoji
                Console.WriteLine("Admin user already exists => exit");
                return;
            }

            // kreiranje admin usera
            var user = new ApplicationUser()
            {
                FirstName = "Admin",
                LastName = "Admin",
                UserName = "admin@admin.com",
                Email = "admin@admin.com",
                CreatedAt = DateTime.Now,
            };
            string initialPassword = "admin123";

            var result = await userManager.CreateAsync(user, initialPassword);
            if(result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "admin");
                Console.WriteLine("Admin user is created!");
                Console.WriteLine("Email: " + user.Email);
                Console.WriteLine("Password: " + initialPassword);
            }


        }





    }
}
