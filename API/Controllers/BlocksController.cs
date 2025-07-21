using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Services;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BlocksController : ControllerBase
    {
        private readonly IBlockService _blockService;

        public BlocksController(IBlockService blockService)
        {
            _blockService = blockService;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                   throw new UnauthorizedAccessException("User ID not found in token");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlockListResponse>>> GetUserBlocks()
        {
            try
            {
                var userId = GetCurrentUserId();
                var blocks = await _blockService.GetUserBlocksAsync(userId);
                return Ok(blocks);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<BlockListResponse>>> GetPublicBlocks(
            [FromQuery] string? search = null,
            [FromQuery] string? tags = null,
            [FromQuery] string sortBy = "created",
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (size < 1 || size > 50) size = 10; // Max 50 items per page

                // Parse tags parameter
                string[]? tagArray = null;
                if (!string.IsNullOrWhiteSpace(tags))
                {
                    tagArray = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(t => t.Trim())
                                  .ToArray();
                }

                // Get current user ID if authenticated
                string? currentUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    currentUserId = GetCurrentUserId();
                }

                var blocks = await _blockService.GetPublicBlocksAsync(search, tagArray, sortBy, page, size, currentUserId);
                return Ok(blocks);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("public/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<BlockResponse>> GetPublicBlock(int id)
        {
            try
            {
                // Get current user ID if authenticated
                string? currentUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    try
                    {
                        currentUserId = GetCurrentUserId();
                    }
                    catch
                    {
                        // Ignore if can't get user ID (anonymous access)
                    }
                }

                var block = await _blockService.GetPublicBlockByIdAsync(id, currentUserId);
                
                if (block == null)
                    return NotFound(new { message = "Public block not found" });

                return Ok(block);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BlockResponse>> GetBlock(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var block = await _blockService.GetBlockByIdAsync(id, userId);
                
                if (block == null)
                    return NotFound(new { message = "Block not found or access denied" });

                return Ok(block);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<BlockResponse>> CreateBlock([FromBody] CreateBlockRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetCurrentUserId();
                var block = await _blockService.CreateBlockAsync(request, userId);
                
                return CreatedAtAction(nameof(GetBlock), new { id = block.Id }, block);
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

        [HttpPut("{id}")]
        public async Task<ActionResult<BlockResponse>> UpdateBlock(int id, [FromBody] UpdateBlockRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetCurrentUserId();
                var block = await _blockService.UpdateBlockAsync(id, request, userId);
                
                if (block == null)
                    return NotFound(new { message = "Block not found or access denied" });

                return Ok(block);
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

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBlock(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _blockService.DeleteBlockAsync(id, userId);
                
                if (!success)
                    return NotFound(new { message = "Block not found or access denied" });

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/star")]
        public async Task<ActionResult<StarResponse>> StarBlock(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _blockService.StarBlockAsync(id, userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/star")]
        public async Task<ActionResult<StarResponse>> UnstarBlock(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _blockService.UnstarBlockAsync(id, userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("starred")]
        public async Task<ActionResult<IEnumerable<BlockListResponse>>> GetStarredBlocks()
        {
            try
            {
                var userId = GetCurrentUserId();
                var blocks = await _blockService.GetStarredBlocksAsync(userId);
                return Ok(blocks);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/stars")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<StarUserResponse>>> GetBlockStars(int id)
        {
            try
            {
                var stars = await _blockService.GetBlockStarsAsync(id);
                return Ok(stars);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Fork System Endpoints

        [HttpPost("{id}/fork")]
        public async Task<ActionResult<ForkResponse>> ForkBlock(int id, [FromBody] ForkBlockRequest? request = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _blockService.ForkBlockAsync(id, userId, request?.Name);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get all forks of a specific block
        /// </summary>
        [HttpGet("{id}/forks")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<BlockListResponse>>> GetBlockForks(int id)
        {
            try
            {
                var forks = await _blockService.GetBlockForksAsync(id);
                return Ok(forks);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("forked")]
        public async Task<ActionResult<IEnumerable<BlockListResponse>>> GetForkedBlocks()
        {
            try
            {
                var userId = GetCurrentUserId();
                var forkedBlocks = await _blockService.GetForkedBlocksAsync(userId);
                return Ok(forkedBlocks);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
