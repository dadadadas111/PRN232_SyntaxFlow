using Models;

namespace Services.Interface
{
    /// <summary>
    /// Contract for translating Blockly AST to code.
    /// </summary>
    public interface ICodeTranslator
    {
        /// <summary>
        /// Translates a Blockly AST to Python code.
        /// </summary>
        /// <param name="ast">The Blockly AST DTO.</param>
        /// <returns>Generated Python code as a string.</returns>
        string Translate(BlocklyAstDto ast);
    }
}
