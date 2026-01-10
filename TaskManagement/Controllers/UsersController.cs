using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;
using TaskManagement.ViewModels;

public class ToggleTaskDto
{
    public int Id { get; set; }
}

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

                var tasksCount = user.AssignedTasks.Count;
                var allProjects = user.OwnedProjects.Concat(user.ProjectsJoined.Select(p => p.Project)).Distinct().ToList();
                var projectCount = allProjects.Count();
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
            if (id == null)
            {
                return NotFound();
            }

            if (db.Users.Where(u => u.Id == id) == null)
                return NotFound();

            var currentUser = _userManager.GetUserId(User);

            var user = await db.Users.Where(u => u.Id == id).
                Include(p => p.OwnedProjects).
                Include(p => p.ProjectsJoined.Where(pj => pj.Project.Members.Any(u => u.UserId == currentUser))).
                    ThenInclude(p => p.Project).
                Include(p => p.AssignedTasks.Where(t => !t.IsArchived)).ThenInclude(t => t.Project)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }





            var isOwnerOrAdmin = User.IsInRole("Admin") || user == await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            //var allProjects = user.OwnedProjects.Concat(user.ProjectsJoined.Select(p => p.Project)).Distinct().ToList();
            var allProjects = user.ProjectsJoined.Select(p => p.Project).Distinct().ToList();

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



        [Authorize(Roles = "Admin, User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTaskCompletion([FromBody] ToggleTaskDto dto)
        {
            if (dto == null || dto.Id <= 0)
                return BadRequest(new { success = false, error = "Payload invalid" });

            var task = await db.AppTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == dto.Id);
            if (task == null)
                return NotFound(new { success = false, error = "Task not found" });

            // permisii: admin sau user asignat
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && task.AssignedToUserId != currentUserId)
                return Forbid();

            // toggle
            task.IsCompleted = !task.IsCompleted;
            task.Status = task.IsCompleted ? TaskStatusEnum.Completed : TaskStatusEnum.InProgress;
            task.EndDate = DateTime.Now;
            db.AppTasks.Update(task);
            await db.SaveChangesAsync();

            return new JsonResult(new { success = true, isCompleted = task.IsCompleted });
        }






        // 1. AFISARE PAGINA (GET)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Securitate: Doar proprietarul sau Adminul intra
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser.Id != user.Id && !await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                return Forbid();
            }

            var model = new UserEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        // 2. SALVARE DATE (POST)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditProfile(UserEditViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // Actualizam doar datele non-critice
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            await _userManager.UpdateAsync(user);

            TempData["Message"] = "Profil actualizat cu succes!";
            return RedirectToAction("UserProfile", new { id = user.Id });
        }
    }
}
