using Models;

namespace Services
{
    public interface IAiCodeGeneratorService
    {
        /// <summary>
        /// Generates code in the specified language using AI
        /// </summary>
        /// <param name="ast">The Blockly AST</param>
        /// <param name="targetLanguage">Target programming language</param>
        /// <returns>Generated code as async enumerable for streaming</returns>
        IAsyncEnumerable<string> GenerateCodeStreamAsync(BlocklyAstDto ast, string targetLanguage);
        
        /// <summary>
        /// Generates code in the specified language using AI (non-streaming)
        /// </summary>
        /// <param name="ast">The Blockly AST</param>
        /// <param name="targetLanguage">Target programming language</param>
        /// <returns>Complete generated code</returns>
        Task<string> GenerateCodeAsync(BlocklyAstDto ast, string targetLanguage);
    }
}
