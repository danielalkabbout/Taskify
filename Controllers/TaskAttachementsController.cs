using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagementSystem.Models;
using TaskManagementSystem.Models.DTOs;

namespace TaskManagementSystem.Controllers
{
    [Authorize]
    [Route("api/tasks/{taskId}/attachments")]
    [ApiController]
    public class TaskAttachmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public TaskAttachmentsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        [HttpGet]
        [ProducesResponseType(typeof(List<AttachmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTaskAttachments(int taskId)
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

                var attachments = await _context.Attachments
                    .Where(a => a.TaskId == taskId)
                    .Include(a => a.User)
                    .OrderByDescending(a => a.UploadedAt)
                    .ToListAsync();

      
                var attachmentDtos = attachments.Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = GetRelativeFilePath(a.FilePath), 
                    UploadedAt = a.UploadedAt,
                    UploadedBy = $"{a.User?.FirstName} {a.User?.LastName}",
                    FileSize = GetFileSize(a.FilePath)
                })
                    .ToList();

                return Ok(attachmentDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving attachments",
                    Error = ex.Message
                });
            }
        }
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> UploadAttachment(
       int taskId,
       [FromForm] UploadAttachmentDto uploadDto)
        {
            try
            {
                var task = await _context.Tasks
                    .Where(t => !t.IsDeleted)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                    return NotFound(new { Message = "Task not found or has been deleted" });

         
                if (uploadDto?.File == null || uploadDto.File.Length == 0)
                    return BadRequest(new { Message = "No file provided" });

                
                if (uploadDto.File.Length > 20 * 1024 * 1024)
                    return BadRequest(new { Message = "File size exceeds 20MB limit" });

         
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(currentUserId))
                    return Unauthorized(new { Message = "User not authenticated properly" });

             
                var fileName = Path.GetFileName(uploadDto.File.FileName);
                if (string.IsNullOrEmpty(fileName))
                    return BadRequest(new { Message = "Invalid file name" });

                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

                var uploadBasePath = Path.Combine(_environment.ContentRootPath, "wwwroot");
                var uploadsDir = Path.Combine("uploads", "tasks", taskId.ToString());
                var fullUploadPath = Path.Combine(uploadBasePath, uploadsDir);

            
                Directory.CreateDirectory(fullUploadPath);

                
                var filePath = Path.Combine(fullUploadPath, uniqueFileName);
                var relativePath = Path.Combine(uploadsDir, uniqueFileName).Replace("\\", "/");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadDto.File.CopyToAsync(stream);
                }

        
                var attachment = new Attachment
                {
                    FileName = fileName,
                    FilePath = relativePath,
                    TaskId = taskId,
                    UserId = currentUserId,
                    UploadedAt = DateTime.UtcNow
                };

                await _context.Attachments.AddAsync(attachment);
                await _context.SaveChangesAsync();

               
                _context.TaskHistories.Add(new TaskHistory
                {
                    TaskId = taskId,
                    ChangedByUserId = currentUserId,
                    ChangeDescription = $"Added attachment: {fileName}",
                    ChangedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(currentUserId);

                return CreatedAtAction(
                    nameof(GetTaskAttachments),
                    new { taskId = taskId },
                    new AttachmentDto
                    {
                        Id = attachment.Id,
                        FileName = attachment.FileName,
                        FilePath = GetRelativeFilePath(attachment.FilePath),
                        UploadedAt = attachment.UploadedAt,
                        UploadedBy = $"{user?.FirstName} {user?.LastName}",
                        FileSize = uploadDto.File.Length
                    });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while uploading the attachment",
                    Error = ex.Message 
                });
            }
        }

   
        private static string GetRelativeFilePath(string storedPath)
        {
            return storedPath.Replace("\\", "/").TrimStart('/');
        }

        private long GetFileSize(string storedPath)
        {
            var physicalPath = Path.Combine(_environment.WebRootPath, storedPath);
            return new FileInfo(physicalPath).Length;
        }
        private string GetPhysicalPath(string storedPath)
        {
            return Path.Combine(_environment.WebRootPath, storedPath);
        }

   
    }
}