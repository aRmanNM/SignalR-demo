using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using idp.Entites;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace idp.Data
{
    public class DatabaseInitializer
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public DatabaseInitializer(
            AppDbContext dbContext,
            UserManager<User> userManager,
            RoleManager<Role> roleManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            if (!_dbContext.Database.IsInMemory())
                await _dbContext.Database.MigrateAsync();

            var roles = new List<Role>()
            {
                new Role() { Name = "Admin" },
                new Role() { Name = "User" }
            };

            foreach (var role in roles)
            {
                if (await _roleManager.RoleExistsAsync(role.Name))
                    continue;

                await _roleManager.CreateAsync(role);
            }

            var users = new List<User>()
            {
                new User() { UserName = "admin1", Email = "admin1@mail.com" },
                new User() { UserName = "user1", Email = "user1@mail.com" }
            };

            foreach (var user in users)
            {
                if ((await _userManager.FindByNameAsync(user.UserName)) != null)
                    continue;

                var res = await _userManager.CreateAsync(user, "P@ssw0rd");
                res = user.UserName switch
                {
                    "admin1" => await _userManager.AddToRoleAsync(user, "Admin"),
                    "user1" => await _userManager.AddToRoleAsync(user, "User"),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
    }
}