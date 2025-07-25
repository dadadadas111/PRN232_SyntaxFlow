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
        private readonly PythonCodeTranslator _pythonTranslator;
        private readonly JavaScriptCodeTranslator _jsTranslator;

        public TranslateController(PythonCodeTranslator pythonTranslator, JavaScriptCodeTranslator jsTranslator)
        {
            _pythonTranslator = pythonTranslator;
            _jsTranslator = jsTranslator;
        }

        [HttpPost]
        public IActionResult Post([FromBody] BlocklyAstDto dto, [FromQuery] string lang = "py")
        {
            if (dto == null)
                return BadRequest("Invalid Blockly AST");

            ICodeTranslator translator = lang?.ToLower() switch
            {
                "js" => _jsTranslator,
                "py" => _pythonTranslator,
                _ => _pythonTranslator // default to Python
            };

            var code = translator.Translate(dto);
            return Ok(code);
        }
    }
}
