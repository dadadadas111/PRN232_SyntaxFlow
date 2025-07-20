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

        /// <summary>
        /// Get all blocks owned by the current user
        /// </summary>
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

        /// <summary>
        /// Get all public blocks with filtering, sorting, and pagination
        /// </summary>
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

                var blocks = await _blockService.GetPublicBlocksAsync(search, tagArray, sortBy, page, size);
                return Ok(blocks);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific public block by ID (accessible to all authenticated users)
        /// </summary>
        [HttpGet("public/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<BlockResponse>> GetPublicBlock(int id)
        {
            try
            {
                var block = await _blockService.GetPublicBlockByIdAsync(id);
                
                if (block == null)
                    return NotFound(new { message = "Public block not found" });

                return Ok(block);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific block by ID (only if owned by current user)
        /// </summary>
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

        /// <summary>
        /// Create a new block
        /// </summary>
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

        /// <summary>
        /// Update an existing block (only if owned by current user)
        /// </summary>
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

        /// <summary>
        /// Delete a block (only if owned by current user)
        /// </summary>
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
    }
}
