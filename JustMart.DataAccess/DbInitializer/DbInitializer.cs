using JustMart.DataAccess.Data;
using JustMart.Models;
using JustMart.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustMart.DataAccess.DbInitializer {
    public class DbInitializer : IDbInitializer {

        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public DbInitializer(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db) {
            _roleManager = roleManager;
            _userManager = userManager;
            _db = db;
        }


        public void Initialize() {


            //migrations if they are not applied
            try {
                if (_db.Database.GetPendingMigrations().Count() > 0) {
                    _db.Database.Migrate();
                }
            }
            catch(Exception ex) { }



            //create roles if they are not created
            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult()) {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();


                //Create default admin user
                _userManager.CreateAsync(new ApplicationUser {
                    UserName = "admin@justmart.com",
                    Email = "admin@justmart.com",
                    Name = "JUST Mart Admin",
                    PhoneNumber = "1112223333",
                    StreetAddress = "Admin Street 123",
                    State = "Dhaka",
                    PostalCode = "1200",
                    City = "Dhaka"
                }, "Admin123*").GetAwaiter().GetResult();

                ApplicationUser adminUser = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@justmart.com");
                _userManager.AddToRoleAsync(adminUser, SD.Role_Admin).GetAwaiter().GetResult();

                //Create default customer user
                _userManager.CreateAsync(new ApplicationUser {
                    UserName = "customer@justmart.com",
                    Email = "customer@justmart.com",
                    Name = "Test Customer",
                    PhoneNumber = "0171234567",
                    StreetAddress = "Customer Road 456",
                    State = "Dhaka",
                    PostalCode = "1205",
                    City = "Dhaka"
                }, "Admin123*").GetAwaiter().GetResult();

                ApplicationUser customerUser = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "customer@justmart.com");
                _userManager.AddToRoleAsync(customerUser, SD.Role_Customer).GetAwaiter().GetResult();

                //Create default company user
                _userManager.CreateAsync(new ApplicationUser {
                    UserName = "company@justmart.com",
                    Email = "company@justmart.com",
                    Name = "Test Company User",
                    PhoneNumber = "0181234567",
                    StreetAddress = "Company Avenue 789",
                    State = "Dhaka",
                    PostalCode = "1210",
                    City = "Dhaka",
                    CompanyId = 1
                }, "Admin123*").GetAwaiter().GetResult();

                ApplicationUser companyUser = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "company@justmart.com");
                _userManager.AddToRoleAsync(companyUser, SD.Role_Company).GetAwaiter().GetResult();

            }

            return;
        }
    }
}
