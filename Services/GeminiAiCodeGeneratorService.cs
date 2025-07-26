using System.Text;
using System.Text.Json;
using Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Services
{
    public class GeminiAiCodeGeneratorService : IAiCodeGeneratorService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiAiCodeGeneratorService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash";

        public GeminiAiCodeGeneratorService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<GeminiAiCodeGeneratorService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API key not configured");
        }

        public async IAsyncEnumerable<string> GenerateCodeStreamAsync(BlocklyAstDto ast, string targetLanguage)
        {
            var requestBody = CreateRequestBody(ast, targetLanguage);
            var url = $"{_baseUrl}:streamGenerateContent?alt=sse&key={_apiKey}";

            _logger.LogInformation("Starting AI code generation for language: {Language}", targetLanguage);
            _logger.LogDebug("Request URL: {Url}", url);
            _logger.LogDebug("Request body prepared for language: {Language}", targetLanguage);

            // Try to get the stream results, handling errors separately
            var streamResults = new List<string>();
            var hasError = false;
            string? errorMessage = null;

            try
            {
                await foreach (var chunk in ProcessStreamInternalAsync(url, requestBody, targetLanguage))
                {
                    streamResults.Add(chunk);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI code generation for language: {Language}", targetLanguage);
                hasError = true;
                errorMessage = ex.Message;
            }

            // Now yield the results or error outside of try-catch
            if (hasError)
            {
                yield return $"// Error generating {targetLanguage} code: {errorMessage}";
            }
            else if (streamResults.Count == 0)
            {
                _logger.LogError("No content received from AI service for language: {Language}", targetLanguage);
                yield return $"// Error: No response received from AI service for {targetLanguage} code generation";
            }
            else
            {
                foreach (var result in streamResults)
                {
                    yield return result;
                }
            }
        }

        private async IAsyncEnumerable<string> ProcessStreamInternalAsync(string url, object requestBody, string targetLanguage)
        {
            var requestJson = JsonSerializer.Serialize(requestBody);
            _logger.LogDebug("Sending request to Gemini API. Request size: {Size} bytes", requestJson.Length);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            // Add the required headers for SSE streaming
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("Connection", "keep-alive");

            _logger.LogInformation("Making HTTP request to Gemini API for language: {Language}", targetLanguage);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            
            _logger.LogInformation("Received HTTP response. Status: {StatusCode}", response.StatusCode);
            _logger.LogDebug("Response headers: {Headers}", string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API returned error status {StatusCode} for language {Language}. Response: {ErrorContent}", 
                    response.StatusCode, targetLanguage, errorContent);
                throw new HttpRequestException($"API returned {response.StatusCode} - {errorContent}");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            _logger.LogDebug("Successfully opened response stream for language: {Language}", targetLanguage);

            using var reader = new StreamReader(stream);

            var codeBuilder = new StringBuilder();
            string? line;
            bool hasContent = false;
            int lineCount = 0;
            int chunkCount = 0;
            int emptyDataLines = 0;

            _logger.LogDebug("Starting to read streaming response for language: {Language}", targetLanguage);

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineCount++;
                _logger.LogTrace("Read line {LineNumber}: '{Line}'", lineCount, line);

                // Handle different SSE line types
                if (string.IsNullOrEmpty(line))
                {
                    // Empty lines are message separators in SSE
                    _logger.LogTrace("Received empty line (message separator) for language: {Language}", targetLanguage);
                    continue;
                }

                if (line.StartsWith("event:"))
                {
                    _logger.LogTrace("Received event line: {Line}", line);
                    continue;
                }

                if (line.StartsWith("data: "))
                {
                    var jsonData = line.Substring(6).Trim(); // Remove "data: " prefix
                    
                    _logger.LogTrace("Processing data line for language: {Language}. Content: '{JsonData}'", targetLanguage, 
                        jsonData.Length > 200 ? jsonData.Substring(0, 200) + "..." : jsonData);

                    if (jsonData == "[DONE]")
                    {
                        _logger.LogDebug("Received [DONE] signal, ending stream for language: {Language}", targetLanguage);
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(jsonData))
                    {
                        emptyDataLines++;
                        _logger.LogTrace("Skipping empty data line #{Count} for language: {Language}", emptyDataLines, targetLanguage);
                        continue;
                    }

                    var textChunk = ParseStreamChunk(jsonData, targetLanguage);
                    if (!string.IsNullOrEmpty(textChunk))
                    {
                        chunkCount++;
                        codeBuilder.Append(textChunk);
                        hasContent = true;
                        _logger.LogTrace("Yielding chunk {ChunkNumber} (length: {Length}) for language: {Language}: '{Preview}'", 
                            chunkCount, textChunk.Length, targetLanguage, 
                            textChunk.Length > 50 ? textChunk.Substring(0, 50) + "..." : textChunk);
                        yield return textChunk;
                    }
                    else
                    {
                        _logger.LogTrace("No text content in chunk for language: {Language}", targetLanguage);
                    }
                }
                else
                {
                    _logger.LogTrace("Received non-data line: '{Line}'", line);
                }
            }

            _logger.LogInformation("Finished reading stream for language: {Language}. Lines: {LineCount}, Chunks: {ChunkCount}, HasContent: {HasContent}, TotalLength: {TotalLength}, EmptyDataLines: {EmptyDataLines}", 
                targetLanguage, lineCount, chunkCount, hasContent, codeBuilder.Length, emptyDataLines);

            // If no content was received, this will be handled by the calling method
            if (!hasContent)
            {
                _logger.LogError("No content received from AI service for language: {Language}. Total lines read: {LineCount}, Empty data lines: {EmptyDataLines}", 
                    targetLanguage, lineCount, emptyDataLines);
                
                // Log a sample of what we actually received for debugging
                if (lineCount > 0)
                {
                    _logger.LogError("Sample of received content (first few lines): This should help debug the response format");
                }
                
                throw new InvalidOperationException($"No content received from AI service for {targetLanguage} code generation. Received {lineCount} lines but no parseable content.");
            }
        }

        private string? ParseStreamChunk(string jsonData, string targetLanguage)
        {
            try
            {
                _logger.LogTrace("Parsing JSON chunk for language: {Language}. Data: '{JsonData}'", targetLanguage, 
                    jsonData.Length > 300 ? jsonData.Substring(0, 300) + "..." : jsonData);
                
                // Handle potential JSON parsing issues - sometimes the API returns partial JSON
                if (!jsonData.StartsWith("{") || !jsonData.EndsWith("}"))
                {
                    _logger.LogWarning("Received malformed JSON for language: {Language}. Data: '{JsonData}'", targetLanguage, jsonData);
                    return null;
                }

                var streamResponse = JsonSerializer.Deserialize<GeminiStreamResponse>(jsonData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });
                
                if (streamResponse == null)
                {
                    _logger.LogWarning("Deserialized response is null for language: {Language}", targetLanguage);
                    return null;
                }

                _logger.LogTrace("Parsed response for language: {Language}. Candidates: {CandidateCount}", 
                    targetLanguage, streamResponse.Candidates?.Length ?? 0);

                if (streamResponse.Candidates == null || !streamResponse.Candidates.Any())
                {
                    _logger.LogWarning("No candidates in response for language: {Language}. Response: {Response}", 
                        targetLanguage, JsonSerializer.Serialize(streamResponse));
                    return null;
                }

                var firstCandidate = streamResponse.Candidates.First();
                if (firstCandidate.Content == null)
                {
                    _logger.LogWarning("No content in first candidate for language: {Language}. Candidate: {Candidate}", 
                        targetLanguage, JsonSerializer.Serialize(firstCandidate));
                    return null;
                }

                if (firstCandidate.Content.Parts == null || !firstCandidate.Content.Parts.Any())
                {
                    _logger.LogWarning("No parts in content for language: {Language}. Content: {Content}", 
                        targetLanguage, JsonSerializer.Serialize(firstCandidate.Content));
                    return null;
                }

                var firstPart = firstCandidate.Content.Parts.First();
                var text = firstPart.Text;
                
                if (string.IsNullOrEmpty(text))
                {
                    _logger.LogTrace("Empty text in part for language: {Language}. Part: {Part}", 
                        targetLanguage, JsonSerializer.Serialize(firstPart));
                    return null;
                }
                else
                {
                    _logger.LogTrace("Successfully extracted text chunk for language: {Language}. Length: {Length}, Preview: '{Preview}'", 
                        targetLanguage, text.Length, text.Length > 100 ? text.Substring(0, 100) + "..." : text);
                    return text;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("Failed to parse streaming response for language: {Language}. Error: {Error}. JSON data: '{JsonData}'", 
                    targetLanguage, ex.Message, jsonData);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error parsing streaming response for language: {Language}. JSON data: '{JsonData}'", 
                    targetLanguage, jsonData);
                return null;
            }
        }

        public async Task<string> GenerateCodeAsync(BlocklyAstDto ast, string targetLanguage)
        {
            _logger.LogInformation("Starting non-streaming AI code generation for language: {Language}", targetLanguage);
            
            var codeBuilder = new StringBuilder();
            var hasError = false;
            var chunkCount = 0;
            
            try
            {
                await foreach (var chunk in GenerateCodeStreamAsync(ast, targetLanguage))
                {
                    chunkCount++;
                    codeBuilder.Append(chunk);
                    
                    // Check if this chunk indicates an error
                    if (chunk.StartsWith("// Error:"))
                    {
                        hasError = true;
                        _logger.LogWarning("Error chunk received for language: {Language}. Chunk: {Chunk}", targetLanguage, chunk);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI code generation for language: {Language}", targetLanguage);
                return $"// Error generating {targetLanguage} code: {ex.Message}";
            }
            
            var result = codeBuilder.ToString();
            
            _logger.LogInformation("Completed non-streaming generation for language: {Language}. Chunks: {ChunkCount}, Length: {Length}, HasError: {HasError}", 
                targetLanguage, chunkCount, result.Length, hasError);
            
            // If we have no content or only error messages, return a proper error
            if (string.IsNullOrWhiteSpace(result) || (hasError && result.Trim().StartsWith("// Error:")))
            {
                _logger.LogError("Failed to generate valid code for language: {Language}. Result: {Result}", targetLanguage, result);
                return $"// Error: Failed to generate {targetLanguage} code using AI service";
            }
            
            return result;
        }

        private object CreateRequestBody(BlocklyAstDto ast, string targetLanguage)
        {
            _logger.LogDebug("Creating request body for language: {Language}", targetLanguage);
            
            var astJson = JsonSerializer.Serialize(ast, new JsonSerializerOptions { WriteIndented = true });
            
            _logger.LogDebug("AST JSON prepared for language: {Language}. Size: {Size} bytes", targetLanguage, astJson.Length);
            
            var systemInstruction = $@"You are an expert code translator. Convert Blockly visual programming blocks (represented as JSON) into {targetLanguage} code.

Rules:
1. Generate clean, readable, and functional {targetLanguage} code
2. Include necessary imports/using statements, or separated methods if needed
3. Add helpful comments explaining the logic
4. Follow {targetLanguage} best practices and conventions
5. Handle edge cases appropriately
6. Only return the code, no explanations unless asked
7. If you encounter unsupported blocks, add a comment explaining what they would do
The JSON represents a visual programming workspace with blocks that form a program flow.";

            var userPrompt = $@"Convert this Blockly JSON to {targetLanguage} code:

```json
{astJson}
```

Please generate clean, functional {targetLanguage} code that implements the logic described by these blocks.";

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new { text = systemInstruction }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = userPrompt }
                        }
                    }
                },
                safetySettings = new[]
                {
                    new
                    {
                        category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                        threshold = "BLOCK_ONLY_HIGH"
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3, // Lower temperature for more consistent code generation
                    maxOutputTokens = 2048,
                    topP = 0.8,
                    topK = 10
                }
            };

            _logger.LogDebug("Request body created for language: {Language}", targetLanguage);
            return requestBody;
        }
    }

    // Response models for Gemini API
    public class GeminiStreamResponse
    {
        public GeminiCandidate[]? Candidates { get; set; }
        public GeminiUsageMetadata? UsageMetadata { get; set; }
        public string? ModelVersion { get; set; }
        public string? ResponseId { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    public class GeminiContent
    {
        public GeminiPart[]? Parts { get; set; }
        public string? Role { get; set; }
    }

    public class GeminiPart
    {
        public string? Text { get; set; }
    }

    public class GeminiUsageMetadata
    {
        public int PromptTokenCount { get; set; }
        public int TotalTokenCount { get; set; }
    }
}

