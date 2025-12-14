using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
            var projects = db.Projects.Find().Members.Any(m => m.UserId == userId) == false && isAdmin;
            ViewBag.Projects = projects;
            return View();
        }



        [Authorize(Roles = "Admin,User")]
        public IActionResult Show(int id)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            if (db.Projects.Find(id) != null && (User.IsInRole("Admin") || db.Projects.Find(id).Members.Any(m => m.UserId == userId)))
            {
                ViewBag.Project = db.Projects.Find(id);
                return View();
            }

            // adaugare mesaj de eroare - nu esti membru
            return View("Home/Index");
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
                return RedirectToAction("Index", "Dashboard");
            }
            RedirectToAction("Index", "Dashboard");
            // Daca ceva a esuat, returnam formularul cu datele completate
            return View(project);
        }

        [Authorize]
        public IActionResult Edit(int id)
        {
            if(db.Projects.Find(id) == null || (db.Projects.Find(id).OrganizerId != _userManager.GetUserId(User) && !User.IsInRole("Admin")))
            {
                // mesaj de eroare - nu esti organizator sau admin
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
            if (ModelState.IsValid)
            {
                var existingProject = db.Projects.Find(project.Id);
                if (existingProject == null || (existingProject.OrganizerId != _userManager.GetUserId(User) && !User.IsInRole("Admin")))
                {
                    // mesaj de eroare - nu esti organizator sau admin
                    return RedirectToAction("Index", "Dashboard");
                }
                existingProject = project;
                db.SaveChanges();
                //mesaj de succes
                return View(existingProject);
            }
            return View();
        }

        [HttpPost]
        [Authorize (Roles = "Admin, User")]
        public IActionResult AddMember(int projectId, string email)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var project = db.Projects.Find(projectId);
            var existingUser = db.Users.FirstOrDefault(u => u.Email == email);
            if (project != null && (User.IsInRole("Admin") || project.OrganizerId == userId) && existingUser != null)
            {
                var projectMember = new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = existingUser.Id,
                    JoinedDate = DateTime.Now
                };
                db.ProjectMembers.Add(projectMember);
                db.SaveChanges();
                //mesaj de succes
            }

            //mesaj de eroare
            return RedirectToAction("Edit", project);
        }
    }
}
