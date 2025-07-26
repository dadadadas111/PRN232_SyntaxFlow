using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Services.Interface;
using System.Security.Claims;

namespace API.Controllers
{
    /// <summary>
    /// API endpoints for managing comments on blocks
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    [Tags("Comments")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IMqttService _mqttService;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(ICommentService commentService, IMqttService mqttService, ILogger<CommentsController> logger)
        {
            _commentService = commentService;
            _mqttService = mqttService;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                   throw new UnauthorizedAccessException("User ID not found in token");
        }

        /// <summary>
        /// Get all comments for a specific block
        /// </summary>
        /// <param name="blockId">Block ID</param>
        /// <returns>List of comments for the block</returns>
        /// <response code="200">Returns comments for the block</response>
        /// <response code="401">User is not authenticated</response>
        [HttpGet("block/{blockId}")]
        [ProducesResponseType(typeof(IEnumerable<CommentDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByBlockId(int blockId)
        {
            try
            {
                var comments = await _commentService.GetCommentsByBlockIdAsync(blockId);
                
                // Mark comments owned by current user
                var currentUserId = GetCurrentUserId();
                foreach (var comment in comments)
                {
                    comment.IsOwner = comment.UserId == currentUserId;
                }
                
                return Ok(comments);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get comment count for a specific block
        /// </summary>
        /// <param name="blockId">Block ID</param>
        /// <returns>Number of comments</returns>
        /// <response code="200">Returns comment count</response>
        [HttpGet("block/{blockId}/count")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(int), 200)]
        public async Task<ActionResult<int>> GetCommentCount(int blockId)
        {
            var count = await _commentService.GetCommentCountByBlockIdAsync(blockId);
            return Ok(count);
        }

        /// <summary>
        /// Create a new comment on a block
        /// </summary>
        /// <param name="dto">Comment creation data</param>
        /// <returns>Created comment details</returns>
        /// <response code="201">Comment created successfully</response>
        /// <response code="400">Invalid request data or block not accessible</response>
        /// <response code="401">User is not authenticated</response>
        [HttpPost]
        [ProducesResponseType(typeof(CommentDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<CommentDto>> CreateComment([FromBody] CommentCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetCurrentUserId();
                var comment = await _commentService.CreateCommentAsync(dto, userId);
                
                if (comment == null)
                    return BadRequest(new { message = "Block not found or not accessible" });

                // Publish to MQTT for real-time updates (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("🚀 Triggering MQTT publish for new comment - CommentId: {CommentId}, BlockId: {BlockId}", 
                            comment.Id, comment.BlockId);
                        await _mqttService.PublishCommentAsync(comment);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the request
                        _logger.LogError(ex, "❌ MQTT publish failed for new comment - CommentId: {CommentId}", comment.Id);
                    }
                });

                return CreatedAtAction(
                    nameof(GetCommentsByBlockId), 
                    new { blockId = comment.BlockId }, 
                    comment);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing comment (only by comment owner)
        /// </summary>
        /// <param name="id">Comment ID</param>
        /// <param name="dto">Comment update data</param>
        /// <returns>Updated comment details</returns>
        /// <response code="200">Comment updated successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="404">Comment not found or access denied</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(CommentDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CommentDto>> UpdateComment(int id, [FromBody] CommentUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetCurrentUserId();
                var comment = await _commentService.UpdateCommentAsync(id, dto.Content, userId);
                
                if (comment == null)
                    return NotFound(new { message = "Comment not found or access denied" });

                // Publish to MQTT for real-time updates (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("🚀 Triggering MQTT publish for comment update - CommentId: {CommentId}, BlockId: {BlockId}", 
                            comment.Id, comment.BlockId);
                        await _mqttService.PublishCommentUpdateAsync(comment);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the request
                        _logger.LogError(ex, "❌ MQTT publish failed for comment update - CommentId: {CommentId}", comment.Id);
                    }
                });

                return Ok(comment);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a comment (only by comment owner)
        /// </summary>
        /// <param name="id">Comment ID</param>
        /// <returns>No content on success</returns>
        /// <response code="204">Comment deleted successfully</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="404">Comment not found or access denied</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteComment(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _commentService.DeleteCommentAsync(id, userId);
                
                if (!success)
                    return NotFound(new { message = "Comment not found or access denied" });

                // Publish to MQTT for real-time updates (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("🚀 Triggering MQTT publish for comment deletion - CommentId: {CommentId}", id);
                        await _mqttService.PublishCommentDeleteAsync(id);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the request
                        _logger.LogError(ex, "❌ MQTT publish failed for comment deletion - CommentId: {CommentId}", id);
                    }
                });

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
