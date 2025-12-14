using Microsoft.AspNetCore.Mvc;
using TaskManagement.Models;
using TaskManagement.Services;

namespace TaskManagement.Controllers
{
    public class BoardController : Controller
    {
        private readonly BoardService _boardService;

        public BoardController(BoardService boardService)
        {
            _boardService = boardService;
        }

        public IActionResult Index()
        {
            // Hardcoded ID 1 for now, as per instruction to fetch "a Project".
            // In real app, this would come from route or user context.
            // Ensuring we handle null if DB is empty or ID 1 doesn't exist.
            var board = _boardService.GetBoard(1);
            
            if (board == null)
            {
               // Graceful empty state
               return View(null);
               // Instruction: "pass the empty model or null to the view to be handled gracefully."
               // I will pass null.
               return View(null);
            }
            
            return View(board);
        }

        [HttpPost]
        public IActionResult AddCard(string columnName, string title, PriorityEnum priority)
        {
            // Note: View was sending 'columnId' as index.
            // I need to update View to send 'columnName'
            if (!string.IsNullOrWhiteSpace(title))
            {
                _boardService.AddTaskToColumn(columnName, title, priority, 1);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult AddColumn(string title)
        {
            _boardService.AddColumn(1, title);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult MoveCard(int taskId, string targetColumn, int index)
        {
            _boardService.MoveTask(taskId, targetColumn, index);
            return Ok();
        }

        [HttpPost]
        public IActionResult ToggleCard(int taskId)
        {
            _boardService.ToggleTaskCompletion(taskId);
            return Ok();
        }
    }
}
