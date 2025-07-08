using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Services;

namespace API.Controllers
{
    // [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TranslateController : ControllerBase
    {
        private readonly ICodeTranslator _translator;

        public TranslateController(ICodeTranslator translator)
        {
            _translator = translator;
        }

        [HttpPost]
        public IActionResult Post([FromBody] BlocklyAstDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid Blockly AST");
            var pythonCode = _translator.TranslateToPython(dto);
            return Ok(pythonCode);
        }
    }
}
