## 🟢 **Day 1 — Auth + Frontend Editor Setup (4 hours)**

🧭 **Goal**: Have user authentication + Blockly UI up and connected.
📦 Output: Working `POST /login` → get JWT → use Blockly → send JSON manually.

---

### 🕐 **Hour 1: Set up Authentication System (Backend)**

#### ✅ Tasks:

* [ ] Setup **ASP.NET Identity** with **Entity Framework Core** in `API`
* [ ] Create **ApplicationUser** model in `Models`
* [ ] Create `IAuthService` and `AuthService` in `Services`
* [ ] Implement:

  * `POST /register`: create user
  * `POST /login`: authenticate user and issue JWT
* [ ] Configure **JWT Authentication** in `Program.cs`:

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

* [ ] Create `AuthController`:

  * `/register`
  * `/login`
* [ ] Return **JWT Token** with expiry on success
* [ ] Protect sample endpoint `/translate` with `[Authorize]`
* [ ] Manual test with Postman:

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

* [ ] Create new view: `BlocklyEditor.cshtml`
* [ ] Add Blockly CDN:

```html
<script src="https://unpkg.com/blockly/blockly.min.js"></script>
```

* [ ] Add basic layout:

```html
<div id="blocklyDiv" style="height: 400px; width: 100%;"></div>
<pre id="codeDisplay"></pre>
<button onclick="exportJson()">Export</button>
```

* [ ] Add JS:

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

* [ ] Add simple login form in Razor or popup modal
* [ ] On login:

  * Fetch JWT → save in `localStorage`
* [ ] Add button to send Blockly JSON to `/translate`:

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

