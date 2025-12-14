using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Controllers
{
    [Authorize]
    public class BoardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BoardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Sync Query with Include
            var project = _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefault(p => p.Id == id && p.OrganizerId == userId);

            if (project == null)
            {
                return NotFound();
            }

            // Dynamic columns from tasks + defaults
            var columns = project.Tasks
                .Select(t => t.ColumnName)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            var defaultColumns = new List<string> { "To Do", "In Progress", "Done" };
            foreach (var def in defaultColumns)
            {
                if (!columns.Contains(def))
                {
                    columns.Add(def);
                }
            }

            ViewBag.Columns = columns;

            return View(project);
        }

        [HttpPost]
        public IActionResult AddCard(int projectId, string columnName, string title, PriorityEnum priority)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Validate Project ownership
            var project = _context.Projects.FirstOrDefault(p => p.Id == projectId && p.OrganizerId == userId);
            if (project == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(title))
            {
                // Find Max Order in this column to append
                var maxOrder = _context.AppTasks
                    .Where(t => t.ProjectId == projectId && t.ColumnName == columnName)
                    .Max(t => (int?)t.Order) ?? 0;

                var task = new AppTask
                {
                    Title = title,
                    Description = "", // Required by model? 
                    Priority = priority,
                    ColumnName = columnName,
                    ProjectId = projectId,
                    Status = TaskStatusEnum.NotStarted,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(7),
                    Order = maxOrder + 1
                };
                
                // Existing Model AppTask has [Required] Description.
                // We must provide a dummy description or change the model.
                // Model is "AppTask.cs" ... [Required] public string Description { get; set; }
                task.Description = "No description"; 

                _context.AppTasks.Add(task);
                _context.SaveChanges();
            }
            
            return RedirectToAction("Index", new { id = projectId });
        }

        [HttpPost]
        public IActionResult AddColumn(int projectId, string title)
        {
             // Verify project
             var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
             var project = _context.Projects.FirstOrDefault(p => p.Id == projectId && p.OrganizerId == userId);
             if (project == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(title))
            {
                // Create a placeholder task to persist the column
                var placeholder = new AppTask
                {
                    Title = "__COLUMN_PLACEHOLDER__",
                    Description = "System Task for Column Persistence",
                    Priority = PriorityEnum.Low,
                    ColumnName = title,
                    ProjectId = projectId,
                    Status = TaskStatusEnum.NotStarted,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now,
                    Order = -1 // Keep at top/hidden usually
                };
                
                _context.AppTasks.Add(placeholder);
                _context.SaveChanges();
            }
            
            return RedirectToAction("Index", new { id = projectId });
        }

        [HttpPost]
        public IActionResult MoveCard(int taskId, string targetColumn, int index)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var task = _context.AppTasks.Include(t => t.Project).FirstOrDefault(t => t.Id == taskId);
            
            if (task == null) return NotFound();
            if (task.Project.OrganizerId != userId) return Unauthorized(); // Check ownership

            // Reordering logic
            // Get all tasks in target column
            var tasksInColumn = _context.AppTasks
                .Where(t => t.ProjectId == task.ProjectId && t.ColumnName == targetColumn && t.Id != taskId)
                .OrderBy(t => t.Order)
                .ToList();

            // Update moved task
            task.ColumnName = targetColumn;
            
            // Insert into list
            if (index < 0) index = 0;
            if (index > tasksInColumn.Count) index = tasksInColumn.Count;
            
            tasksInColumn.Insert(index, task);

            // Reassign orders
            for (var i = 0; i < tasksInColumn.Count; i++)
            {
                tasksInColumn[i].Order = i;
            }

            _context.SaveChanges();
            
            return Ok();
        }

        [HttpPost]
        public IActionResult ToggleCard(int taskId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
             
            var task = _context.AppTasks.Include(t => t.Project).FirstOrDefault(t => t.Id == taskId);
            if (task == null) return NotFound();
             if (task.Project.OrganizerId != userId) return Unauthorized();

            task.IsCompleted = !task.IsCompleted;
            // Also update Status enum?
            task.Status = task.IsCompleted ? TaskStatusEnum.Completed : TaskStatusEnum.InProgress;
            
            _context.SaveChanges();

            return Ok();
        }
    }
}
