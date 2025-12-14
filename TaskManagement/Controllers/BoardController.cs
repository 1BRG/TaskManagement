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
            // Make sure to include Columns and Tasks for those columns
            var project = _context.Projects
                .Include(p => p.Columns) // Load columns
                .ThenInclude(c => c.Tasks) // Load tasks in columns
                .FirstOrDefault(p => p.Id == id && p.OrganizerId == userId);

            if (project == null)
            {
                return NotFound();
            }

            // Setup columns for view
            // We can just pass the project.Columns directly, but let's order them
            project.Columns = project.Columns.OrderBy(c => c.Order).ToList();

            return View(project);
        }

        [HttpPost]
        public IActionResult AddCard(int projectId, int columnId, string title, PriorityEnum priority)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Validate Project ownership
            var project = _context.Projects.FirstOrDefault(p => p.Id == projectId && p.OrganizerId == userId);
            if (project == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(title))
            {
                // Find Max Order in this column
                var maxOrder = _context.AppTasks
                    .Where(t => t.ProjectId == projectId && t.BoardColumnId == columnId)
                    .Max(t => (int?)t.Order) ?? 0;

                var task = new AppTask
                {
                    Title = title,
                    Description = "No description", 
                    Priority = priority,
                    BoardColumnId = columnId,
                    ProjectId = projectId,
                    Status = TaskStatusEnum.NotStarted,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(7),
                    Order = maxOrder + 1
                };

                _context.AppTasks.Add(task);
                _context.SaveChanges();
            }
            
            return RedirectToAction("Index", new { id = projectId });
        }

        [HttpPost]
        public IActionResult AddColumn(int projectId, string title)
        {
             var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
             var project = _context.Projects.Include(p => p.Columns).FirstOrDefault(p => p.Id == projectId && p.OrganizerId == userId);
             if (project == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(title))
            {
                var maxOrder = project.Columns.Any() ? project.Columns.Max(c => c.Order) : 0;

                var column = new BoardColumn
                {
                    Title = title,
                    ProjectId = projectId,
                    Order = maxOrder + 1
                };
                
                _context.BoardColumns.Add(column);
                _context.SaveChanges();
            }
            
            return RedirectToAction("Index", new { id = projectId });
        }

        [HttpPost]
        public IActionResult MoveCard(int taskId, int targetColumnId, int index)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var task = _context.AppTasks.Include(t => t.Project).FirstOrDefault(t => t.Id == taskId);
            
            if (task == null) return NotFound();
            if (task.Project.OrganizerId != userId) return Unauthorized(); 

            // Reordering logic
            var tasksInColumn = _context.AppTasks
                .Where(t => t.ProjectId == task.ProjectId && t.BoardColumnId == targetColumnId && t.Id != taskId)
                .OrderBy(t => t.Order)
                .ToList();

            task.BoardColumnId = targetColumnId;
            
            if (index < 0) index = 0;
            if (index > tasksInColumn.Count) index = tasksInColumn.Count;
            
            tasksInColumn.Insert(index, task);

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
            task.Status = task.IsCompleted ? TaskStatusEnum.Completed : TaskStatusEnum.InProgress;
            
            _context.SaveChanges();

            return Ok();
        }
    }
}
