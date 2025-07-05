
## 🧱 **CodeCanvas: Visual Programming with .NET API Backend**

### 🎯 **Goal**

Build a system where users write code using **visual blocks** (like Scratch), then send the block data to a **.NET backend API**, which converts it into real code (e.g., Python or JavaScript). The system includes **authentication** to secure access to the API.

---

## 🔧 **Tech Stack**

|Layer|Technology|Purpose|
|---|---|---|
|Frontend View|**ASP.NET MVC (Razor)**|UI hosting Blockly and controlling user interaction|
|Visual Editor|**Google Blockly (JS lib)**|Drag-and-drop coding blocks|
|API Backend|**ASP.NET Web API**|Receives Blockly JSON, converts it to code, includes authentication|
|Authentication|**JWT Bearer Auth (ASP.NET Identity)**|Secure the API endpoints|
|Optional|**Code Execution Engine**|To run generated code and return output (Python, JS sandbox, etc.)|

---

## 🔁 **System Flow**

1. User logs in (or registers) using a basic account system.
    
2. Upon login, a **JWT token** is issued.
    
3. User interacts with the Blockly interface in the browser.
    
4. Blockly generates a **JSON AST** representing the blocks.
    
5. The frontend sends this JSON to the API via `POST` (with the JWT token).
    
6. The API processes the JSON and **translates it to real code** (e.g., Python).
    
7. (Optional) The API executes the code and returns the result.
    
8. Frontend displays the **generated code** and/or **execution result**.
    

---

## 🔐 **Authentication Layer (Summary)**

- Use **ASP.NET Identity** with JWT authentication.
    
- Add `[Authorize]` attributes to protect the translation and execution endpoints.
    
- Public endpoints:
    
    - `POST /register`
        
    - `POST /login`
        
- Protected endpoints:
    
    - `POST /translate`
        
    - `POST /execute`
        

---

## 🔧 **Blockly Integration**

- Blockly is a **JavaScript library** for visual programming.
    
- Include it in Razor Views using a CDN or local JS files.
    
- Generate a **JSON AST** (Blockly's workspace model) to send to the API.
    

### Sample HTML

```html
<script src="https://unpkg.com/blockly/blockly.min.js"></script>
<div id="blocklyDiv" style="height: 400px; width: 600px;"></div>
```

### JavaScript to get JSON

```js
var workspace = Blockly.inject('blocklyDiv', { toolbox: toolbox });

function getBlocklyJson() {
    var json = Blockly.serialization.workspaces.save(workspace);
    fetch('/api/translate', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + yourJwtToken
        },
        body: JSON.stringify(json)
    })
    .then(response => response.text())
    .then(code => {
        document.getElementById("codeDisplay").innerText = code;
    });
}
```

---

## 🧱 **Initial Custom Block Set**

Start with these simple blocks:

- `print`
    
- `variables`
    
- `math` (e.g., add, subtract)
    
- `logic` (if / if-else)
    
- `loops` (repeat, while)
    

This makes the Blockly JSON simple enough to process and keeps your translation logic manageable.

---

## 📦 **Backend (ASP.NET Web API)**

### Auth Endpoints:

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register(UserDto dto) { /* Register user */ }

[HttpPost("login")]
public async Task<IActionResult> Login(LoginDto dto) { /* Return JWT */ }
```

### Protected Translation Endpoint:

```csharp
[Authorize]
[HttpPost("translate")]
public IActionResult Translate([FromBody] BlocklyAstDto input)
{
    var code = YourCodeTranslator.ConvertToPython(input);
    return Ok(code);
}
```

---

## 📄 **Sample JWT Setup (Startup.cs / Program.cs)**

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            // configure token validation...
        };
    });

app.UseAuthentication();
app.UseAuthorization();
```

---

## 🧠 **Why This Project Is Valuable**

- Demonstrates API architecture and security (JWT auth).
    
- Shows frontend–backend interaction via JSON and HTTP.
    
- Real-world logic: translating structured input (Blockly) into real code.
    
- Teachable, scalable, and extensible — future support for more blocks, users, and output features.
    
- Focuses on the **backend**, which aligns with .NET API course goals.