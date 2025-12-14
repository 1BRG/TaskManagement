using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers
{
    public class DashboardController : Controller
    {

        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public DashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }



        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // User cookie exists but user is not in DB (e.g. after reset)
                // Force logout and redirect
                await ((Microsoft.AspNetCore.Identity.SignInManager<ApplicationUser>)HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Identity.SignInManager<ApplicationUser>))).SignOutAsync();
                return RedirectToAction("Index", "Home");
            }

            var userId = user.Id;
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            
            var projectsList = await db.Projects
                               .Include(p => p.Organizer)
                               .Where(p => p.Members.Any(pm => pm.UserId == userId) || isAdmin)
                               .ToListAsync();
                               
            ViewBag.User = user;
            return View(projectsList);
        }





    }
}
