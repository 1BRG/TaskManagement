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
        private readonly Services.AiStrategistService _aiService;

        public BoardController(ApplicationDbContext context, Services.AiStrategistService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        public IActionResult Index(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Load project with columns and non-archived tasks
            var project = _context.Projects
                .Include(p => p.Columns)
                .ThenInclude(c => c.Tasks.Where(t => !t.IsArchived)) // Filter out archived tasks
                .ThenInclude(t => t.Labels)
                .Include(p => p.Columns)
                .ThenInclude(c => c.Tasks.Where(t => !t.IsArchived))
                .ThenInclude(t => t.Images)
                .Include(p => p.Columns)
                .ThenInclude(c => c.Tasks.Where(t => !t.IsArchived))
                .ThenInclude(t => t.AssignedToUser)
                .FirstOrDefault(p => p.Id == id && p.OrganizerId == userId);

            if (project == null)
            {
                return NotFound();
            }

            // Order columns
            project.Columns = project.Columns.OrderBy(c => c.Order).ToList();

            return View(project);
        }

        [HttpPost]
        public async Task<IActionResult> AddCard(int projectId, int columnId, string title, string labels, IFormFile? coverImage)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Validate Project ownership
            var project = _context.Projects.FirstOrDefault(p => p.Id == projectId && p.OrganizerId == userId);
            if (project == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(title))
            {
                // Find Max Order
                var maxOrder = _context.AppTasks
                    .Where(t => t.ProjectId == projectId && t.BoardColumnId == columnId)
                    .Max(t => (int?)t.Order) ?? 0;

                var task = new AppTask
                {
                    Title = title,
                    Description = "", 
                    BoardColumnId = columnId,
                    ProjectId = projectId,
                    Status = TaskStatusEnum.NotStarted,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(7),
                    Order = maxOrder + 1
                };

                // Handle Labels
                if (!string.IsNullOrWhiteSpace(labels))
                {
                    var labelNames = labels.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).Distinct();
                    foreach (var name in labelNames)
                    {
                        // Find or Create Label
                        var label = _context.BoardLabels.FirstOrDefault(l => l.ProjectId == projectId && l.Name == name);
                        if (label == null)
                        {
                            // Assign random pastel color if new
                            var colors = new[] { "#ef4444", "#3b82f6", "#10b981", "#f59e0b", "#8b5cf6", "#ec4899" };
                            var randomColor = colors[new Random().Next(colors.Length)];
                            
                            label = new BoardLabel { Name = name, ProjectId = projectId, Color = randomColor };
                            _context.BoardLabels.Add(label); // Will be saved with task
                        }
                        task.Labels.Add(label);
                    }
                }

                // Handle Cover Image
                if (coverImage != null && coverImage.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(coverImage.FileName);
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                    
                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await coverImage.CopyToAsync(stream);
                    }

                    task.Images.Add(new TaskImage { FilePath = "/uploads/" + fileName, OriginalFileName = coverImage.FileName });
                }

                _context.AppTasks.Add(task);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", new { id = projectId });
        }

        [HttpPost]
        public async Task<IActionResult> UploadTaskImage(int taskId, IFormFile file)
        {
            var task = _context.AppTasks.Include(t=>t.Project).FirstOrDefault(t => t.Id == taskId);
            if (task == null) return NotFound();

            if (file != null && file.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var image = new TaskImage { FilePath = "/uploads/" + fileName, OriginalFileName = file.FileName, AppTaskId = taskId };
                _context.TaskImages.Add(image);
                await _context.SaveChangesAsync();
                return Ok(new { filePath = image.FilePath });
            }
            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTaskDescription(int taskId, string description)
        {
            var task = _context.AppTasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return NotFound();

            task.Description = description ?? "";
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public IActionResult GetTaskDetails(int taskId)
        {
            var task = _context.AppTasks
                .Include(t => t.Labels)
                .Include(t => t.Images)
                .FirstOrDefault(t => t.Id == taskId);

            if (task == null) return NotFound();

            return Json(new
            {
                id = task.Id,
                title = task.Title,
                description = task.Description,
                labels = task.Labels.Select(l => new { l.Id, l.Name, l.Color }),
                images = task.Images.Select(i => new { i.Id, i.FilePath, i.OriginalFileName })
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddLabel(int taskId, string name)
        {
             var task = _context.AppTasks.Include(t => t.Labels).FirstOrDefault(t => t.Id == taskId);
             if(task == null) return NotFound();

             var label = _context.BoardLabels.FirstOrDefault(l => l.ProjectId == task.ProjectId && l.Name == name);
             if(label == null)
             {
                 // Better pastel palette
                 var colors = new[] { "#ffadad", "#ffd6a5", "#fdffb6", "#caffbf", "#9bf6ff", "#a0c4ff", "#bdb2ff", "#ffc6ff" };
                 var randomColor = colors[new Random().Next(colors.Length)];
                 label = new BoardLabel { Name = name, ProjectId = task.ProjectId, Color = randomColor };
                 _context.BoardLabels.Add(label);
             }

             if(!task.Labels.Any(l => l.Id == label.Id))
             {
                 task.Labels.Add(label);
                 await _context.SaveChangesAsync();
             }
             
             return Json(new { id = label.Id, name = label.Name, color = label.Color });
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
        [HttpPost]
        public async Task<IActionResult> ArchiveTask(int taskId)
        {
            var task = await _context.AppTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return NotFound();
            
            if (task.IsArchived) return Ok(); // Already archived

            task.IsArchived = true;
            task.ArchivedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RestoreTask(int taskId)
        {
            var task = await _context.AppTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return NotFound();

            if (!task.IsArchived) return Ok(); // Not archived

            task.IsArchived = false;
            task.ArchivedDate = null;
            await _context.SaveChangesAsync();
            return Ok();
        }
        
        [HttpPost]
        public async Task<IActionResult> AssignTask(int taskId, string userId)
        {
            var task = await _context.AppTasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if(task == null) return NotFound();
            
            task.AssignedToUserId = userId; 
            if(string.IsNullOrEmpty(userId) || userId == "unassigned") task.AssignedToUserId = null;
            
            await _context.SaveChangesAsync();
            return Ok();
        }
        
        [HttpGet]
        public async Task<IActionResult> GetCardHtml(int taskId)
        {
             var task = await _context.AppTasks
                .Include(t => t.Labels)
                .Include(t => t.Images)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);
                
             if(task == null) return NotFound();
             
             return PartialView("_CardPartial", task);
        }

        [HttpGet]
        public async Task<IActionResult> GetArchivedTasks(int projectId, string? query)
        {
             var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
             var project = await _context.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);
             
             if(project == null) return Unauthorized();
             // Simple auth check: Organizer or Member
             if(project.OrganizerId != userId && !project.Members.Any(m => m.UserId == userId)) return Unauthorized();
             
             var tasksQuery = _context.AppTasks
                 .Include(t => t.Labels)
                 .Include(t => t.BoardColumn)
                 .Where(t => t.ProjectId == projectId && t.IsArchived);
                 
             if(!string.IsNullOrEmpty(query))
             {
                 tasksQuery = tasksQuery.Where(t => t.Title.Contains(query) || t.Description.Contains(query));
             }
             
             var tasks = await tasksQuery.OrderByDescending(t => t.ArchivedDate).ToListAsync();
             return PartialView("_ArchiveHubPartial", tasks);
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectInsights(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OrganizerId == userId);
            
            if (project == null) return Unauthorized();
            
            var insights = await _aiService.GenerateProjectInsightsAsync(projectId);
            return Json(new { success = true, insights });
        }
    }
}
