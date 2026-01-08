using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UsersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> IndexUsers()
        {
            var users = await db.Users.Include(a => a.OwnedProjects).
                Include(a => a.ProjectsJoined).
                    ThenInclude(a => a.Project).
                Include(a => a.AssignedTasks)
                .ToListAsync();

            var viewModel = new List<UserProfileViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var projectCount = user.OwnedProjects.Count + user.ProjectsJoined.Count;
                var tasksCount = user.AssignedTasks.Count;
                var allProjects = user.OwnedProjects.Concat(user.ProjectsJoined.Select(p => p.Project)).Distinct().ToList();
                viewModel.Add(new UserProfileViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = roles.FirstOrDefault() ?? "User",
                    ProjectsCount = projectCount,
                    TasksCount = tasksCount,
                    Projects = allProjects,
                    Tasks = user.AssignedTasks.ToList(),
                    IsOwnerOrAdmin = User.IsInRole("Admin") || User.Identity.Name == user.Email
                });
            }
            return View(viewModel);
        }

        [Authorize(Roles = "Admin, User")]
        public async Task<IActionResult> UserProfile(string id)
        {
            if(id == null)
            {
                return NotFound();
            }
            var user = await db.Users.Include(p => p.OwnedProjects).
                Include(p => p.ProjectsJoined).
                    ThenInclude(p => p.Project).
                Include(p => p.AssignedTasks).ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if(user == null)
                    return NotFound();
            var currentUser = await _userManager.GetUserAsync(User);
            var isOwnerOrAdmin = User.IsInRole("Admin") || user == currentUser;
            var roles = await _userManager.GetRolesAsync(user);

            var allProjects = user.OwnedProjects.Concat(user.ProjectsJoined.Select(p => p.Project)).Distinct().ToList();
            var viewModel = new UserProfileViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "User",
                Projects = allProjects,
                Tasks = user.AssignedTasks,
                ProjectsCount = allProjects.Count,
                TasksCount = user.AssignedTasks.Count,
                IsOwnerOrAdmin = isOwnerOrAdmin
            };
            return View(viewModel);
        }

    }
}
