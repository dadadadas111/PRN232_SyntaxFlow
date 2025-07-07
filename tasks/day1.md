## 🟢 **Day 1 — Auth + Frontend Editor Setup (4 hours)**

🧭 **Goal**: Have user authentication + Blockly UI up and connected.
📦 Output: Working `POST /login` → get JWT → use Blockly → send JSON manually.

---

### 🕐 **Hour 1: Set up Authentication System (Backend)**

#### ✅ Tasks:

* [x] Setup **ASP.NET Identity** with **Entity Framework Core** in `API`
* [x] Create **ApplicationUser** model in `Models`
* [x] Create `IAuthService` and `AuthService` in `Services`
* [x] Implement:

  * [x] `POST /register`: create user
  * [x] `POST /login`: authenticate user and issue JWT
* [x] Configure **JWT Authentication** in `Program.cs`:

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "SyntaxFlowAPI",
            ValidAudience = "SyntaxFlowFrontend",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-strong-jwt-secret"))
        };
    });
```

---

### 🕑 **Hour 2: Build AuthController + Test Auth**

#### ✅ Tasks:

* [x] Create `AuthController`:

  * [x] `/register`
  * [x] `/login`
* [x] Return **JWT Token** with expiry on success
* [x] Protect sample endpoint `/translate` with `[Authorize]`
* [x] Manual test with Postman:

  * Register user
  * Login → get JWT
  * Call `/translate` with JWT in `Authorization: Bearer <token>`

✅ **Success Criteria**:

* You can get a JWT token via login
* JWT is valid for calling protected API
* API rejects requests without token

---

### 🕒 **Hour 3: Embed Blockly UI in Razor View (Frontend)**

#### ✅ Tasks:

* [x] Create new view: `BlocklyEditor.cshtml`
* [x] Add Blockly CDN:

```html
<script src="https://unpkg.com/blockly/blockly.min.js"></script>
```

* [x] Add basic layout:

```html
<div id="blocklyDiv" style="height: 400px; width: 100%;"></div>
<pre id="codeDisplay"></pre>
<button onclick="exportJson()">Export</button>
```

* [x] Add JS:

```js
const workspace = Blockly.inject('blocklyDiv', {toolbox: '<xml>...</xml>'});

function exportJson() {
    const json = Blockly.serialization.workspaces.save(workspace);
    console.log(JSON.stringify(json));
}
```

✅ **Success Criteria**:

* Blockly loads visually
* Drag/drop blocks work
* Clicking button shows block JSON in console

---

### 🕓 **Hour 4: Glue UI + Token Storage + Mock API POST**

#### ✅ Tasks:

* [x] Add simple login form in Razor or popup modal
* [x] On login:
  * Fetch JWT → save in `localStorage`
* [x] Add button to send Blockly JSON to `/translate`:

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

* [x] Add UI/UX for JWT status, error handling, and logout
* [x] Display API result and errors clearly

#### 🔒 Security Tip:

* Set short JWT expiry (e.g. 30 mins)
* Use HTTPS even in dev with self-signed cert

✅ **Success Criteria**:

* User can login
* JWT saved & used in `Authorization` header
* Blockly sends JSON to protected `/translate` endpoint (mock response is OK for now)

---

## ✅ DAY 1 COMPLETION CRITERIA

* [x] JWT Auth system built and tested
* [x] User login/register via UI
* [x] Blockly visual editor loads with working blocks
* [x] Blockly JSON can be manually exported
* [x] JSON sent to API with JWT token attached
* [x] Frontend–backend communication tested with mock response

