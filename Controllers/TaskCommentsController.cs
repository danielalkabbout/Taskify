using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagementSystem.Models;
using TaskManagementSystem.Models.DTOs;

namespace TaskManagementSystem.Controllers
{
    [Authorize]
    [Route("api/tasks/{taskId}/comments")]
    [ApiController]
    public class TaskCommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TaskCommentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<CommentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTaskComments(int taskId)
        {
            try
            {
             
                var taskExists = await _context.Tasks
                    .Where(t => !t.IsDeleted)
                    .AnyAsync(t => t.Id == taskId);

                if (!taskExists)
                {
                    return NotFound(new { Message = "Task not found or has been deleted" });
                }

                var comments = await _context.Comments
                    .Where(c => c.TaskId == taskId)
                    .Include(c => c.User)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CommentDto
                    {
                        Id = c.Id,
                        Text = c.Text,
                        CreatedAt = c.CreatedAt,
                        Author = $"{c.User.FirstName} {c.User.LastName}",
                        AuthorId = c.UserId
                    })
                    .ToListAsync();

                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving comments", Error = ex.Message });
            }
        }
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddComment(int taskId, [FromBody] AddCommentDto addCommentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var taskExists = await _context.Tasks
                    .Where(t => !t.IsDeleted)
                    .AnyAsync(t => t.Id == taskId);

                if (!taskExists)
                    return NotFound(new { Message = "Task not found or has been deleted" });

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var comment = new Comment
                {
                    Text = addCommentDto.Text,
                    TaskId = taskId,
                    UserId = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Comments.AddAsync(comment);
                await _context.SaveChangesAsync();

                var createdComment = await _context.Comments
                    .Include(c => c.User)
                    .Where(c => c.Id == comment.Id)
                    .Select(c => new CommentDto
                    {
                        Id = c.Id,
                        Text = c.Text,
                        CreatedAt = c.CreatedAt,
                        Author = $"{c.User.FirstName} {c.User.LastName}",
                        AuthorId = c.UserId
                    })
                    .FirstOrDefaultAsync();

             
                _context.TaskHistories.Add(new TaskHistory
                {
                    TaskId = taskId,
                    ChangedByUserId = currentUserId,
                    ChangeDescription = $"Added comment: {addCommentDto.Text.Truncate(50)}...",
                    ChangedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetTaskComments),
                    new { taskId = taskId },
                    createdComment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while adding the comment",
                    Error = ex.Message
                });
            }
        }
    }
}