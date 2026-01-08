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
            var userId = _userManager.GetUserId(User);
            var isAdmin = _userManager.IsInRoleAsync(await _userManager.GetUserAsync(User), "Admin").Result;
            var projectsList = await db.Projects
                               .Include(p => p.Organizer)
                               .Where(p => p.Members.Any(pm => pm.UserId == userId) || isAdmin)
                               .ToListAsync();
            ViewBag.User = _userManager.GetUserAsync(User).Result;
            return View(projectsList);
        }



        




    }
}
