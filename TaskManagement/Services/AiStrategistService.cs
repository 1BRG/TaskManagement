using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Services
{
    public class AiStrategistService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        public AiStrategistService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        }

        public async Task<string> GenerateProjectInsightsAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Columns)
                .ThenInclude(c => c.Tasks)
                .ThenInclude(t => t.Labels)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return "Project not found.";

            var boardData = AggregateBoardData(project);
            var prompt = CraftPrompt(project.Title, boardData);

            return await CallGeminiApiAsync(prompt);
        }

        private string AggregateBoardData(Project project)
        {
            var sb = new StringBuilder();

            foreach (var column in project.Columns.OrderBy(c => c.Order))
            {
                sb.AppendLine($"## Column: {column.Title}");
                
                var activeTasks = column.Tasks.Where(t => !t.IsArchived).ToList();
                var archivedTasks = column.Tasks.Where(t => t.IsArchived).ToList();

                sb.AppendLine($"Active Tasks ({activeTasks.Count}):");
                foreach (var task in activeTasks.OrderBy(t => t.Order))
                {
                    sb.AppendLine($"  - {task.Title}");
                    if (!string.IsNullOrEmpty(task.Description))
                        sb.AppendLine($"    Description: {task.Description}");
                    if (task.Labels?.Any() == true)
                        sb.AppendLine($"    Labels: {string.Join(", ", task.Labels.Select(l => l.Name))}");
                    sb.AppendLine($"    Status: {(task.IsCompleted ? "Completed" : "In Progress")}");
                }

                if (archivedTasks.Any())
                {
                    sb.AppendLine($"Archived Tasks ({archivedTasks.Count}):");
                    foreach (var task in archivedTasks.OrderByDescending(t => t.ArchivedDate))
                    {
                        sb.AppendLine($"  - {task.Title} (archived {task.ArchivedDate?.ToString("MMM dd")})");
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string CraftPrompt(string projectName, string boardData)
        {
            return $@"You are an AI Project Strategist analyzing a Kanban board for ""{projectName}"".

Based on the following board data, provide a concise executive summary that includes:
1. **Project Overview**: A 2-3 sentence summary of the project's current state.
2. **Progress Analysis**: What has been accomplished (completed tasks, archived items).
3. **Current Focus**: What the team is actively working on.
4. **Potential Bottlenecks**: Any columns with too many tasks or tasks that seem stalled.
5. **Strategic Recommendations**: 2-3 actionable next steps based on the data.

Keep the response concise and actionable. Use markdown formatting.

---

## Board Data:

{boardData}

---

Provide your analysis:";
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "**Error:** API key not configured. Please set the GEMINI_API_KEY environment variable.";
            }

            try
            {
                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 2048
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";
                
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"**API Error ({response.StatusCode}):** Unable to generate insights. Please try again later.";
                }

                using var doc = JsonDocument.Parse(responseBody);
                var textContent = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return textContent ?? "No insights generated.";
            }
            catch (Exception ex)
            {
                return $"**Error:** {ex.Message}";
            }
        }
    }
}
