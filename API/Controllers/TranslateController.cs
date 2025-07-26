using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using Services;
using System.Text;

namespace API.Controllers
{
    // [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TranslateController : ControllerBase
    {
        private readonly PythonCodeTranslator _pythonTranslator;
        private readonly JavaScriptCodeTranslator _jsTranslator;
        private readonly IAiCodeGeneratorService _aiCodeGenerator;

        public TranslateController(
            PythonCodeTranslator pythonTranslator, 
            JavaScriptCodeTranslator jsTranslator,
            IAiCodeGeneratorService aiCodeGenerator)
        {
            _pythonTranslator = pythonTranslator;
            _jsTranslator = jsTranslator;
            _aiCodeGenerator = aiCodeGenerator;
        }

        [HttpPost]
        public IActionResult Post([FromBody] BlocklyAstDto dto, [FromQuery] string lang = "py")
        {
            if (dto == null)
                return BadRequest("Invalid Blockly AST");

            ICodeTranslator? translator = lang?.ToLower() switch
            {
                "js" => _jsTranslator,
                "py" => _pythonTranslator,
                _ => null
            };

            if (translator != null)
            {
                // Use native translator for supported languages
                var code = translator.Translate(dto);
                return Ok(code);
            }
            else
            {
                // Use AI for unsupported languages - but return synchronously for this endpoint
                try
                {
                    var aiCode = _aiCodeGenerator.GenerateCodeAsync(dto, lang).Result;
                    return Ok(aiCode);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Failed to generate {lang} code: {ex.Message}");
                }
            }
        }

        [HttpPost("stream")]
        public async Task<IActionResult> PostStream([FromBody] BlocklyAstDto dto, [FromQuery] string lang = "py")
        {
            if (dto == null)
                return BadRequest("Invalid Blockly AST");

            // Check if we have a native translator
            ICodeTranslator? translator = lang?.ToLower() switch
            {
                "js" => _jsTranslator,
                "py" => _pythonTranslator,
                _ => null
            };

            if (translator != null)
            {
                // For native translators, return immediately
                var code = translator.Translate(dto);
                Response.ContentType = "text/plain";
                await Response.WriteAsync(code);
                return new EmptyResult();
            }

            // Use AI streaming for unsupported languages
            Response.ContentType = "text/plain";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                await foreach (var chunk in _aiCodeGenerator.GenerateCodeStreamAsync(dto, lang))
                {
                    await Response.WriteAsync(chunk);
                    await Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                await Response.WriteAsync($"\n// Error: {ex.Message}");
            }

            return new EmptyResult();
        }

        [HttpGet("supported-languages")]
        public IActionResult GetSupportedLanguages()
        {
            var languages = new
            {
                native = new[]
                {
                    new { code = "py", name = "Python", type = "native" },
                    new { code = "js", name = "JavaScript", type = "native" }
                },
                ai_supported = new[]
                {
                    new { code = "java", name = "Java", type = "ai" },
                    new { code = "cpp", name = "C++", type = "ai" },
                    new { code = "csharp", name = "C#", type = "ai" },
                    new { code = "go", name = "Go", type = "ai" },
                    new { code = "rust", name = "Rust", type = "ai" },
                    new { code = "php", name = "PHP", type = "ai" },
                    new { code = "ruby", name = "Ruby", type = "ai" },
                    new { code = "swift", name = "Swift", type = "ai" },
                    new { code = "kotlin", name = "Kotlin", type = "ai" },
                    new { code = "dart", name = "Dart", type = "ai" }
                }
            };

            return Ok(languages);
        }
    }
}
