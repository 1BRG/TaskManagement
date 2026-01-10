using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public ProjectsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        [Authorize(Roles = "Admin,User")]
        public IActionResult Index()
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            List<Project> projects = isAdmin
            ? db.Projects.Include(p => p.Members).ToList()
            : db.Projects
                .Include(p => p.Members)
                .Where(p => p.Members.Any(m => m.UserId == userId))
                .ToList();
            ViewBag.Projects = projects;
            return View();
        }



        [Authorize(Roles = "Admin,User")]
        public IActionResult Show(int id)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var project = db.Projects.Include(p => p.Members).FirstOrDefault(p => p.Id == id);
            if (db.Projects.Find(id) != null && (isAdmin || project.Members.Any(m => m.UserId == userId)))
            {
                ViewBag.Project = db.Projects.Find(id);
                return View();
            }

            // adaugare mesaj de eroare - nu esti membru
           TempData["Error"] = "Nu ai acces la acest proiect (nu eşti membru).";


            return View("Home/Index");
        }
        [HttpPost]
        [Authorize (Roles = "Admin, User")]
        public IActionResult Delete(int projectId)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var project = db.Projects.Find(projectId);

            if (isAdmin || project.OrganizerId == userId)
            {
                // Sterge toate entitatile legate de proiect
                var projectMembers = db.ProjectMembers.Where(pm => pm.ProjectId == projectId);
                db.ProjectMembers.RemoveRange(projectMembers);
                var boardColumns = db.BoardColumns.Where(bc => bc.ProjectId == projectId);
                foreach (var column in boardColumns)
                {
                    var tasks = db.AppTasks.Where(t => t.BoardColumnId == column.Id);
                    db.AppTasks.RemoveRange(tasks);
                }
                db.BoardColumns.RemoveRange(boardColumns);
                db.Projects.Remove(project);
                db.SaveChanges();
                TempData["Success"] = "Proiectul a fost sters cu succes.";
                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                TempData["Error"] = "Nu ai drept de organizator la acest proiect!";
                return RedirectToAction("Index", "Dashboard");
            }

        }


        [Authorize(Roles = "Admin,User")]
        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        [Authorize (Roles = "Admin,User")]
        public IActionResult New(Project project)
        {
            project.OrganizerId = _userManager.GetUserId(User);
            project.CreatedDate = DateTime.Now;
            ModelState.Remove(nameof(project.OrganizerId));
            ModelState.Remove(nameof(project.Organizer));
            ModelState.Remove(nameof(project.Members));
            ModelState.Remove(nameof(project.Tasks));
            ModelState.Remove(nameof(project.Columns));
            if (ModelState.IsValid)
            {
                db.Projects.Add(project);
                db.SaveChanges();
                var projectMember = new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = _userManager.GetUserId(User),
                    JoinedDate = DateTime.Now
                };
                db.ProjectMembers.Add(projectMember);
                db.SaveChanges();
                TempData["Success"] = "Proiectul a fost creat cu succes.";
                return RedirectToAction("Index", "Dashboard");
            }
            // Daca ceva a esuat, returnam formularul cu datele completate
            return View(project);
        }

        [Authorize]
        public IActionResult Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            if (db.Projects.Find(id) == null || (db.Projects.Find(id).OrganizerId != userId && !isAdmin))
            {
                // mesaj de eroare - nu esti organizator sau admin
                TempData["message"] = "Nu ai drept de organizator la acest proiect!";
                return RedirectToAction("Index", "Dashboard");
            }
            var project = db.Projects.Find(id);
            var members = db.ProjectMembers.Where(pm => pm.ProjectId == id).ToList();
            ViewBag.Members = members;
            return View(project);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, User")]
        public IActionResult Edit(Project project)
        {
            var ceva = 0;
            ModelState.Remove(nameof(project.OrganizerId));
            ModelState.Remove(nameof(project.Organizer));
            ModelState.Remove(nameof(project.Members));
            ModelState.Remove(nameof(project.Tasks));
            ModelState.Remove(nameof(project.Columns));
            if (ModelState.IsValid)
            {
                var existingProject = db.Projects.Find(project.Id);
                if (existingProject == null || (existingProject.OrganizerId != _userManager.GetUserId(User) && !User.IsInRole("Admin")))
                {
                    TempData["Error"] = "Nu ai drept de organizator la acest proiect!";
                    return RedirectToAction("Index", "Dashboard");
                }
                existingProject.Title = project.Title;
                existingProject.Description = project.Description;

                db.SaveChanges();
                //mesaj de succes
                TempData["message"] = "Proiectul a fost editat cu succes";
                return RedirectToAction("Edit", existingProject);
            }
            return RedirectToAction("Edit", new { id = project.Id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public IActionResult AddMember(int projectId, string email)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var project = db.Projects.Find(projectId);

            if (project == null || !(isAdmin || project.OrganizerId == userId))
            {
                TempData["Error"] = "Proiectul nu exista sau nu ai drept de editare!";
                return RedirectToAction("Edit", new { id = projectId });
            }

            var existingUser = db.Users.FirstOrDefault(u => u.Email == email);
            if (existingUser == null)
            {
                TempData["Error"] = $"Utilizatorul cu email {email} nu exista!";
                return RedirectToAction("Edit", new { id = projectId });
            }

            var isAlreadyMember = db.ProjectMembers.Any(pm => pm.ProjectId == projectId && pm.UserId == existingUser.Id);
            if (isAlreadyMember)
            {
                TempData["Error"] = $"{existingUser.Email} este deja membru!";
                return RedirectToAction("Edit", new { id = projectId });
            }

            var projectMember = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = existingUser.Id,
                JoinedDate = DateTime.Now
            };
            db.ProjectMembers.Add(projectMember);
            db.SaveChanges();

            TempData["Success"] = $"L-ai adaugat cu succes pe {existingUser.Email} în proiect!";
            return RedirectToAction("Edit", new { id = projectId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public IActionResult RemoveMember(int projectId, string removedUserId)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var project = db.Projects.Find(projectId);

            if (project == null || !(isAdmin || project.OrganizerId == userId))
            {
                TempData["Error"] = "Proiectul nu exista sau nu ai drept de editare!";
                return RedirectToAction("Edit", new { id = projectId });
            }

            if (removedUserId == userId)
            {
                TempData["Error"] = "Nu te poți elimina pe tine însuți.";
                return RedirectToAction("Edit", new { id = projectId });
            }

            var existingUser = db.Users.FirstOrDefault(u => u.Id == removedUserId);
            if (existingUser == null)
            {
                TempData["Error"] = "Utilizatorul nu exista!";
                return RedirectToAction("Edit", new { id = projectId });
            }

            var projectMember = db.ProjectMembers.FirstOrDefault(pm => pm.ProjectId == projectId && pm.UserId == removedUserId);
            if (projectMember == null)
            {
                TempData["Error"] = "Utilizatorul nu este membru al proiectului.";
                return RedirectToAction("Edit", new { id = projectId });
            }

            db.ProjectMembers.Remove(projectMember);
            db.SaveChanges();

            TempData["Success"] = $"{existingUser.Email} a fost eliminat cu succes";
            return RedirectToAction("Edit", new { id = projectId });
        }

    }

}
