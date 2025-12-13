using TaskManagement.Models;

namespace TaskManagement.Services
{
    public class BoardService
    {
        private Project _project;
        private int _nextTaskId = 100;
        private List<string> _columns = new List<string> { "To Do", "Doing", "Done" };

        public BoardService()
        {
            SeedData();
        }

        private void SeedData()
        {
            _project = new Project
            {
                Id = 1,
                Title = "Trello Clone Prototype",
                Description = "A vertical slice of a Kanban board.",
                OrganizerId = "mock-admin",
                Tasks = new List<AppTask>()
            };

            var sampleHelper = new[]
            {
                new { Title = "Design DB Schema", Status = TaskStatusEnum.Completed, Priority = PriorityEnum.High },
                new { Title = "Implement Auth Identity", Status = TaskStatusEnum.Completed, Priority = PriorityEnum.High },
                new { Title = "Create Board Controller", Status = TaskStatusEnum.InProgress, Priority = PriorityEnum.Medium },
                new { Title = "Fix CSS Flexbox Issues", Status = TaskStatusEnum.InProgress, Priority = PriorityEnum.Low },
                new { Title = "Deploy to Production", Status = TaskStatusEnum.NotStarted, Priority = PriorityEnum.High }
            };

            foreach (var s in sampleHelper)
            {
                // Map status to default column names
                string colName = "To Do";
                if (s.Status == TaskStatusEnum.InProgress) colName = "Doing";
                if (s.Status == TaskStatusEnum.Completed) colName = "Done";

                _project.Tasks.Add(new AppTask
                {
                    Id = _nextTaskId++,
                    Title = s.Title,
                    Description = "Sample task description...",
                    Status = s.Status,
                    Priority = s.Priority,
                    ColumnName = colName,
                    Order = _project.Tasks.Count(t => t.ColumnName == colName), // Simple increment
                    ProjectId = 1,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(7),
                    AssignedToUserId = "mock-user"
                });
            }
        }

        public Project GetBoard()
        {
            // Ensure tasks are sorted by Order when retrieved (handled in view or here if returning sorted list)
            // But Project.Tasks is a Collection. We'll sort in the View or return sorted list? 
            // View uses .Where(), so we should rely on Order property being correct.
            return _project;
        }

        public void AddTask(int columnId, string title, PriorityEnum priority)
        {
            // Validate index
            if (columnId < 0 || columnId >= _columns.Count) return;

            string targetColumn = _columns[columnId];
            
            // Still try to map to enum for backward compatibility if possible
            var status = TaskStatusEnum.NotStarted;
            if (targetColumn == "Doing") status = TaskStatusEnum.InProgress;
            if (targetColumn == "Done") status = TaskStatusEnum.Completed;

            // Calc order: end of list
            int newOrder = _project.Tasks.Count(t => t.ColumnName == targetColumn);

            var newTask = new AppTask
            {
                Id = _nextTaskId++,
                Title = title,
                Description = "", 
                Status = status,
                Priority = priority,
                ColumnName = targetColumn,
                Order = newOrder,
                ProjectId = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };

            _project.Tasks.Add(newTask);
        }

        public List<string> GetColumns()
        {
            return _columns;
        }

        public void AddColumn(string title)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                _columns.Add(title);
            }
        }

        public void MoveTask(int taskId, string targetColumnName, int newIndex)
        {
            var task = _project.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;

            string oldColumn = task.ColumnName ?? "To Do";
            
            // 1. Remove from logical position in old column (if changing columns)
            if (oldColumn != targetColumnName)
            {
                var oldColTasks = _project.Tasks.Where(t => t.ColumnName == oldColumn && t.Id != taskId).OrderBy(t => t.Order).ToList();
                for (int i = 0; i < oldColTasks.Count; i++)
                {
                    oldColTasks[i].Order = i;
                }
            }
            // If same column, we treat the removal implicitly by reordering the whole set minus the moved task, then inserting.
            
            // 2. Prepare target column tasks
            var targetColTasks = _project.Tasks
                .Where(t => t.ColumnName == targetColumnName && t.Id != taskId)
                .OrderBy(t => t.Order)
                .ToList();

            // 3. Insert task at newIndex
            // Clamp index
            if (newIndex < 0) newIndex = 0;
            if (newIndex > targetColTasks.Count) newIndex = targetColTasks.Count;

            targetColTasks.Insert(newIndex, task);

            // 4. Update task properties and re-assign Orders
            task.ColumnName = targetColumnName;
            // Update Status enum if applicable for backward compat
            if (targetColumnName == "To Do") task.Status = TaskStatusEnum.NotStarted;
            else if (targetColumnName == "Doing") task.Status = TaskStatusEnum.InProgress;
            else if (targetColumnName == "Done") task.Status = TaskStatusEnum.Completed;

            for (int i = 0; i < targetColTasks.Count; i++)
            {
                targetColTasks[i].Order = i;
            }
        }

        public void ToggleTaskCompletion(int taskId)
        {
            var task = _project.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.IsCompleted = !task.IsCompleted;
            }
        }
    }
}
