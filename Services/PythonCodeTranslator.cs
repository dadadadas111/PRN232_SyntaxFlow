using System;
using System.Text;
using System.Text.Json;
using Models;

namespace Services
{
    public class PythonCodeTranslator : ICodeTranslator
    {
        private Dictionary<string, string> _variableIdToName = new();
        private HashSet<string> usedHelpers = new HashSet<string>();

        private bool needsMathImport = false;
        private bool needsRandomImport = false;

        public string TranslateToPython(BlocklyAstDto ast)
        {
            _variableIdToName.Clear();
            usedHelpers.Clear();
            needsMathImport = false;
            needsRandomImport = false;
            var helpers = new StringBuilder();
            // Map variable IDs to names if present
            if (ast.Variables.HasValue && ast.Variables.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var varObj in ast.Variables.Value.EnumerateArray())
                {
                    if (varObj.TryGetProperty("id", out var idElem) && varObj.TryGetProperty("name", out var nameElem))
                    {
                        _variableIdToName[idElem.GetString() ?? ""] = nameElem.GetString() ?? "";
                    }
                }
            }

            if (ast == null || ast.Blocks.ValueKind != JsonValueKind.Object)
                return "# Invalid or empty AST";

            var sb = new StringBuilder();
            // Support both Blockly JSON root formats: { blocks: { blocks: [...] } } and { blocks: { root: { children: [...] } } }
            if (ast.Blocks.TryGetProperty("blocks", out var blocksElem))
            {
                // Newer Blockly: { blocks: [...] }
                if (blocksElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var block in blocksElem.EnumerateArray())
                    {
                        TranslateBlock(block, sb, 0);
                    }
                }
                // Legacy Blockly: { root: { children: [...] } }
                else if (blocksElem.ValueKind == JsonValueKind.Object)
                {
                    if (blocksElem.TryGetProperty("root", out var rootElem) && rootElem.ValueKind == JsonValueKind.Object)
                    {
                        if (rootElem.TryGetProperty("children", out var childrenElem) && childrenElem.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var block in childrenElem.EnumerateArray())
                            {
                                TranslateBlock(block, sb, 0);
                            }
                        }
                    }
                }
            }
            // Add header comments
            var header = new StringBuilder();
            if (needsMathImport) header.AppendLine("import math");
            if (needsRandomImport) header.AppendLine("import random");
            header.AppendLine("# Translated code by SyntaxFlow Blockly-to-Python engine");
            header.AppendLine("# Helper methods are auto-included as needed for block compatibility\n");

            // After translation, prepend helpers if any
            if (usedHelpers.Contains("is_prime"))
            {
                helpers.AppendLine("def is_prime(n):");
                helpers.AppendLine("    if n <= 1: return False");
                helpers.AppendLine("    if n == 2: return True");
                helpers.AppendLine("    if n % 2 == 0: return False");
                helpers.AppendLine("    for i in range(3, int(n ** 0.5) + 1, 2):");
                helpers.AppendLine("        if n % i == 0: return False");
                helpers.AppendLine("    return True\n");
                helpers.AppendLine("# --- End of helpers, user code below ---\n");

            }
            return header.ToString() + helpers.ToString() + sb.ToString();
        }

        private void TranslateBlock(JsonElement block, StringBuilder sb, int indent)
        {
            if (!block.TryGetProperty("type", out var typeElem)) return;
            var type = typeElem.GetString();
            switch (type)
            {
                case "text_print":
                    HandlePrint(block, sb, indent);
                    break;
                case "math_arithmetic":
                    sb.AppendLine(Indent(indent) + HandleMath(block));
                    break;
                case "controls_if":
                    HandleIf(block, sb, indent);
                    break;
                case "controls_repeat_ext":
                    HandleRepeat(block, sb, indent);
                    break;
                case "variables_set":
                    HandleVariableSet(block, sb, indent);
                    break;
                case "math_change":
                    HandleMathChange(block, sb, indent);
                    break;
                case "variables_get":
                    var varFieldGet = block.GetProperty("fields").GetProperty("VAR");
                    string varNameGet = "";
                    if (varFieldGet.ValueKind == JsonValueKind.Object && varFieldGet.TryGetProperty("id", out var idElemGet))
                    {
                        var id = idElemGet.GetString() ?? "";
                        varNameGet = _variableIdToName.TryGetValue(id, out var name) ? name : id;
                    }
                    else if (varFieldGet.ValueKind == JsonValueKind.String)
                    {
                        varNameGet = varFieldGet.GetString() ?? "";
                    }
                    else
                    {
                        varNameGet = varFieldGet.ToString();
                    }
                    sb.Append(varNameGet);
                    break;
                case "text":
                    var textValue = block.GetProperty("fields").GetProperty("TEXT").GetString() ?? "";
                    sb.Append('"').Append(textValue.Replace("\"", "\\\"")).Append('"');
                    break;
                // Logic blocks
                case "logic_compare":
                    sb.Append(HandleLogicCompare(block));
                    break;
                case "logic_operation":
                    sb.Append(HandleLogicOperation(block));
                    break;
                case "logic_negate":
                    sb.Append($"not {ExtractValue(block, "BOOL")}");
                    break;
                case "logic_boolean":
                    var boolVal = block.GetProperty("fields").GetProperty("BOOL").GetString();
                    sb.Append(boolVal == "TRUE" ? "True" : "False");
                    break;
                case "logic_null":
                    sb.Append("None");
                    break;
                case "logic_ternary":
                    var cond = ExtractValue(block, "IF");
                    var thenVal = ExtractValue(block, "THEN");
                    var elseVal = ExtractValue(block, "ELSE");
                    sb.Append($"({thenVal} if {cond} else {elseVal})");
                    break;
                // Loops
                case "controls_whileUntil":
                    HandleWhileUntil(block, sb, indent);
                    break;
                case "controls_for":
                    HandleFor(block, sb, indent);
                    break;
                case "controls_forEach":
                    HandleForEach(block, sb, indent);
                    break;
                case "controls_flow_statements":
                    var flow = block.GetProperty("fields").GetProperty("FLOW").GetString();
                    sb.AppendLine(Indent(indent) + (flow == "BREAK" ? "break" : "continue"));
                    break;
                // Math
                case "math_number":
                    var numField = block.GetProperty("fields").GetProperty("NUM");
                    string numValue = numField.ValueKind switch
                    {
                        JsonValueKind.String => numField.GetString() ?? "0",
                        JsonValueKind.Number => numField.GetRawText(),
                        _ => "0"
                    };
                    sb.Append(numValue);
                    break;
                case "math_single":
                    sb.Append(HandleMathSingle(block));
                    break;
                case "math_trig":
                    sb.Append(HandleMathTrig(block));
                    break;
                case "math_constant":
                    sb.Append(HandleMathConstant(block));
                    break;
                case "math_number_property":
                    sb.Append(HandleMathNumberProperty(block));
                    break;
                case "math_round":
                    sb.Append(HandleMathRound(block));
                    break;
                case "math_on_list":
                    sb.Append(HandleMathOnList(block));
                    break;
                case "math_modulo":
                    sb.Append(HandleMathModulo(block));
                    break;
                case "math_constrain":
                    sb.Append(HandleMathConstrain(block));
                    break;
                case "math_random_int":
                    sb.Append(HandleMathRandomInt(block));
                    break;
                case "math_random_float":
                    sb.Append(HandleMathRandomFloat(block));
                    break;
                // Text
                case "text_join":
                    sb.Append(HandleTextJoin(block));
                    break;
                case "text_append":
                    sb.Append(HandleTextAppend(block));
                    break;
                case "text_length":
                    sb.Append(HandleTextLength(block));
                    break;
                case "text_isEmpty":
                    sb.Append(HandleTextIsEmpty(block));
                    break;
                case "text_indexOf":
                    sb.Append(HandleTextIndexOf(block));
                    break;
                case "text_charAt":
                    sb.Append(HandleTextCharAt(block));
                    break;
                case "text_getSubstring":
                    sb.Append(HandleTextGetSubstring(block));
                    break;
                case "text_changeCase":
                    sb.Append(HandleTextChangeCase(block));
                    break;
                case "text_trim":
                    sb.Append(HandleTextTrim(block));
                    break;
                // Lists
                case "lists_create_with":
                    sb.Append(HandleListsCreateWith(block));
                    break;
                case "lists_repeat":
                    sb.Append(HandleListsRepeat(block));
                    break;
                case "lists_length":
                    sb.Append(HandleListsLength(block));
                    break;
                case "lists_isEmpty":
                    sb.Append(HandleListsIsEmpty(block));
                    break;
                case "lists_indexOf":
                    sb.Append(HandleListsIndexOf(block));
                    break;
                case "lists_getIndex":
                    sb.Append(HandleListsGetIndex(block));
                    break;
                case "lists_setIndex":
                    sb.Append(HandleListsSetIndex(block));
                    break;
                case "lists_getSublist":
                    sb.Append(HandleListsGetSublist(block));
                    break;
                case "lists_split":
                    sb.Append(HandleListsSplit(block));
                    break;
                case "lists_sort":
                    sb.Append(HandleListsSort(block));
                    break;
                default:
                    sb.AppendLine(Indent(indent) + $"# Unsupported block: {type}");
                    break;
            }
            // Recursively handle next block in sequence
            if (block.TryGetProperty("next", out var nextElem) && nextElem.ValueKind == JsonValueKind.Object)
            {
                if (nextElem.TryGetProperty("block", out var nextBlock) && nextBlock.ValueKind == JsonValueKind.Object)
                {
                    TranslateBlock(nextBlock, sb, indent);
                }
            }
        }

        private void HandlePrint(JsonElement block, StringBuilder sb, int indent)
        {
            var value = ExtractValue(block, "TEXT");
            sb.AppendLine(Indent(indent) + $"print({value})");
        }

        private string HandleMath(JsonElement block)
        {
            var op = block.GetProperty("fields").GetProperty("OP").GetString();
            var left = ExtractValue(block, "A");
            var right = ExtractValue(block, "B");
            var pyOp = op switch
            {
                "ADD" => "+",
                "MINUS" => "-",
                "MULTIPLY" => "*",
                "DIVIDE" => "/",
                "POWER" => "**",
                _ => op
            };
            return $"{left} {pyOp} {right}";
        }

        private void HandleIf(JsonElement block, StringBuilder sb, int indent)
        {
            int n = 0;
            // Handle all IFx/DOx pairs
            while (true)
            {
                string ifKey = $"IF{n}";
                string doKey = $"DO{n}";
                if (!(block.TryGetProperty("inputs", out var inputsElem) && inputsElem.TryGetProperty(ifKey, out var ifElem)))
                    break;
                var cond = ExtractValue(block, ifKey);
                string keyword = n == 0 ? "if" : "elif";
                sb.AppendLine(Indent(indent) + $"{keyword} {cond}:");
                if (inputsElem.TryGetProperty(doKey, out var doElem))
                {
                    if (doElem.TryGetProperty("block", out var doBlock) && doBlock.ValueKind == JsonValueKind.Object)
                    {
                        TranslateBlock(doBlock, sb, indent + 1);
                    }
                }
                n++;
            }
            // Handle ELSE branch if present
            if (block.TryGetProperty("inputs", out var inputsElem2) && inputsElem2.TryGetProperty("ELSE", out var elseElem))
            {
                sb.AppendLine(Indent(indent) + "else:");
                if (elseElem.TryGetProperty("block", out var elseBlock) && elseBlock.ValueKind == JsonValueKind.Object)
                {
                    TranslateBlock(elseBlock, sb, indent + 1);
                }
            }
        }

        private void HandleRepeat(JsonElement block, StringBuilder sb, int indent)
        {
            var times = ExtractValue(block, "TIMES");
            sb.AppendLine(Indent(indent) + $"for _ in range({times}):");
            if (block.TryGetProperty("inputs", out var inputsElem) && inputsElem.TryGetProperty("DO", out var doElem))
            {
                if (doElem.TryGetProperty("block", out var doBlock) && doBlock.ValueKind == JsonValueKind.Object)
                {
                    TranslateBlock(doBlock, sb, indent + 1);
                }
            }
        }

        private void HandleVariableSet(JsonElement block, StringBuilder sb, int indent)
        {
            string varName = "";
            var varField = block.GetProperty("fields").GetProperty("VAR");
            if (varField.ValueKind == JsonValueKind.Object && varField.TryGetProperty("id", out var idElem))
            {
                var id = idElem.GetString() ?? "";
                varName = _variableIdToName.TryGetValue(id, out var name) ? name : id;
            }
            else if (varField.ValueKind == JsonValueKind.String)
            {
                varName = varField.GetString() ?? "";
            }
            else
            {
                varName = varField.ToString();
            }
            var value = ExtractValue(block, "VALUE");
            sb.AppendLine(Indent(indent) + $"{varName} = {value}");
        }

        private void HandleMathChange(JsonElement block, StringBuilder sb, int indent)
        {
            string varName = "";
            var varField = block.GetProperty("fields").GetProperty("VAR");
            if (varField.ValueKind == JsonValueKind.Object && varField.TryGetProperty("id", out var idElem))
            {
                var id = idElem.GetString() ?? "";
                varName = _variableIdToName.TryGetValue(id, out var name) ? name : id;
            }
            else if (varField.ValueKind == JsonValueKind.String)
            {
                varName = varField.GetString() ?? "";
            }
            else
            {
                varName = varField.ToString();
            }
            var delta = ExtractValue(block, "DELTA");
            sb.AppendLine(Indent(indent) + $"{varName} += {delta}");
        }

        private void HandleWhileUntil(JsonElement block, StringBuilder sb, int indent)
        {
            var mode = block.GetProperty("fields").GetProperty("MODE").GetString();
            var cond = ExtractValue(block, "BOOL");
            var pyCond = mode == "UNTIL" ? $"not ({cond})" : cond;
            sb.AppendLine(Indent(indent) + $"while {pyCond}:");
            if (block.TryGetProperty("inputs", out var inputsElem) && inputsElem.TryGetProperty("DO", out var doElem))
            {
                if (doElem.TryGetProperty("block", out var doBlock) && doBlock.ValueKind == JsonValueKind.Object)
                {
                    TranslateBlock(doBlock, sb, indent + 1);
                }
            }
        }

        private void HandleFor(JsonElement block, StringBuilder sb, int indent)
        {
            string varName = "";
            var varField = block.GetProperty("fields").GetProperty("VAR");
            if (varField.ValueKind == JsonValueKind.Object && varField.TryGetProperty("id", out var idElem))
            {
                var id = idElem.GetString() ?? "";
                varName = _variableIdToName.TryGetValue(id, out var name) ? name : id;
            }
            else if (varField.ValueKind == JsonValueKind.String)
            {
                varName = varField.GetString() ?? "";
            }
            else
            {
                varName = varField.ToString();
            }
            var from = ExtractValue(block, "FROM");
            var to = ExtractValue(block, "TO");
            var by = ExtractValue(block, "BY");
            sb.AppendLine(Indent(indent) + $"for {varName} in range({from}, {to} + 1, {by}):");
            if (block.TryGetProperty("inputs", out var inputsElem) && inputsElem.TryGetProperty("DO", out var doElem))
            {
                if (doElem.TryGetProperty("block", out var doBlock) && doBlock.ValueKind == JsonValueKind.Object)
                {
                    TranslateBlock(doBlock, sb, indent + 1);
                }
            }
        }

        private void HandleForEach(JsonElement block, StringBuilder sb, int indent)
        {
            string varName = "";
            var varField = block.GetProperty("fields").GetProperty("VAR");
            if (varField.ValueKind == JsonValueKind.Object && varField.TryGetProperty("id", out var idElem))
            {
                var id = idElem.GetString() ?? "";
                varName = _variableIdToName.TryGetValue(id, out var name) ? name : id;
            }
            else if (varField.ValueKind == JsonValueKind.String)
            {
                varName = varField.GetString() ?? "";
            }
            else
            {
                varName = varField.ToString();
            }
            var list = ExtractValue(block, "LIST");
            sb.AppendLine(Indent(indent) + $"for {varName} in {list}:");
            if (block.TryGetProperty("inputs", out var inputsElem) && inputsElem.TryGetProperty("DO", out var doElem))
            {
                if (doElem.TryGetProperty("block", out var doBlock) && doBlock.ValueKind == JsonValueKind.Object)
                {
                    TranslateBlock(doBlock, sb, indent + 1);
                }
            }
        }

        private string ExtractValue(JsonElement block, string inputName)
        {
            if (block.TryGetProperty("inputs", out var inputsElem) && inputsElem.TryGetProperty(inputName, out var inputElem))
            {
                if (inputElem.TryGetProperty("block", out var valueBlock) && valueBlock.ValueKind == JsonValueKind.Object)
                {
                    // Recursively translate the value block to a string
                    var sb = new StringBuilder();
                    TranslateBlock(valueBlock, sb, 0);
                    return sb.ToString().Trim();
                }
            }
            // Try fields for literals
            if (block.TryGetProperty("fields", out var fieldsElem) && fieldsElem.TryGetProperty(inputName, out var fieldElem))
            {
                switch (fieldElem.ValueKind)
                {
                    case JsonValueKind.String:
                        return fieldElem.GetString() ?? "";
                    case JsonValueKind.Number:
                        return fieldElem.GetRawText(); // preserves number formatting
                    case JsonValueKind.True:
                        return "True";
                    case JsonValueKind.False:
                        return "False";
                    default:
                        return fieldElem.ToString();
                }
            }
            return "0";
        }

        private string Indent(int level) => new string(' ', level * 4);

        private string HandleLogicCompare(JsonElement block)
        {
            var op = block.GetProperty("fields").GetProperty("OP").GetString();
            var a = ExtractValue(block, "A");
            var b = ExtractValue(block, "B");
            var pyOp = op switch
            {
                "EQ" => "==",
                "NEQ" => "!=",
                "LT" => "<",
                "LTE" => "<=",
                "GT" => ">",
                "GTE" => ">=",
                _ => op
            };
            return $"{a} {pyOp} {b}";
        }

        private string HandleLogicOperation(JsonElement block)
        {
            var op = block.GetProperty("fields").GetProperty("OP").GetString();
            var a = ExtractValue(block, "A");
            var b = ExtractValue(block, "B");
            var pyOp = op switch
            {
                "AND" => "and",
                "OR" => "or",
                _ => op
            };
            return $"({a} {pyOp} {b})";
        }

        private string HandleMathSingle(JsonElement block)
        {
            needsMathImport = true;
            var op = block.GetProperty("fields").GetProperty("OP").GetString();
            var num = ExtractValue(block, "NUM");
            return op switch
            {
                "ROOT" => $"({num}) ** 0.5",
                "ABS" => $"abs({num})",
                "NEG" => $"-({num})",
                "LN" => $"math.log({num})",
                "LOG10" => $"math.log10({num})",
                "EXP" => $"math.exp({num})",
                "POW10" => $"10 ** ({num})",
                _ => $"# Unsupported math_single op: {op}"
            };
        }

        private string HandleMathTrig(JsonElement block)
        {
            needsMathImport = true;
            var op = block.GetProperty("fields").GetProperty("OP").GetString();
            var angle = ExtractValue(block, "NUM");
            return op switch
            {
                "SIN" => $"math.sin(math.radians({angle}))",
                "COS" => $"math.cos(math.radians({angle}))",
                "TAN" => $"math.tan(math.radians({angle}))",
                "ASIN" => $"math.degrees(math.asin({angle}))",
                "ACOS" => $"math.degrees(math.acos({angle}))",
                "ATAN" => $"math.degrees(math.atan({angle}))",
                _ => $"# Unsupported math_trig op: {op}"
            };
        }

        private string HandleMathConstant(JsonElement block)
        {
            needsMathImport = true;
            var constant = block.GetProperty("fields").GetProperty("CONSTANT").GetString();
            return constant switch
            {
                "PI" => "math.pi",
                "E" => "math.e",
                "GOLDEN_RATIO" => "(1 + 5 ** 0.5) / 2",
                "SQRT2" => "math.sqrt(2)",
                "SQRT1_2" => "math.sqrt(0.5)",
                "INFINITY" => "float('inf')",
                _ => $"# Unsupported math_constant: {constant}"
            };
        }

        private string HandleMathNumberProperty(JsonElement block)
        {
            var prop = block.GetProperty("fields").GetProperty("PROPERTY").GetString();
            var num = ExtractValue(block, "NUMBER_TO_CHECK");
            switch (prop)
            {
                case "EVEN": return $"({num}) % 2 == 0";
                case "ODD": return $"({num}) % 2 == 1";
                case "PRIME":
                    // Mark is_prime as used
                    usedHelpers?.Add("is_prime");
                    return $"is_prime({num})";
                case "WHOLE": return $"({num}) % 1 == 0";
                case "POSITIVE": return $"({num}) > 0";
                case "NEGATIVE": return $"({num}) < 0";
                case "DIVISIBLE_BY": return $"({num}) % {ExtractValue(block, "DIVISOR")} == 0";
                default: return $"# Unsupported math_number_property: {prop}";
            }
        }

        private string HandleMathRound(JsonElement block)
        {
            needsMathImport = true;
            var op = block.GetProperty("fields").GetProperty("OP").GetString();
            var num = ExtractValue(block, "NUM");
            return op switch
            {
                "ROUND" => $"round({num})",
                "ROUNDUP" => $"math.ceil({num})",
                "ROUNDDOWN" => $"math.floor({num})",
                _ => $"# Unsupported math_round op: {op}"
            };
        }

        private string HandleMathOnList(JsonElement block) => "# Unsupported: math_on_list";

        private string HandleMathModulo(JsonElement block)
        {
            var dividend = ExtractValue(block, "DIVIDEND");
            var divisor = ExtractValue(block, "DIVISOR");
            return $"({dividend}) % ({divisor})";
        }

        private string HandleMathConstrain(JsonElement block)
        {
            var value = ExtractValue(block, "VALUE");
            var low = ExtractValue(block, "LOW");
            var high = ExtractValue(block, "HIGH");
            return $"min(max({value}, {low}), {high})";
        }

        private string HandleMathRandomInt(JsonElement block)
        {
            needsRandomImport = true;
            var from = ExtractValue(block, "FROM");
            var to = ExtractValue(block, "TO");
            return $"random.randint({from}, {to})";
        }

        private string HandleMathRandomFloat(JsonElement block)
        {
            needsRandomImport = true;
            return "random.random()";
        }

        // --- Text Block Helpers ---
        private string HandleTextJoin(JsonElement block)
        {
            var args = new List<string>();
            // Modern Blockly: ADD0, ADD1, ...
            if (block.TryGetProperty("inputs", out var inputsElem))
            {
                int i = 0;
                while (inputsElem.TryGetProperty($"ADD{i}", out var addElem))
                {
                    if (addElem.TryGetProperty("block", out var itemBlock) && itemBlock.ValueKind == JsonValueKind.Object)
                    {
                        var sb = new StringBuilder();
                        TranslateBlock(itemBlock, sb, 0);
                        var arg = sb.ToString().Trim();
                        // If it's a string literal, don't wrap; else, wrap in str()
                        if (arg.StartsWith("\"") && arg.EndsWith("\""))
                            args.Add(arg);
                        else
                            args.Add($"str({arg})");
                    }
                    i++;
                }
                // Legacy: ITEMS input
                if (i == 0 && inputsElem.TryGetProperty("ITEMS", out var itemsElem))
                {
                    if (itemsElem.TryGetProperty("block", out var itemsBlock) && itemsBlock.ValueKind == JsonValueKind.Object)
                    {
                        FlattenTextJoinItems(itemsBlock, args, true);
                    }
                }
            }
            return string.Join(" + ", args);
        }

        // Overload to support str() wrapping for legacy join
        private void FlattenTextJoinItems(JsonElement block, List<string> args, bool wrapNonString)
        {
            if (block.GetProperty("type").GetString() == "text")
            {
                var textValue = block.GetProperty("fields").GetProperty("TEXT").GetString() ?? "";
                args.Add('"' + textValue.Replace("\"", "\\\"") + '"');
            }
            else if (block.GetProperty("type").GetString() == "text_join")
            {
                // Nested join
                if (block.TryGetProperty("inputs", out var inputsElem) && inputsElem.TryGetProperty("ITEMS", out var itemsElem))
                {
                    if (itemsElem.TryGetProperty("block", out var itemsBlock) && itemsBlock.ValueKind == JsonValueKind.Object)
                    {
                        FlattenTextJoinItems(itemsBlock, args, wrapNonString);
                    }
                }
            }
            else
            {
                var val = ExtractValue(block, "");
                if (wrapNonString && !(val.StartsWith("\"") && val.EndsWith("\"")))
                    args.Add($"str({val})");
                else
                    args.Add(val);
            }
            // Handle next item in the join list
            if (block.TryGetProperty("next", out var nextElem) && nextElem.ValueKind == JsonValueKind.Object)
            {
                if (nextElem.TryGetProperty("block", out var nextBlock) && nextBlock.ValueKind == JsonValueKind.Object)
                {
                    FlattenTextJoinItems(nextBlock, args, wrapNonString);
                }
            }
        }

        private string HandleTextAppend(JsonElement block)
        {
            var varName = block.GetProperty("fields").GetProperty("VAR").GetString();
            var value = ExtractValue(block, "TEXT");
            return $"{varName} += str({value})";
        }

        private string HandleTextLength(JsonElement block)
        {
            var value = ExtractValue(block, "VALUE");
            return $"len({value})";
        }

        private string HandleTextIsEmpty(JsonElement block)
        {
            var value = ExtractValue(block, "VALUE");
            return $"len({value}) == 0";
        }

        private string HandleTextIndexOf(JsonElement block)
        {
            var haystack = ExtractValue(block, "VALUE");
            var needle = ExtractValue(block, "FIND");
            var end = block.GetProperty("fields").GetProperty("END").GetString();
            var method = end == "FIRST" ? "find" : "rfind";
            return $"{haystack}.{method}({needle})";
        }

        private string HandleTextCharAt(JsonElement block)
        {
            var value = ExtractValue(block, "VALUE");
            var at = ExtractValue(block, "AT");
            return $"{value}[{at}]";
        }

        private string HandleTextGetSubstring(JsonElement block)
        {
            var value = ExtractValue(block, "STRING");
            var start = ExtractValue(block, "START");
            var end = ExtractValue(block, "END");
            return $"{value}[{start}:{end}]";
        }

        private string HandleTextChangeCase(JsonElement block)
        {
            var value = ExtractValue(block, "TEXT");
            var caseOp = block.GetProperty("fields").GetProperty("CASE").GetString();
            return caseOp switch
            {
                "UPPERCASE" => $"str({value}).upper()",
                "LOWERCASE" => $"str({value}).lower()",
                "TITLECASE" => $"str({value}).title()",
                _ => $"str({value})"
            };
        }

        private string HandleTextTrim(JsonElement block)
        {
            var value = ExtractValue(block, "TEXT");
            var mode = block.GetProperty("fields").GetProperty("MODE").GetString();
            return mode switch
            {
                "BOTH" => $"str({value}).strip()",
                "LEFT" => $"str({value}).lstrip()",
                "RIGHT" => $"str({value}).rstrip()",
                _ => $"str({value})"
            };
        }

        // --- List Block Helpers ---
        private string HandleListsCreateWith(JsonElement block)
        {
            // Create a list with N items
            var items = new List<string>();
            if (block.TryGetProperty("inputs", out var inputsElem))
            {
                int i = 0;
                while (inputsElem.TryGetProperty($"ADD{i}", out var addElem))
                {
                    if (addElem.TryGetProperty("block", out var itemBlock) && itemBlock.ValueKind == JsonValueKind.Object)
                    {
                        var sb = new StringBuilder();
                        TranslateBlock(itemBlock, sb, 0);
                        items.Add(sb.ToString().Trim());
                    }
                    i++;
                }
            }
            return $"[{string.Join(", ", items)}]";
        }

        private string HandleListsRepeat(JsonElement block)
        {
            var item = ExtractValue(block, "ITEM");
            var count = ExtractValue(block, "NUM");
            return $"[{item}] * {count}";
        }

        private string HandleListsLength(JsonElement block)
        {
            var value = ExtractValue(block, "VALUE");
            return $"len({value})";
        }

        private string HandleListsIsEmpty(JsonElement block)
        {
            var value = ExtractValue(block, "VALUE");
            return $"len({value}) == 0";
        }

        private string HandleListsIndexOf(JsonElement block)
        {
            var list = ExtractValue(block, "VALUE");
            var item = ExtractValue(block, "FIND");
            var end = block.GetProperty("fields").GetProperty("END").GetString();
            var method = end == "FIRST" ? "index" : "len({list}) - 1 - {list}[::-1].index({item})";
            if (end == "FIRST")
                return $"{list}.index({item})";
            else
                return $"len({list}) - 1 - {list}[::-1].index({item})";
        }

        private string HandleListsGetIndex(JsonElement block)
        {
            var list = ExtractValue(block, "VALUE");
            var index = ExtractValue(block, "AT");
            return $"{list}[{index}]";
        }

        private string HandleListsSetIndex(JsonElement block)
        {
            var list = ExtractValue(block, "LIST");
            var index = ExtractValue(block, "AT");
            var value = ExtractValue(block, "TO");
            return $"{list}[{index}] = {value}";
        }

        private string HandleListsGetSublist(JsonElement block)
        {
            var list = ExtractValue(block, "LIST");
            var start = ExtractValue(block, "START");
            var end = ExtractValue(block, "END");
            return $"{list}[{start}:{end}]";
        }

        private string HandleListsSplit(JsonElement block)
        {
            var value = ExtractValue(block, "INPUT");
            var delimiter = ExtractValue(block, "DELIM");
            var mode = block.GetProperty("fields").GetProperty("MODE").GetString();
            if (mode == "SPLIT")
                return $"{value}.split({delimiter})";
            else
                return $"{delimiter}.join({value})";
        }

        private string HandleListsSort(JsonElement block)
        {
            var list = ExtractValue(block, "LIST");
            var type = block.GetProperty("fields").GetProperty("TYPE").GetString();
            var direction = block.GetProperty("fields").GetProperty("DIRECTION").GetString();
            var reverse = direction == "1" ? "True" : "False";
            return $"sorted({list}, reverse={reverse})";
        }
    }
}
