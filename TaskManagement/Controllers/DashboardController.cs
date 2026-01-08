using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
        public async Task<IActionResult> Index(int ?page)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = _userManager.IsInRoleAsync(await _userManager.GetUserAsync(User), "Admin").Result;

            int pageSize = 6; 
            int pageNumber = page ?? 1;


            var query = db.Projects
                               .Include(p => p.Organizer)
                               .Where(p => p.Members.Any(pm => pm.UserId == userId) || isAdmin);

            var projectsList = await query
                               .OrderByDescending(p => p.CreatedDate)
                               .Skip((pageNumber - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;



            ViewBag.User = _userManager.GetUserAsync(User).Result;


            /*
            var adminUser = _userManager.GetUserId(User);
            var adminProject = new Project
            {
                Title = "Proiect Demo Admin",
                Description = "Proiect creat automat pentru testare task-uri.",
                CreatedDate = DateTime.Now,
                OrganizerId = adminUser
            };
            db.Projects.Add(adminProject);
            db.SaveChanges();

            if (!db.AppTasks.Any(t => t.Title == "Task Critic - Fix Login"))
            {
                db.AppTasks.Add(new AppTask
                {
                    Title = "Task Critic - Fix Login",
                    Description = "Userii nu se pot loga pe Safari. Prioritate maxima!",
                    Status = TaskStatusEnum.InProgress,
                    Priority = PriorityEnum.High,      // <--- AICI TESTAM PRIORITATEA
                    IsCompleted = false,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(1),
                    ProjectId = adminProject.Id,
                    AssignedToUserId = adminUser,
                    Order = 1,
                    BoardColumnId = null
                });
            }

            // TASK 2: Prioritate MEDIUM
            if (!db.AppTasks.Any(t => t.Title == "Update Documentatie"))
            {
                db.AppTasks.Add(new AppTask
                {
                    Title = "Update Documentatie",
                    Description = "Trebuie actualizat Readme.md cu noile instructiuni.",
                    Status = TaskStatusEnum.NotStarted,
                    Priority = PriorityEnum.Medium,    // <--- MEDIUM
                    IsCompleted = false,
                    StartDate = DateTime.Now.AddDays(1),
                    EndDate = DateTime.Now.AddDays(3),
                    ProjectId = adminProject.Id,
                    AssignedToUserId = adminUser,
                    Order = 2,
                    BoardColumnId = null
                });
            }

            // TASK 3: Prioritate LOW (Completed)
            var adminUser = _userManager.GetUserId(User);
            if (!db.AppTasks.Any(t => t.Title == "Descriere mare Cod"))
            {
                db.AppTasks.Add(new AppTask
                {
                    Title = "Curatenie Cod",
                    Description = "acum e cel mai bine sa scriu o descriere cat casa ca sa vad cat de avansat este acest proiect in condiitle in care nu am scris niciun view iar singurul html pe care l am scris vreodata a fost la tehnici web in anul 1 semestrul 1 la doamna profesoara CChirita, alo alo aho aho mos craciun cu plete dalbe a sosit de prin namtei si aduce " +
                    "daruri multe la fetite si batei",
                    Status = TaskStatusEnum.Completed,
                    Priority = PriorityEnum.Low,       // <--- LOW
                    IsCompleted = false,
                    StartDate = DateTime.Now.AddDays(-5),
                    EndDate = DateTime.Now.AddDays(-1),
                    ProjectId = 5,
                    AssignedToUserId = adminUser,
                    Order = 3,
                    BoardColumnId = null
                });
            }

            // Salvam modificarile
            db.SaveChanges();
            */
        
            

            return View(projectsList);

        }



        




    }
}
