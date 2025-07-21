using System.Text.Json;
using System.Collections.Generic;

namespace Models
{
    public class BlocklyAstDto
    {
        public JsonElement Blocks { get; set; }
        public JsonElement? Variables { get; set; }
    }
}
