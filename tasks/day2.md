## 🟡 **Day 2 — Translation Engine + API Glue (4 hours)**

🧭 **Goal**: Build the translation engine and wire it to the UI
📦 Output: Blockly → Python working end-to-end

---

### 🕐 **Hour 1: Design the Blockly AST DTO + Translator Contract**

#### ✅ Tasks:

* [ ] Define a clean DTO to represent Blockly JSON:

```csharp
public class BlocklyAstDto
{
    public object Blocks { get; set; } // raw JSON AST
}
```

* [ ] (Optional, cleaner) Use a `JObject` or `JsonElement` if structure isn't fixed yet.

* [ ] Define translator contract:

```csharp
public interface ICodeTranslator
{
    string TranslateToPython(BlocklyAstDto ast);
}
```

* [ ] Implement placeholder `PythonCodeTranslator` in `Services`

---

### 🕑 **Hour 2: Build Simple Translation Engine (Python Target)**

> **Goal**: Support basic block types: `print`, `math`, `logic`, `loop`

#### ✅ Tasks:

* [ ] Implement recursive translator logic:

```csharp
public class PythonCodeTranslator : ICodeTranslator
{
    public string TranslateToPython(BlocklyAstDto ast)
    {
        // walk AST and output Python code
        // use switch on block type e.g. "print", "math_arithmetic", etc.
        // example:
        // if (block.type == "print") return $"print({innerValue})";
        return "...";
    }
}
```

* [ ] Make translation readable & maintain indentation

* [ ] Create helper methods to handle block types:

```csharp
string HandleMath(Block block) { ... }
string HandleLoop(Block block) { ... }
```

✅ **Scope only:**

* `print`
* `math_arithmetic`
* `controls_if`
* `controls_repeat_ext` or `controls_whileUntil`
* `variables_set`

---

### 🕒 **Hour 3: Wire Translation into Controller + Test in Postman**

#### ✅ Tasks:

* [ ] Inject translator into `CodeController`
* [ ] Update `/translate` endpoint:

```csharp
[Authorize]
[HttpPost("translate")]
public IActionResult Translate([FromBody] BlocklyAstDto dto)
{
    var pythonCode = _translator.TranslateToPython(dto);
    return Ok(pythonCode);
}
```

* [ ] Test in Postman:

  * Use exported Blockly JSON from Day 1
  * Send it to `/translate`
  * Confirm Python output returns

✅ **Optional**: Log block types as you traverse to help debugging

---

### 🕓 **Hour 4: Connect UI to Backend + Display Result**

#### ✅ Tasks:

* [ ] Update `sendToApi()` JS to:

  * Call `/translate` with workspace JSON
  * Parse response and display Python code

```js
function sendToApi() {
    const jwt = localStorage.getItem("token");
    const json = Blockly.serialization.workspaces.save(workspace);

    fetch("/api/translate", {
        method: "POST",
        headers: {
            "Authorization": "Bearer " + jwt,
            "Content-Type": "application/json"
        },
        body: JSON.stringify(json)
    })
    .then(res => res.text())
    .then(code => document.getElementById("codeDisplay").innerText = code);
}
```

* [ ] Basic error display in case of unauthorized or parse error

✅ **Success Criteria**:

* A user logs in
* Builds a visual program with blocks
* Clicks a button → receives generated Python code and sees it in UI

---

## ✅ DAY 2 COMPLETION CRITERIA

* [x] Blockly AST DTO created
* [x] `ICodeTranslator` interface + Python implementation
* [x] Recursively translates basic Blockly types to Python code
* [x] `/translate` endpoint connected to translator
* [x] Blockly JSON flows to backend and returns Python code
* [x] Full flow works in frontend: Blockly → Python code visible

