using System.Text.Json;
using System.Collections.Generic;

namespace Models
{
    /// <summary>
    /// Data Transfer Object for receiving Blockly workspace JSON AST.
    /// </summary>
    public class BlocklyAstDto
    {
        /// <summary>
        /// The root blocks object from Blockly's JSON AST.
        /// </summary>
        public JsonElement Blocks { get; set; }
        /// <summary>
        /// The variables array from Blockly's JSON AST (optional, may be undefined).
        /// </summary>
        public JsonElement? Variables { get; set; }
    }
}
