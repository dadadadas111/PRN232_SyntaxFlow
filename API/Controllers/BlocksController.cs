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
