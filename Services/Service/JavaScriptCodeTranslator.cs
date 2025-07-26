using System;
using System.Text;
using System.Text.Json;
using Models;
using Services.Interface;

namespace Services.Service
{
    public class JavaScriptCodeTranslator : ICodeTranslator
    {
        private Dictionary<string, string> _variableIdToName = new();
        private HashSet<string> usedHelpers = new HashSet<string>();

        public string Translate(BlocklyAstDto ast)
        {
            _variableIdToName.Clear();
            usedHelpers.Clear();
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
                return "// Invalid or empty AST";

            var sb = new StringBuilder();
            if (ast.Blocks.TryGetProperty("blocks", out var blocksElem))
            {
                if (blocksElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var block in blocksElem.EnumerateArray())
                    {
                        TranslateBlock(block, sb, 0);
                    }
                }
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
            header.AppendLine("// Translated code by SyntaxFlow Blockly-to-JavaScript engine");
            header.AppendLine("// Helper methods are auto-included as needed for block compatibility\n");

            // After translation, prepend helpers if any
            if (usedHelpers.Contains("isPrime"))
            {
                helpers.AppendLine("function isPrime(n) {");
                helpers.AppendLine("    if (n <= 1) return false;");
                helpers.AppendLine("    if (n === 2) return true;");
                helpers.AppendLine("    if (n % 2 === 0) return false;");
                helpers.AppendLine("    for (let i = 3; i <= Math.sqrt(n); i += 2) {");
                helpers.AppendLine("        if (n % i === 0) return false;");
                helpers.AppendLine("    }");
                helpers.AppendLine("    return true;");
                helpers.AppendLine("}\n");
                helpers.AppendLine("// --- End of helpers, user code below ---\n");
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
                    sb.AppendLine(Indent(indent) + HandleMath(block) + ";");
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
                    sb.Append($"!{ExtractValue(block, "BOOL")}");
                    break;
                case "logic_boolean":
                    var boolVal = block.GetProperty("fields").GetProperty("BOOL").GetString();
                    sb.Append(boolVal == "TRUE" ? "true" : "false");
                    break;
                case "logic_null":
                    sb.Append("null");
                    break;
                case "logic_ternary":
                    var cond = ExtractValue(block, "IF");
                    var thenVal = ExtractValue(block, "THEN");
                    var elseVal = ExtractValue(block, "ELSE");
                    sb.Append($"({cond} ? {thenVal} : {elseVal})");
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
                    sb.AppendLine(Indent(indent) + (flow == "BREAK" ? "break;" : "continue;"));
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
                    sb.AppendLine(Indent(indent) + $"// Unsupported block: {type}");
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
            sb.AppendLine(Indent(indent) + $"console.log({value});");
        }

        private string HandleMath(JsonElement block)
        {
            var op = block.GetProperty("fields").GetProperty("OP").GetString();
            var left = ExtractValue(block, "A");
            var right = ExtractValue(block, "B");
            var jsOp = op switch
            {
                "ADD" => "+",
                "MINUS" => "-",
                "MULTIPLY" => "*",
                "DIVIDE" => "/",
                "POWER" => "**",
                _ => op
            };
            return $"{left} {jsOp} {right}";
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
                string keyword = n == 0 ? "if" : "else if";
                sb.AppendLine(Indent(indent) + $"{keyword} ({cond}) {{");
                if (inputsElem.TryGetProperty(doKey, out var doElem))
                {
                    if (doElem.TryGetProperty("block", out var doBlock) && doBlock.ValueKind == JsonValueKind.Object)
                    {
                        TranslateBlock(doBlock, sb, indent + 1);
                    }
                }
                sb.AppendLine(Indent(indent) + "}");
                n++;
            }
            // Handle ELSE branch if present
            if (block.TryGetProperty("inputs", out var inputsElem2) && inputsElem2.TryGetProperty("ELSE", out var elseElem))
            {
                sb.AppendLine(Indent(indent) + "else {");
                if (elseElem.TryGetProperty("block", out var elseBlock) && elseBlock.ValueKind == JsonValueKind.Object)
                {
                    TranslateBlock(elseBlock, sb, indent + 1);
                }
                sb.AppendLine(Indent(indent) + "}");
            }
        }

        private void HandleRepeat(JsonElement block, StringBuilder sb, int indent)
        {
            var times = ExtractValue(block, "TIMES");
            sb.AppendLine(Indent(indent) + $"for (let i = 0; i < {times}; i++) {{");
            if (block.TryGetProperty("inputs", out var inputsElem) && inputsElem.TryGetProperty("DO", out var doElem))
            {
                if (doElem.TryGetProperty("block", out var doBlock) && doBlock.ValueKind == JsonValueKind.Object)
                {
                    TranslateBlock(doBlock, sb, indent + 1);
                }
            }
            sb.AppendLine(Indent(indent) + "}");
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
            sb.AppendLine(Indent(indent) + $"let {varName} = {value};");
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
            sb.AppendLine(Indent(indent) + $"{varName} += {delta};");
        }

        private void HandleWhileUntil(JsonElement block, StringBuilder sb, int indent)
        {
            var mode = block.GetProperty("fields").GetProperty("MODE").GetString();
            var cond = ExtractValue(block, "BOOL");
            var jsCond = mode == "UNTIL" ? $"!({cond})" : cond;
            sb.AppendLine(Indent(indent) + $"while ({jsCond}) {{");
            if (block.TryGetProperty("inputs", out var inputsElem) && inputsElem.TryGetProperty("DO", out var doElem))
            {
                if (doElem.TryGetProperty("block", out var doBlock) && doBlock.ValueKind == JsonValueKind.Object)
                {
                    TranslateBlock(doBlock, sb, indent + 1);
                }
            }
            sb.AppendLine(Indent(indent) + "}");
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
            sb.AppendLine(Indent(indent) + $"for (let {varName} = {from}; {varName} <= {to}; {varName} += {by}) {{");
            if (block.TryGetProperty("inputs", out var inputsElem) && inputsElem.TryGetProperty("DO", out var doElem))
            {
                if (doElem.TryGetProperty("block", out var doBlock) && doBlock.ValueKind == JsonValueKind.Object)
                {
                    TranslateBlock(doBlock, sb, indent + 1);
                }
            }
            sb.AppendLine(Indent(indent) + "}");
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
            sb.AppendLine(Indent(indent) + $"for (let {varName} of {list}) {{");
            if (block.TryGetProperty("inputs", out var inputsElem) && inputsElem.TryGetProperty("DO", out var doElem))
            {
                if (doElem.TryGetProperty("block", out var doBlock) && doBlock.ValueKind == JsonValueKind.Object)
                {
                    TranslateBlock(doBlock, sb, indent + 1);
                }
            }
            sb.AppendLine(Indent(indent) + "}");
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
                        return fieldElem.GetRawText();
                    case JsonValueKind.True:
                        return "true";
                    case JsonValueKind.False:
                        return "false";
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
            var jsOp = op switch
            {
                "EQ" => "===",
                "NEQ" => "!==",
                "LT" => "<",
                "LTE" => "<=",
                "GT" => ">",
                "GTE" => ">=",
                _ => op
            };
            return $"{a} {jsOp} {b}";
        }

        private string HandleLogicOperation(JsonElement block)
        {
            var op = block.GetProperty("fields").GetProperty("OP").GetString();
            var a = ExtractValue(block, "A");
            var b = ExtractValue(block, "B");
            var jsOp = op switch
            {
                "AND" => "&&",
                "OR" => "||",
                _ => op
            };
            return $"({a} {jsOp} {b})";
        }

        private string HandleMathSingle(JsonElement block)
        {
            var op = block.GetProperty("fields").GetProperty("OP").GetString();
            var num = ExtractValue(block, "NUM");
            return op switch
            {
                "ROOT" => $"Math.sqrt({num})",
                "ABS" => $"Math.abs({num})",
                "NEG" => $"-({num})",
                "LN" => $"Math.log({num})",
                "LOG10" => $"Math.log10({num})",
                "EXP" => $"Math.exp({num})",
                "POW10" => $"Math.pow(10, {num})",
                _ => $"// Unsupported math_single op: {op}"
            };
        }

        private string HandleMathTrig(JsonElement block)
        {
            var op = block.GetProperty("fields").GetProperty("OP").GetString();
            var angle = ExtractValue(block, "NUM");
            return op switch
            {
                "SIN" => $"Math.sin(({angle}) * Math.PI / 180)",
                "COS" => $"Math.cos(({angle}) * Math.PI / 180)",
                "TAN" => $"Math.tan(({angle}) * Math.PI / 180)",
                "ASIN" => $"Math.asin({angle}) * 180 / Math.PI",
                "ACOS" => $"Math.acos({angle}) * 180 / Math.PI",
                "ATAN" => $"Math.atan({angle}) * 180 / Math.PI",
                _ => $"// Unsupported math_trig op: {op}"
            };
        }

        private string HandleMathConstant(JsonElement block)
        {
            var constant = block.GetProperty("fields").GetProperty("CONSTANT").GetString();
            return constant switch
            {
                "PI" => "Math.PI",
                "E" => "Math.E",
                "GOLDEN_RATIO" => "(1 + Math.sqrt(5)) / 2",
                "SQRT2" => "Math.SQRT2",
                "SQRT1_2" => "Math.SQRT1_2",
                "INFINITY" => "Infinity",
                _ => $"// Unsupported math_constant: {constant}"
            };
        }

        private string HandleMathNumberProperty(JsonElement block)
        {
            var prop = block.GetProperty("fields").GetProperty("PROPERTY").GetString();
            var num = ExtractValue(block, "NUMBER_TO_CHECK");
            switch (prop)
            {
                case "EVEN": return $"({num}) % 2 === 0";
                case "ODD": return $"({num}) % 2 === 1";
                case "PRIME":
                    usedHelpers?.Add("isPrime");
                    return $"isPrime({num})";
                case "WHOLE": return $"({num}) % 1 === 0";
                case "POSITIVE": return $"({num}) > 0";
                case "NEGATIVE": return $"({num}) < 0";
                case "DIVISIBLE_BY": return $"({num}) % {ExtractValue(block, "DIVISOR")} === 0";
                default: return $"// Unsupported math_number_property: {prop}";
            }
        }

        private string HandleMathRound(JsonElement block)
        {
            var op = block.GetProperty("fields").GetProperty("OP").GetString();
            var num = ExtractValue(block, "NUM");
            return op switch
            {
                "ROUND" => $"Math.round({num})",
                "ROUNDUP" => $"Math.ceil({num})",
                "ROUNDDOWN" => $"Math.floor({num})",
                _ => $"// Unsupported math_round op: {op}"
            };
        }

        private string HandleMathOnList(JsonElement block) => "// Unsupported: math_on_list";

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
            return $"Math.min(Math.max({value}, {low}), {high})";
        }

        private string HandleMathRandomInt(JsonElement block)
        {
            var from = ExtractValue(block, "FROM");
            var to = ExtractValue(block, "TO");
            return $"Math.floor(Math.random() * ({to} - {from} + 1)) + {from}";
        }

        private string HandleMathRandomFloat(JsonElement block)
        {
            return "Math.random()";
        }

        // --- Text Block Helpers ---
        private string HandleTextJoin(JsonElement block)
        {
            var args = new List<string>();
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
                        // Convert to string if not already a string literal
                        if (!(arg.StartsWith("\"") && arg.EndsWith("\"")))
                            args.Add($"String({arg})");
                        else
                            args.Add(arg);
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

        private void FlattenTextJoinItems(JsonElement block, List<string> args, bool wrapNonString)
        {
            if (block.GetProperty("type").GetString() == "text")
            {
                var textValue = block.GetProperty("fields").GetProperty("TEXT").GetString() ?? "";
                args.Add('"' + textValue.Replace("\"", "\\\"") + '"');
            }
            else if (block.GetProperty("type").GetString() == "text_join")
            {
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
                    args.Add($"String({val})");
                else
                    args.Add(val);
            }
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
            return $"{varName} += String({value});";
        }

        private string HandleTextLength(JsonElement block)
        {
            var value = ExtractValue(block, "VALUE");
            return $"{value}.length";
        }

        private string HandleTextIsEmpty(JsonElement block)
        {
            var value = ExtractValue(block, "VALUE");
            return $"{value}.length === 0";
        }

        private string HandleTextIndexOf(JsonElement block)
        {
            var haystack = ExtractValue(block, "VALUE");
            var needle = ExtractValue(block, "FIND");
            var end = block.GetProperty("fields").GetProperty("END").GetString();
            var method = end == "FIRST" ? "indexOf" : "lastIndexOf";
            return $"{haystack}.{method}({needle})";
        }

        private string HandleTextCharAt(JsonElement block)
        {
            var value = ExtractValue(block, "VALUE");
            var at = ExtractValue(block, "AT");
            return $"{value}.charAt({at})";
        }

        private string HandleTextGetSubstring(JsonElement block)
        {
            var value = ExtractValue(block, "STRING");
            var start = ExtractValue(block, "START");
            var end = ExtractValue(block, "END");
            return $"{value}.substring({start}, {end})";
        }

        private string HandleTextChangeCase(JsonElement block)
        {
            var value = ExtractValue(block, "TEXT");
            var caseOp = block.GetProperty("fields").GetProperty("CASE").GetString();
            return caseOp switch
            {
                "UPPERCASE" => $"String({value}).toUpperCase()",
                "LOWERCASE" => $"String({value}).toLowerCase()",
                "TITLECASE" => $"String({value}).replace(/\\w\\S*/g, (txt) => txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase())",
                _ => $"String({value})"
            };
        }

        private string HandleTextTrim(JsonElement block)
        {
            var value = ExtractValue(block, "TEXT");
            var mode = block.GetProperty("fields").GetProperty("MODE").GetString();
            return mode switch
            {
                "BOTH" => $"String({value}).trim()",
                "LEFT" => $"String({value}).trimStart()",
                "RIGHT" => $"String({value}).trimEnd()",
                _ => $"String({value})"
            };
        }

        // --- List Block Helpers ---
        private string HandleListsCreateWith(JsonElement block)
        {
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
            return $"Array({count}).fill({item})";
        }

        private string HandleListsLength(JsonElement block)
        {
            var value = ExtractValue(block, "VALUE");
            return $"{value}.length";
        }

        private string HandleListsIsEmpty(JsonElement block)
        {
            var value = ExtractValue(block, "VALUE");
            return $"{value}.length === 0";
        }

        private string HandleListsIndexOf(JsonElement block)
        {
            var list = ExtractValue(block, "VALUE");
            var item = ExtractValue(block, "FIND");
            var end = block.GetProperty("fields").GetProperty("END").GetString();
            var method = end == "FIRST" ? "indexOf" : "lastIndexOf";
            return $"{list}.{method}({item})";
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
            return $"{list}[{index}] = {value};";
        }

        private string HandleListsGetSublist(JsonElement block)
        {
            var list = ExtractValue(block, "LIST");
            var start = ExtractValue(block, "START");
            var end = ExtractValue(block, "END");
            return $"{list}.slice({start}, {end})";
        }

        private string HandleListsSplit(JsonElement block)
        {
            var value = ExtractValue(block, "INPUT");
            var delimiter = ExtractValue(block, "DELIM");
            var mode = block.GetProperty("fields").GetProperty("MODE").GetString();
            if (mode == "SPLIT")
                return $"{value}.split({delimiter})";
            else
                return $"{value}.join({delimiter})";
        }

        private string HandleListsSort(JsonElement block)
        {
            var list = ExtractValue(block, "LIST");
            var type = block.GetProperty("fields").GetProperty("TYPE").GetString();
            var direction = block.GetProperty("fields").GetProperty("DIRECTION").GetString();
            var sortCall = direction == "1" ? $"[...{list}].sort().reverse()" : $"[...{list}].sort()";
            return sortCall;
        }
    }
}
