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
            var board = _boardService.GetBoard();
            ViewBag.Columns = _boardService.GetColumns();
            return View(board);
        }

        [HttpPost]
        public IActionResult AddCard(int columnId, string title, PriorityEnum priority)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                _boardService.AddTask(columnId, title, priority);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult AddColumn(string title)
        {
            _boardService.AddColumn(title);
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
