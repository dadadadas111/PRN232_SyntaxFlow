using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TranslateController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] object blocklyJson)
        {
            // For now, just return a mock response
            return Ok("// Translated code will appear here\n// Received: " + blocklyJson.ToString());
        }

        [HttpGet]
        public IActionResult Get()
        {
            // For now, just return a mock response
            return Ok("// Translated code will appear here\n// This is a GET request, no Blockly JSON provided.");
        }
    }
}
