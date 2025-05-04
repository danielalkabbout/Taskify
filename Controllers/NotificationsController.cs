using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagementSystem.Models;
using TaskManagementSystem.Models.DTOs;

namespace TaskManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

     
        [HttpGet]
        [ProducesResponseType(typeof(List<NotificationDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        Message = n.Message,
                        CreatedAt = n.CreatedAt,
                        IsRead = n.IsRead
                    })
                    .ToListAsync();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Failed to fetch notifications",
                    Error = ex.Message
                });
            }
        }

        [HttpPut("{id}/mark-as-read")]  
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

                if (notification == null)
                {
                    return NotFound();
                }

                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Failed to mark notification as read",
                    Error = ex.Message
                });
            }
        }

    
        [HttpPost]
        [ProducesResponseType(typeof(NotificationDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "Admin,Manager")] 
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var notification = new Notification
                {
                    UserId = dto.UserId,
                    Message = dto.Message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var result = new NotificationDto
                {
                    Id = notification.Id,
                    Message = notification.Message,
                    CreatedAt = notification.CreatedAt,
                    IsRead = notification.IsRead
                };

                return CreatedAtAction(nameof(GetNotifications), result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Failed to create notification",
                    Error = ex.Message
                });
            }
        }
    }
}