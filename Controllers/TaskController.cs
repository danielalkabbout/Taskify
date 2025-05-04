using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Helpers;
using System.Security.Claims;
using System.Text;
using TaskManagementSystem.Models;
using TaskManagementSystem.Models.DTOs;
using QuestPDF.Fluent; 
using QuestPDF.Infrastructure;
using ClosedXML.Excel;

namespace TaskManagementSystem.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTasks(
            [FromQuery] string? status = null,
            [FromQuery] string? category = null,
            [FromQuery] string? priority = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "DueDate",
            [FromQuery] bool sortDescending = false)
        {
            try
            {
                var query = _context.Tasks
                    .Where(t=>!t.IsDeleted)
                    .Include(t => t.Category)
                    .Include(t => t.CreatedByUser)
                    .AsQueryable();

       
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<Models.TaskStatus>(status, ignoreCase: true, out var statusEnum))
                    {
                        query = query.Where(t => t.Status == statusEnum);
                    }
                    else
                    {
                        return BadRequest(new { Message = $"Invalid status value. Valid values are: {string.Join(", ", Enum.GetNames(typeof(Models.TaskStatus)))}" });
                    }
                }
                  
                
                    if (!string.IsNullOrEmpty(category))
                    query = query.Where(t => t.Category.Name == category);

                if (!string.IsNullOrEmpty(priority))
                {
                    if (Enum.TryParse<TaskPriority>(priority, true, out var priorityEnum))
                    {
                        query = query.Where(t => t.Priority == priorityEnum);
                    }
                    else
                    {
                        return BadRequest(new { Message = $"Invalid priority value. Valid values are: {string.Join(", ", Enum.GetNames(typeof(TaskPriority)))}" });
                    }
                }

       
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(t =>
                        t.Title.Contains(search) ||
                        t.Description.Contains(search));
                }

           
                query = sortBy.ToLower() switch
                {
                    "title" => sortDescending
                        ? query.OrderByDescending(t => t.Title)
                        : query.OrderBy(t => t.Title),
                    "duedate" => sortDescending
                        ? query.OrderByDescending(t => t.DueDate)
                        : query.OrderBy(t => t.DueDate),
                    "priority" => sortDescending
                        ? query.OrderByDescending(t => t.Priority)
                        : query.OrderBy(t => t.Priority),
                    "status" => sortDescending
                        ? query.OrderByDescending(t => t.Status)
                        : query.OrderBy(t => t.Status),
                    _ => sortDescending
                        ? query.OrderByDescending(t => t.DueDate)
                        : query.OrderBy(t => t.DueDate)
                };

                var tasks = await query
                    .Select(t => new TaskDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        DueDate = t.DueDate,
                        Status = t.Status.ToString(),
                        Priority = t.Priority.ToString(),
                        CategoryName = t.Category.Name,
                        CreatedBy = $"{t.CreatedByUser.FirstName} {t.CreatedByUser.LastName}",
                        ReminderDate = t.ReminderDate,
                        Recurrence = t.Recurrence != null ? t.Recurrence.ToString() : null
                    })
                    .ToListAsync();

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTaskById(int id)
        {
            try
            {
                var task = await _context.Tasks.Where(t => !t.IsDeleted)
                    .Include(t => t.Category)
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.User)
                    .Include(t => t.Attachments)
                    .Include(t => t.UserTasks)
                        .ThenInclude(ut => ut.User)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return NotFound(new { Message = "Task not found" });

                var taskDetail = new TaskDetailDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    DueDate = task.DueDate,
                    Status = task.Status.ToString(),
                    Priority = task.Priority.ToString(),
                    CategoryName = task.Category?.Name,
                    CreatedBy = $"{task.CreatedByUser?.FirstName} {task.CreatedByUser?.LastName}",
          
                    ReminderDate = task.ReminderDate,
                    Recurrence = task.Recurrence?.ToString(),
                    AssignedUsers = task.UserTasks.Select(ut => new AssignedUserDto
                    {
                        UserId = ut.UserId,
                        FullName = $"{ut.User.FirstName} {ut.User.LastName}",
                        Email = ut.User.Email
                    }).ToList(),
                    Comments = task.Comments.Select(c => new CommentDto
                    {
                        Id = c.Id,
                        Text = c.Text,
                        CreatedAt = c.CreatedAt,
                        Author = $"{c.User?.FirstName} {c.User?.LastName}"
                    }).ToList(),
                    Attachments = task.Attachments.Select(a => new AttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        UploadedAt = a.UploadedAt
                    }).ToList()
                };

                return Ok(taskDetail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                
                var task = new Models.Task
                {
                    Title = createTaskDto.Title,
                    Description = createTaskDto.Description,
                    DueDate = createTaskDto.DueDate,
                    Status = (Models.TaskStatus)Enum.Parse(typeof(Models.TaskStatus), createTaskDto.Status),
                    Priority = (TaskPriority)Enum.Parse(typeof(TaskPriority), createTaskDto.Priority),
                    CategoryId = createTaskDto.CategoryId,
                    CreatedByUserId = currentUserId,
                    ReminderDate = createTaskDto.ReminderDate,
                    Recurrence = createTaskDto.Recurrence != null ?
                        (TaskRecurrence)Enum.Parse(typeof(TaskRecurrence), createTaskDto.Recurrence) : null,
                  
                };

                await _context.Tasks.AddAsync(task);
                await _context.SaveChangesAsync();

               
                if (createTaskDto.AssignedUserIds != null && createTaskDto.AssignedUserIds.Any())
                {
                    foreach (var userId in createTaskDto.AssignedUserIds)
                    {
                        if (await _context.Users.AnyAsync(u => u.Id == userId))
                        {
                            _context.UserTasks.Add(new UserTask
                            {
                                TaskId = task.Id,
                                UserId = userId
                            });

                      
                            _context.TaskHistories.Add(new TaskHistory
                            {
                                TaskId = task.Id,
                                ChangedByUserId = currentUserId,
                                ChangeDescription = $"Task assigned to user {userId}",
                                ChangedAt = DateTime.UtcNow
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                _context.TaskHistories.Add(new TaskHistory
                {
                    TaskId = task.Id,
                    ChangedByUserId = currentUserId,
                    ChangeDescription = $"Task created with status {task.Status}",
                    ChangedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                var createdTask = await GetTaskWithDetails(task.Id);
                return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, createdTask);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
            }
        }

        private async Task<TaskDetailDto> GetTaskWithDetails(int taskId)
        {
            var task = await _context.Tasks
                .Where(t=>!t.IsDeleted)
                .Include(t => t.Category)
                .Include(t => t.CreatedByUser)
                .Include(t => t.UserTasks)
                    .ThenInclude(ut => ut.User)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            return new TaskDetailDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                CategoryName = task.Category?.Name,
                CreatedBy = $"{task.CreatedByUser?.FirstName} {task.CreatedByUser?.LastName}",
           
                ReminderDate = task.ReminderDate,
                Recurrence = task.Recurrence?.ToString(),
                AssignedUsers = task.UserTasks.Select(ut => new AssignedUserDto
                {
                    UserId = ut.UserId,
                    FullName = $"{ut.User.FirstName} {ut.User.LastName}",
                    Email = ut.User.Email
                }).ToList(),
                Comments = task.Comments.Select(c => new CommentDto
                {
                    Id = c.Id,
                    Text = c.Text,
                    CreatedAt = c.CreatedAt,
                    Author = $"{c.User?.FirstName} {c.User?.LastName}"
                }).ToList(),
                Attachments = task.Attachments.Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    UploadedAt = a.UploadedAt
                }).ToList()
            };
        }
    
    [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto updateTaskDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                
                var task = await _context.Tasks.Where(t => !t.IsDeleted)
                    .Include(t => t.Category)
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.UserTasks)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return NotFound(new { Message = "Task not found" });

            
                if (task.CreatedByUserId != currentUserId && !isAdmin)
                    return Forbid();

                var changes = new List<string>();

            
                if (updateTaskDto.Title != null && task.Title != updateTaskDto.Title)
                {
                    changes.Add($"Title changed from '{task.Title}' to '{updateTaskDto.Title}'");
                    task.Title = updateTaskDto.Title;
                }

                if (updateTaskDto.Description != null && task.Description != updateTaskDto.Description)
                {
                    changes.Add($"Description updated");
                    task.Description = updateTaskDto.Description;
                }

                if (updateTaskDto.DueDate.HasValue && task.DueDate != updateTaskDto.DueDate.Value)
                {
                    changes.Add($"Due date changed from {task.DueDate} to {updateTaskDto.DueDate}");
                    task.DueDate = updateTaskDto.DueDate.Value;
                }

                if (updateTaskDto.Status != null && task.Status.ToString() != updateTaskDto.Status)
                {
                    changes.Add($"Status changed from {task.Status} to {updateTaskDto.Status}");
                    task.Status = Enum.Parse<Models.TaskStatus>(updateTaskDto.Status);
                }

                if (updateTaskDto.Priority != null && task.Priority.ToString() != updateTaskDto.Priority)
                {
                    changes.Add($"Priority changed from {task.Priority} to {updateTaskDto.Priority}");
                    task.Priority = Enum.Parse<TaskPriority>(updateTaskDto.Priority);
                }

                if (updateTaskDto.CategoryId.HasValue && task.CategoryId != updateTaskDto.CategoryId.Value)
                {
                    var oldCategory = task.Category?.Name;
                    var newCategory = await _context.Categories.FindAsync(updateTaskDto.CategoryId);
                    changes.Add($"Category changed from '{oldCategory}' to '{newCategory?.Name}'");
                    task.CategoryId = updateTaskDto.CategoryId.Value;
                }

                if (updateTaskDto.ReminderDate != task.ReminderDate)
                {
                    changes.Add($"Reminder date changed from {task.ReminderDate} to {updateTaskDto.ReminderDate}");
                    task.ReminderDate = updateTaskDto.ReminderDate;
                }

                if (updateTaskDto.Recurrence != task.Recurrence?.ToString())
                {
                    changes.Add($"Recurrence changed from {task.Recurrence} to {updateTaskDto.Recurrence}");
                    task.Recurrence = updateTaskDto.Recurrence != null
                        ? Enum.Parse<TaskRecurrence>(updateTaskDto.Recurrence)
                        : null;
                }

            
                if (updateTaskDto.AssignedUserIds != null)
                {
                    var currentAssignments = task.UserTasks.Select(ut => ut.UserId).ToList();
                    var newAssignments = updateTaskDto.AssignedUserIds;
        
                    var usersToRemove = currentAssignments.Except(newAssignments).ToList();
                    foreach (var userId in usersToRemove)
                    {
                        var assignment = task.UserTasks.FirstOrDefault(ut => ut.UserId == userId);
                        if (assignment != null)
                        {
                            _context.UserTasks.Remove(assignment);
                            changes.Add($"Removed assignment for user {userId}");
                        }
                    }

 
                    var usersToAdd = newAssignments.Except(currentAssignments).ToList();
                    foreach (var userId in usersToAdd)
                    {
                        if (await _context.Users.AnyAsync(u => u.Id == userId))
                        {
                            _context.UserTasks.Add(new UserTask
                            {
                                TaskId = task.Id,
                                UserId = userId
                            });
                            changes.Add($"Added assignment for user {userId}");
                        }
                    }
                }

     
                await _context.SaveChangesAsync();

              
                if (changes.Any())
                {
                    foreach (var change in changes)
                    {
                        _context.TaskHistories.Add(new TaskHistory
                        {
                            TaskId = task.Id,
                            ChangedByUserId = currentUserId,
                            ChangeDescription = change,
                            ChangedAt = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                var updatedTask = await GetTaskWithDetails(task.Id);
                return Ok(updatedTask);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
            }
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                    return NotFound();

                task.IsDeleted = true;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error deleting task", Error = ex.Message });
            }
        }
        [Authorize(Roles = "Admin")] 
        [HttpPatch("{id}/restore")]
        [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RestoreTask(int id)
        {
            try
            {
                
                var task = await _context.Tasks
                    .IgnoreQueryFilters() 
                    .Include(t => t.Category)
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.UserTasks)
                        .ThenInclude(ut => ut.User)
                    .Include(t => t.Comments)
                    .Include(t => t.Attachments)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                {
                    return NotFound(new { Message = "Task not found" });
                }

                if (!task.IsDeleted)
                {
                    return BadRequest(new { Message = "Task is not deleted" });
                }

              
                task.IsDeleted = false;

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _context.TaskHistories.Add(new TaskHistory
                {
                    TaskId = task.Id,
                    ChangedByUserId = currentUserId,
                    ChangeDescription = $"Task restored by user {currentUserId}",
                    ChangedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                
                var restoredTask = await GetTaskWithDetails(task.Id);
                return Ok(restoredTask);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while restoring the task", Error = ex.Message });
            }
        }
        [HttpGet("{taskId}/history")]
        [ProducesResponseType(typeof(List<TaskHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTaskHistory(int taskId)
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

               
                var history = await _context.TaskHistories
                    .Where(th => th.TaskId == taskId)
                    .Include(th => th.ChangedByUser)
                    .OrderByDescending(th => th.ChangedAt)
                    .Select(th => new TaskHistoryDto
                    {
                        Id = th.Id,
                        ChangeDescription = th.ChangeDescription,
                        ChangedAt = th.ChangedAt,
                        ChangedBy = $"{th.ChangedByUser.FirstName} {th.ChangedByUser.LastName}",
                        ChangedByUserId = th.ChangedByUserId
                    })
                    .ToListAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving task history",
                    Error = ex.Message
                });
            }
        }
        [HttpGet("my/history/filtered")]
        [ProducesResponseType(typeof(List<UserTaskHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyFilteredTaskHistory(
            [FromQuery] int? taskId = null,
            [FromQuery] string? changeType = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? taskStatus = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { Message = "User not authenticated properly" });
                }
         var query = _context.TaskHistories
                    .Where(th => th.ChangedByUserId == currentUserId)
                    .Include(th => th.Task)
                    .AsQueryable();

       
                if (taskId.HasValue)
                {
                    query = query.Where(th => th.TaskId == taskId.Value);
                }

                if (!string.IsNullOrEmpty(changeType))
                {
                    query = query.Where(th => th.ChangeDescription.Contains(changeType));
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(th => th.ChangedAt >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(th => th.ChangedAt <= toDate.Value);
                }

                if (!string.IsNullOrEmpty(taskStatus))
                {
               
                    if (Enum.TryParse<Models.TaskStatus>(taskStatus, true, out var statusEnum))
                    {
                        query = query.Where(th => th.Task.Status == statusEnum);
                    }
                    else
                    {
                        return BadRequest(new { Message = "Invalid task status value" });
                    }
                }

               
                var totalCount = await query.CountAsync();

      
                var history = await query
                    .OrderByDescending(th => th.ChangedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(th => new UserTaskHistoryDto
                    {
                        Id = th.Id,
                        ChangeDescription = th.ChangeDescription,
                        ChangedAt = th.ChangedAt,
                        TaskId = th.TaskId,
                        TaskTitle = th.Task.Title,
                        TaskStatus = th.Task.Status.ToString(),
                        ChangeType = GetChangeType(th.ChangeDescription)
                    })
                    .ToListAsync();

                
                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while retrieving your task history",
                    Error = ex.Message
                });
            }
        }
        [HttpGet("export")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportTasks(
          [FromQuery] string format = "csv",
          [FromQuery] string? status = null,
          [FromQuery] string? category = null,
          [FromQuery] DateTime? fromDate = null,
          [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.Tasks
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.Category)
                    .Include(t => t.CreatedByUser)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<Models.TaskStatus>(status, true, out var statusEnum))
                    {
                        query = query.Where(t => t.Status == statusEnum);
                    }
                    else
                    {
                        return BadRequest("Invalid status value");
                    }
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(t => t.Category.Name == category);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(t => t.DueDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(t => t.DueDate <= toDate.Value);
                }

                var tasks = await query
                    .Select(t => new TaskDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        DueDate = t.DueDate,
                        Status = t.Status.ToString(),
                        Priority = t.Priority.ToString(),
                        CategoryName = t.Category.Name,
                        CreatedBy = $"{t.CreatedByUser.FirstName} {t.CreatedByUser.LastName}",
                        ReminderDate = t.ReminderDate
                    })
                    .ToListAsync();

    
                byte[] fileContents;
                string contentType;
                string fileExtension;

                switch (format.ToLower())
                {
                    case "csv":
                      
                        (fileContents, contentType, fileExtension) = GenerateCsvExport(tasks, _context);
                       
                        break;
                    case "excel":
                        (fileContents, contentType, fileExtension) = GenerateExcelExport(tasks,_context);
                        break;
                    case "pdf":
                        (fileContents, contentType, fileExtension) = GeneratePdfExport(tasks, _context);
                        break;
                    default:
                        return BadRequest("Invalid format. Supported formats: csv, excel, pdf");
                }

              
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                return File(fileContents, contentType, $"tasks_export_{timestamp}.{fileExtension}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while exporting tasks",
                    Error = ex.Message
                });
            }
        }

        private (byte[], string, string) GenerateCsvExport(List<TaskDto> tasks, ApplicationDbContext context)
        {
            var csv = new StringBuilder();

  
            csv.AppendLine("ID,Title,Description,DueDate,Status,Priority,Category,CreatedBy,AssignedUsers,Comments,Attachments");

            foreach (var task in tasks)
            {
              
                var fullTask = context.Tasks
                    .Include(t => t.UserTasks).ThenInclude(ut => ut.User)
                    .Include(t => t.Comments).ThenInclude(c => c.User)
                    .Include(t => t.Attachments)
                    .FirstOrDefault(t => t.Id == task.Id);

             
                var assignedUsers = fullTask?.UserTasks.Any() == true
                    ? string.Join("; ", fullTask.UserTasks.Select(ut => $"{ut.User.FirstName} {ut.User.LastName} ({ut.User.Email})"))
                    : "None";

              
                var comments = fullTask?.Comments.Any() == true
                    ? string.Join(" | ", fullTask.Comments.Select(c =>
                        $"\"{c.Text.Replace("\"", "'\"'")}\" ({c.User.FirstName} {c.User.LastName} on {c.CreatedAt:yyyy-MM-dd})"))
                    : "None";

               
                var attachments = fullTask?.Attachments.Any() == true
                    ? string.Join(" | ", fullTask.Attachments.Select(a =>
                        $"{a.FileName} (uploaded by {a.User.FirstName} {a.User.LastName} on {a.UploadedAt:yyyy-MM-dd})"))
                    : "None";

               
                csv.AppendLine(
                    $"{task.Id}," +
                    $"\"{EscapeCsv(task.Title)}\"," +
                    $"\"{EscapeCsv(task.Description)}\"," +
                    $"{task.DueDate:yyyy-MM-dd}," +
                    $"{task.Status}," +
                    $"{task.Priority}," +
                    $"\"{EscapeCsv(task.CategoryName)}\"," +
                    $"\"{EscapeCsv(task.CreatedBy)}\"," +
                    $"\"{EscapeCsv(assignedUsers)}\"," +
                    $"\"{EscapeCsv(comments)}\"," +
                    $"\"{EscapeCsv(attachments)}\""
                );
            }

            return (Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "csv");
        }

        private string EscapeCsv(string field)
        {
            return field?.Replace("\"", "\"\"") ?? "";
        }

     
        private (byte[], string, string) GenerateExcelExport(List<TaskDto> tasks, ApplicationDbContext context)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Tasks");

            
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Title";
            worksheet.Cell(1, 3).Value = "Description";
            worksheet.Cell(1, 4).Value = "Due Date";
            worksheet.Cell(1, 5).Value = "Status";
            worksheet.Cell(1, 6).Value = "Priority";
            worksheet.Cell(1, 7).Value = "Category";
            worksheet.Cell(1, 8).Value = "Created By";
            worksheet.Cell(1, 9).Value = "Assigned Users";
            worksheet.Cell(1, 10).Value = "Comments";
            worksheet.Cell(1, 11).Value = "Attachments";

            worksheet.Range("A1:K1").Style.Font.Bold = true;
            worksheet.Range("A1:K1").Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var task in tasks)
            {
                var fullTask = context.Tasks
                    .Include(t => t.UserTasks).ThenInclude(ut => ut.User)
                    .Include(t => t.Comments).ThenInclude(c => c.User)
                    .Include(t => t.Attachments)
                    .FirstOrDefault(t => t.Id == task.Id);

       
                worksheet.Cell(row, 1).Value = task.Id;
                worksheet.Cell(row, 2).Value = task.Title;
                worksheet.Cell(row, 3).Value = task.Description;
                worksheet.Cell(row, 4).Value = task.DueDate;
                worksheet.Cell(row, 4).Style.DateFormat.Format = "yyyy-mm-dd";
                worksheet.Cell(row, 5).Value = task.Status;
                worksheet.Cell(row, 6).Value = task.Priority;
                worksheet.Cell(row, 7).Value = task.CategoryName;
                worksheet.Cell(row, 8).Value = task.CreatedBy;

                worksheet.Cell(row, 9).Value = fullTask?.UserTasks.Any() == true
                    ? string.Join("\n", fullTask.UserTasks.Select(ut =>
                        $"{ut.User.FirstName} {ut.User.LastName} ({ut.User.Email})"))
                    : "None";

                worksheet.Cell(row, 10).Value = fullTask?.Comments.Any() == true
                    ? string.Join("\n\n", fullTask.Comments.Select(c =>
                        $"{c.Text}\n- {c.User.FirstName} {c.User.LastName}, {c.CreatedAt:yyyy-MM-dd HH:mm}"))
                    : "None";

          
                worksheet.Cell(row, 11).Value = fullTask?.Attachments.Any() == true
                    ? string.Join("\n", fullTask.Attachments.Select(a =>
                        $"{a.FileName}\n(uploaded by {a.User.FirstName} {a.User.LastName} on {a.UploadedAt:yyyy-MM-dd})"))
                    : "None";

             
                worksheet.Cell(row, 9).Style.Alignment.WrapText = true;
                worksheet.Cell(row, 10).Style.Alignment.WrapText = true;
                worksheet.Cell(row, 11).Style.Alignment.WrapText = true;

                row++;
            }

           
            worksheet.Columns().AdjustToContents();
            worksheet.Column(10).Width = 40; 
            worksheet.Column(11).Width = 30; 

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return (stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx");
        }

        private (byte[], string, string) GeneratePdfExport(List<TaskDto> tasks, ApplicationDbContext context)
        {
            QuestPDF.Settings.License = LicenseType.Community;

        
            var fullTasks = context.Tasks
                .Where(t => tasks.Select(x => x.Id).Contains(t.Id))
                .Include(t => t.UserTasks)
                    .ThenInclude(ut => ut.User)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Attachments)
                .ToList();

            var document = Document.Create(container =>
            {
                foreach (var task in fullTasks)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                     
                        page.Header()
                            .PaddingBottom(10)
                            .Text($"Task: {task.Title}")
                            .Bold().FontSize(14);

                   
                        page.Content()
                            .Column(column =>
                            {
                              
                                column.Item().Grid(grid =>
                                {
                                    grid.Columns(2);
                                    grid.Item().Text($"ID: {task.Id}");
                                    grid.Item().Text($"Status: {task.Status}");
                                    grid.Item().Text($"Due Date: {task.DueDate:yyyy-MM-dd}");
                                    grid.Item().Text($"Priority: {task.Priority}");
                                });

                       
                                column.Item().PaddingTop(10).Text("Assigned To:").Bold();
                                column.Item().PaddingBottom(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    foreach (var assignment in task.UserTasks)
                                    {
                                        table.Cell().Text(assignment.User.Email);
                                        table.Cell().Text($"{assignment.User.FirstName} {assignment.User.LastName}");
                                    }
                                });

                                column.Item().PaddingTop(10).Text($"Comments ({task.Comments.Count}):").Bold();
                                foreach (var comment in task.Comments.OrderBy(c => c.CreatedAt))
                                {
                                    column.Item()
                                        .Border(1)
                                        .BorderColor(Colors.Grey.Lighten2)
                                        .Padding(5)
                                        .Column(commentColumn =>
                                        {
                                            commentColumn.Item().Text(comment.Text);
                                            commentColumn.Item().AlignRight()
                                                .Text($"- {comment.User?.FirstName} {comment.User?.LastName}, {comment.CreatedAt:g}")
                                                .FontSize(8);
                                        });
                                }

                             
                                column.Item().PaddingTop(10).Text($"Attachments ({task.Attachments.Count}):").Bold();
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    foreach (var attachment in task.Attachments.OrderBy(a => a.UploadedAt))
                                    {
                                        table.Cell().Text(attachment.FileName);
                                        table.Cell().Text($"Uploaded: {attachment.UploadedAt:g} by {attachment.User?.FirstName} {attachment.User?.LastName}");
                                    }
                                });
                            });

                
                        page.Footer()
                            .AlignCenter()
                            .Text(x => x.CurrentPageNumber());
                    });
                }
            });

            return (document.GeneratePdf(), "application/pdf", "pdf");
        }
     
        private static string GetChangeType(string description)
        {
            if (description.Contains("status", StringComparison.OrdinalIgnoreCase))
                return "StatusChange";
            if (description.Contains("comment", StringComparison.OrdinalIgnoreCase))
                return "Comment";
            if (description.Contains("attachment", StringComparison.OrdinalIgnoreCase))
                return "Attachment";
            if (description.Contains("assigned", StringComparison.OrdinalIgnoreCase))
                return "Assignment";
            if (description.Contains("created", StringComparison.OrdinalIgnoreCase))
                return "Creation";

            return "Other";
        }

    }
}

