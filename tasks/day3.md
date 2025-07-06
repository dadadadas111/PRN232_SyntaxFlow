## 🔵 **Day 3 — Execution Engine, Workspace Persistence, and Polish (4 hours)**

🧭 **Goal**: Add optional execution, workspace save/load, refine UI, and prep for deployment
📦 Output: Feature-complete MVP – Blockly → Python → output, workspaces persist, UI refined

---

### 🕐 **Hour 1: Add (Optional) Code Execution Engine**

> ⚠️ Local code execution can be risky — this implementation is **only for trusted environments**.

#### ✅ Tasks:

* [ ] In `Services`, create:

```csharp
public interface ICodeExecutionService
{
    Task<string> ExecutePythonAsync(string code);
}
```

* [ ] Use `System.Diagnostics.Process`:

```csharp
public async Task<string> ExecutePythonAsync(string code)
{
    var tempFile = Path.GetTempFileName() + ".py";
    await File.WriteAllTextAsync(tempFile, code);

    var psi = new ProcessStartInfo
    {
        FileName = "python", // or "python3"
        Arguments = tempFile,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(psi);
    var output = await process.StandardOutput.ReadToEndAsync();
    var error = await process.StandardError.ReadToEndAsync();

    return string.IsNullOrWhiteSpace(error) ? output : $"Error:\n{error}";
}
```

* [ ] Create `/execute` endpoint in `CodeController`:

```csharp
[Authorize]
[HttpPost("execute")]
public async Task<IActionResult> Execute([FromBody] BlocklyAstDto dto)
{
    var code = _translator.TranslateToPython(dto);
    var output = await _executor.ExecutePythonAsync(code);
    return Ok(output);
}
```

✅ **Success Criteria**: Full Blockly → Python → output → display in UI

---

### 🕑 **Hour 2: Add Workspace Save/Load Feature**

> Store user workspaces as JSON in memory or DB (depending on scope)

#### ✅ Tasks:

* [ ] Add `WorkspaceDto`:

```csharp
public class WorkspaceDto
{
    public string Name { get; set; }
    public string Json { get; set; }
}
```

* [ ] Create `IWorkspaceService` and store in-memory (or SQLite via EF Core)
* [ ] Create endpoints:

```csharp
[Authorize]
[HttpPost("workspace/save")]
public async Task<IActionResult> Save(WorkspaceDto dto) { ... }

[Authorize]
[HttpGet("workspace/load/{name}")]
public async Task<IActionResult> Load(string name) { ... }

[Authorize]
[HttpGet("workspace/list")]
public async Task<IActionResult> List() { ... }
```

* [ ] Add JS UI for:

  * Saving current workspace
  * Loading a named workspace into Blockly

```js
Blockly.serialization.workspaces.load(json, workspace);
```

✅ **Success Criteria**: User can save and reload Blockly workspaces by name

---

### 🕒 **Hour 3: UI Cleanup & Error Handling**

#### ✅ Tasks:

* [ ] Show:

  * Python code output
  * Execution result output
  * Errors (invalid JWT, translation failure, etc.)

* [ ] Add loading spinner during requests

* [ ] Add basic session-based UI:

  * Show login only if not logged in
  * Show Blockly + buttons only after login
  * Logout button clears localStorage

* [ ] Frontend page structure:

```html
- Login form
- Blockly editor
- Buttons:
  - Export / Translate
  - Execute
  - Save workspace
  - Load workspace
- Code + Output display sections
```

✅ **Success Criteria**:

* Smooth user flow from login → Blockly → translate/execute → save/load workspace
* Errors and loading states are visible

---

### 🕓 **Hour 4: Deployment Readiness + Docs**

#### ✅ Tasks:

* [ ] Update `appsettings.json`:

  * JWT secret
  * ValidIssuer / ValidAudience
* [ ] Add CORS config for local → API

```csharp
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://localhost:PORT")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
```

* [ ] Prepare README:

  * Setup instructions
  * Tech stack overview
  * Feature list
  * Dev & run instructions
* [ ] Optional: Dockerize `API`

---

## ✅ DAY 3 COMPLETION CRITERIA

* [x] Code execution engine works (or mocked)
* [x] Workspace save/load APIs complete and functional
* [x] UI supports execution + storage
* [x] Error handling + UI refinement done
* [x] Project is deploy-ready with clear documentation

---

## 🎯 Final Product Summary (After Day 3)

* Users can **log in** and use a **Blockly editor**
* Blockly blocks are **translated to Python**
* Code can be **executed**, and output shown
* Workspaces can be **saved and restored**
* Frontend is functional and secured with JWT
* Code is modular and clean (services, repositories, DTOs, controllers)
