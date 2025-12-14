using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Services
{
    public class BoardService
    {
        private readonly ApplicationDbContext _context;

        public BoardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Project GetBoard(int projectId)
        {
            // 1. Fetch Project with Columns and Tasks eagerly
            var project = _context.Projects
                .Include(p => p.Columns)
                .Include(p => p.Tasks)
                .FirstOrDefault(p => p.Id == projectId);

            // 2. Auto-Seed if ANY project is missing (Prototype Convenience)
            if (project == null)
            {
                // Ensure we have a user for the OrganizerId Foreign Key
                var user = _context.Users.FirstOrDefault();
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = "admin@demo.com",
                        Email = "admin@demo.com",
                        NormalizedUserName = "ADMIN@DEMO.COM",
                        NormalizedEmail = "ADMIN@DEMO.COM",
                        EmailConfirmed = true
                    };
                    _context.Users.Add(user);
                    _context.SaveChanges();
                }

                // Create Default Project
                project = new Project
                {
                    Title = "Trello Clone Prototype",
                    Description = "A vertical slice of a Kanban board.",
                    OrganizerId = user.Id,
                    CreatedDate = DateTime.Now
                };
                
                _context.Projects.Add(project);
                _context.SaveChanges(); // Save to get Id

                // Reload to attach
                project = _context.Projects
                    .Include(p => p.Columns)
                    .Include(p => p.Tasks)
                    .FirstOrDefault(p => p.Id == project.Id);
            }

            // 3. Ensure Default Columns Exist (Seeding Logic)
            if (project != null && !project.Columns.Any())
            {
                var defaultColumns = new List<ProjectColumn>
                {
                    new ProjectColumn { Name = "To Do", Order = 0, ProjectId = project.Id },
                    new ProjectColumn { Name = "Doing", Order = 1, ProjectId = project.Id },
                    new ProjectColumn { Name = "Done", Order = 2, ProjectId = project.Id }
                };

                _context.ProjectColumns.AddRange(defaultColumns);
                _context.SaveChanges(); // Synchronous Save

                // Reload columns
                _context.Entry(project).Collection(p => p.Columns).Load();
            }

            return project;
        }

        public void AddTask(int columnId, string title, PriorityEnum priority)
        {
            // Note: columnId here might be index or ID.
            // In the view, we passed index. Refactoring to pass Name or ID would be better.
            // But to keep constraints: "UX looking like before".
            // Previous code: GetColumns()[columnId] -> string targetColumn.
            
            // Let's resolve the column name by index from the project's columns, 
            // OR change controller to pass something more robust.
            // Given I can change Controller, I will make Controller pass Column Name.
            // BUT, let's look at the method signature: (int columnId, ...).
            // This 'columnId' in the old code was an INDEX.
            // I should interpret it as ID or INDEX?
            // Safer to refactor Controller to pass the NAME or ID of the column directly.
            // I'll stick to string Name in this method to match AppTask.ColumnName logic.
            // Wait, I am rewriting BoardService. So I can change signature.
        }
        
        // Revised AddTask accepting string for strict Column Mapping
        public void AddTaskToColumn(string columnName, string title, PriorityEnum priority, int projectId)
        {
             var project = _context.Projects.Include(p => p.Tasks).FirstOrDefault(p => p.Id == projectId);
             if (project == null) return;

             var status = TaskStatusEnum.NotStarted;
             if (columnName == "Doing") status = TaskStatusEnum.InProgress;
             if (columnName == "Done") status = TaskStatusEnum.Completed;
             
             // Calculate Order
             var existingTasksInCol = project.Tasks.Count(t => t.ColumnName == columnName);

             var newTask = new AppTask
             {
                 Title = title,
                 Description = "",
                 Status = status,
                 Priority = priority,
                 ColumnName = columnName,
                 Order = existingTasksInCol,
                 ProjectId = projectId,
                 StartDate = DateTime.Now,
                 EndDate = DateTime.Now.AddDays(1),
                 AssignedToUserId = project.OrganizerId // Or null
             };
             
             // Check if CreatedDate/Organizer is needed?
             // AppTask doesn't have Organizer, Project does.
             // AssignedToUserId: let's leave null or set to current user if we had context.
             // Since I don't have user context here easily without passing it, I'll leave null.
             
             _context.AppTasks.Add(newTask);
             _context.SaveChanges();
        }

        public void AddColumn(int projectId, string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            
            var project = _context.Projects.Include(p => p.Columns).FirstOrDefault(p => p.Id == projectId);
            if (project == null) return;

            // Start Order at end
            int maxOrder = project.Columns.Any() ? project.Columns.Max(c => c.Order) : -1;
            
            var newCol = new ProjectColumn
            {
                Name = title,
                Order = maxOrder + 1,
                ProjectId = projectId
            };
            
            _context.ProjectColumns.Add(newCol);
            _context.SaveChanges();
        }

        public void MoveTask(int taskId, string targetColumnName, int newIndex)
        {
            var task = _context.AppTasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;

            // Update State
            task.ColumnName = targetColumnName;
            
            // Map Status if standard columns (UX consistency)
            if (targetColumnName == "To Do") task.Status = TaskStatusEnum.NotStarted;
            else if (targetColumnName == "Doing") task.Status = TaskStatusEnum.InProgress;
            else if (targetColumnName == "Done") task.Status = TaskStatusEnum.Completed;
            
            // Note: Reordering logic is complex to do purely in DB without loading all tasks.
            // For now, I will blindly update the order. 
            // In a real Trello, you shift others.
            // Constraint: "Strict Synchronous Code... Real Database Connection".
            // I'll implement basic reordering synchronously.
            
            // 1. Fetch all tasks in target column to shift them
            // We need to shift everything >= newIndex down.
            // And normalization.
            // This is heavy, but fits constraints.
            
            var targetTasks = _context.AppTasks
                .Where(t => t.ProjectId == task.ProjectId && t.ColumnName == targetColumnName && t.Id != taskId)
                .OrderBy(t => t.Order)
                .ToList();

            if (newIndex < 0) newIndex = 0;
            if (newIndex > targetTasks.Count) newIndex = targetTasks.Count;

            targetTasks.Insert(newIndex, task);

            for (int i = 0; i < targetTasks.Count; i++)
            {
                targetTasks[i].Order = i;
            }
            
            _context.SaveChanges();
        }

        public void ToggleTaskCompletion(int taskId)
        {
            var task = _context.AppTasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.IsCompleted = !task.IsCompleted;
                _context.SaveChanges();
            }
        }
    }
}
