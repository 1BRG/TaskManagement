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
            if(db.Projects.Find(id) != null && db.Projects.Find(id).Members.Any(m => m.UserId == _userManager.GetUserId(User)) == false && !User.IsInRole("Admin"))
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
    }
}
