using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account"); // Or similar

            // Sync query
            var projects = _context.Projects.Where(p => p.OrganizerId == userId).ToList();
            return View(projects);
        }

        [HttpPost]
        public IActionResult Create(Project model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Force User ID
            model.OrganizerId = userId;
            model.Description ??= ""; // Handle null description

            // Clear validation for Organizer and Description (optional)
            ModelState.Remove(nameof(model.Organizer));
            ModelState.Remove(nameof(model.OrganizerId));
             ModelState.Remove(nameof(model.Description));
             
             // FIX: Remove validation for navigation collections which are empty on creation
             ModelState.Remove(nameof(model.Tasks));
             ModelState.Remove(nameof(model.Members));

            if (ModelState.IsValid)
            {
                _context.Projects.Add(model);
                _context.SaveChanges(); // Sync save
                return RedirectToAction(nameof(Index)); // Loop back to list
            }
            
            // Log errors for debugging
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key].Errors;
                foreach (var error in errors)
                {
                    System.Console.WriteLine($"Validation Error - Key: {key}, Error: {error.ErrorMessage}");
                }
            }

            // Reload list to show in view
            var projects = _context.Projects.Where(p => p.OrganizerId == userId).ToList();
            ViewData["ShowCreateError"] = true; // Flag to open modal or show alert
            return View("Index", projects); 
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
             var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
             
             var project = _context.Projects.FirstOrDefault(p => p.Id == id && p.OrganizerId == userId);
             if (project != null)
             {
                 _context.Projects.Remove(project);
                 _context.SaveChanges();
             }
             
             return RedirectToAction(nameof(Index));
        }
    }
}
