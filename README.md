# GreenOpportunitie
 production-grade RESTful API built with **ASP.NET Core 9 Web API** to manage opportunities (scholarships, trainings, competitions, internships…) for the **فرص خضراء** (Green Opportunities) platform.

> Backend technical assessment — clean architecture, EF Core, JWT, Swagger.

---

## ✨ Features

- **Full CRUD** on opportunities: `GET / POST / PUT / DELETE`
- **Pagination** (`page`, `pageSize`) on `GET /api/opportunities`
- **Keyword search** across `Title` / `Description`
- **Filtering** by `Type`, `Country`, `IsFullyFunded`
- **JWT authentication** (BCrypt password hashing)
- **Role-based authorization**: `Admin` or `Editor` required for `POST / PUT / DELETE`
- **Public read** access: anyone can browse, only authenticated editors can mutate
- **Swagger / OpenAPI** with a built-in "Authorize" button for Bearer tokens
- **EF Core Migrations** (SQLite, zero configuration)
- **Automatic seed** at startup: 5 demo opportunities + a default admin account

---

## 🏗️ Architecture (Clean / Layered)

```
GreenOpportunities/
├── README.md
├── .gitignore
└── src/
    └── GreenOpportunities.API/
        ├── Controllers/        ← AuthController, OpportunitiesController
        ├── Services/           ← Business logic (OpportunityService, AuthService)
        ├── Repositories/       ← Data access (OpportunityRepository, UserRepository)
        ├── Models/             ← Domain entities (Opportunity, ApplicationUser)
        ├── DTOs/               ← Request / Response shapes
        ├── Data/               ← AppDbContext + EF Core Migrations
        ├── Helpers/            ← JwtTokenGenerator
        ├── Program.cs          ← Composition root + HTTP pipeline
        └── appsettings.json    ← Configuration (DB connection, JWT, logs)
```

**Request flow**: `Controller` → `Service` → `Repository` → `DbContext` (EF Core) → SQLite

---

## 🛠️ Tech Stack

| Concern         | Technology                                |
|-----------------|-------------------------------------------|
| Language        | C# 12 (.NET 9)                            |
| Framework       | ASP.NET Core 9 Web API                    |
| ORM             | Entity Framework Core 9 (SQLite)          |
| Auth            | JWT Bearer + BCrypt.Net-Next              |
| API Docs        | Swashbuckle / Swagger UI                  |
| Serialization   | System.Text.Json (cycle references ignored) |

---

## 🚀 Getting Started

### Prerequisites

- **.NET SDK 9.0+** — [download here](https://dotnet.microsoft.com/download)
- (Optional) **dotnet-ef** tool for migrations:
  ```bash
  dotnet tool install -g dotnet-ef --version 9.0.0
  ```

### Run in 3 steps

```bash
# 1. Unzip and enter the project
unzip GreenOpportunities.zip
cd GreenOpportunities/src/GreenOpportunities.API

# 2. Restore dependencies
dotnet restore

# 3. Run the API in development mode
dotnet run
```

The application will start on:
- ➜ **HTTP**:   `http://localhost:5189`
- ➜ **Swagger UI**: `http://localhost:5189/swagger`
- ➜ `/` redirects to `/swagger`

On first run the app automatically:
1. Creates `greenopportunities.db` (SQLite)
2. Applies all EF Core migrations
3. Seeds **5 demo opportunities** + a default admin (`admin` / `Admin@123`)

> **Note**: the JWT key shipped in `appsettings.Development.json` is for **development only**. For production, set a strong key (≥ 32 characters) via the `Jwt__Secret` environment variable.

### Production environment variables

```bash
export ConnectionStrings__DefaultConnection="Data Source=/var/data/greenopportunities.db"
export Jwt__Secret="your-strong-random-secret-of-at-least-32-characters"
export Jwt__Issuer="GreenOpportunitiesAPI"
export Jwt__Audience="GreenOpportunitiesClient"
export Jwt__ExpirationInMinutes="1440"
```

---

## 🔐 Default admin account (seeded)

| Field      | Value                          |
|------------|--------------------------------|
| Username   | `admin`                        |
| Password   | `Admin@123`                    |
| Role       | `Admin`                        |

> ⚠️ Change this in production.

---

## 📚 API Endpoints

### Auth — `/api/auth`

| Method | Route                  | Auth | Description                                  |
|--------|------------------------|------|----------------------------------------------|
| POST   | `/api/auth/register`   | ❌   | Create a new account (Admin role by default) |
| POST   | `/api/auth/login`      | ❌   | Returns a JWT if credentials are valid       |

### Opportunities — `/api/opportunities`

| Method | Route                          | Auth     | Description                                            |
|--------|--------------------------------|----------|--------------------------------------------------------|
| GET    | `/api/opportunities`           | ❌       | Paginated list with filters (see below)                |
| GET    | `/api/opportunities/{id}`      | ❌       | Get one opportunity by id                              |
| POST   | `/api/opportunities`           | ✅ JWT   | Create a new opportunity (Admin/Editor role required)  |
| PUT    | `/api/opportunities/{id}`      | ✅ JWT   | Update an opportunity (Admin/Editor role required)    |
| DELETE | `/api/opportunities/{id}`      | ✅ JWT   | Delete an opportunity (Admin/Editor role required)    |

#### Query parameters for `GET /api/opportunities`

| Parameter       | Type   | Default | Description                                                    |
|-----------------|--------|---------|----------------------------------------------------------------|
| `page`          | int    | 1       | Page number                                                    |
| `pageSize`      | int    | 10      | Items per page (max 100)                                       |
| `keyword`       | string | —       | Full-text search across `Title` / `Description` (case-insensitive) |
| `type`          | string | —       | Exact match on `Type` (e.g. `Scholarship`, `Training`)         |
| `country`       | string | —       | `contains` match on `Country` (case-insensitive)               |
| `isFullyFunded` | bool   | —       | `true` / `false`                                               |

#### Example request

```http
GET /api/opportunities?keyword=Maroc&isFullyFunded=true&page=1&pageSize=10
Authorization: Bearer <jwt>
```

#### HTTP status codes

| Code | Meaning                                                       |
|------|---------------------------------------------------------------|
| 200  | OK                                                            |
| 201  | Created (`POST`)                                              |
| 204  | No Content (`DELETE`)                                         |
| 400  | Validation error (DataAnnotations)                            |
| 401  | JWT missing or invalid                                        |
| 404  | Opportunity not found                                         |
| 409  | Conflict (duplicate username/email on registration)           |

---

## 🧪 Examples with `curl`

### 1. Login

```bash
curl -X POST http://localhost:5189/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'

# Response:
# { "token": "eyJhbGciOi...", "username": "admin", "role": "Admin", ... }
```

### 2. Public list with filters

```bash
curl "http://localhost:5189/api/opportunities?keyword=Maroc&isFullyFunded=true&page=1&pageSize=10"
```

### 3. Create (with JWT)

```bash
TOKEN="<paste-token-from-step-1>"

curl -X POST http://localhost:5189/api/opportunities \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "title":"Eiffel Scholarship 2026",
    "description":"Eiffel Programme for Masters in France",
    "type":"Scholarship",
    "country":"France",
    "deadline":"2027-01-15T00:00:00Z",
    "isFullyFunded":true
  }'
```

### 4. Update (PUT)

```bash
curl -X PUT http://localhost:5189/api/opportunities/1 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "title":"Chevening — Updated",
    "description":"Updated description",
    "type":"Scholarship",
    "country":"UK",
    "deadline":"2026-12-01T00:00:00Z",
    "isFullyFunded":true
  }'
```

### 5. Delete

```bash
curl -X DELETE http://localhost:5189/api/opportunities/2 \
  -H "Authorization: Bearer $TOKEN"
```

---

## 🧱 Data Model

### `Opportunity`

| Field            | Type       | Description                                |
|------------------|------------|--------------------------------------------|
| `Id`             | int        | Unique identifier (PK, auto-incremented)   |
| `Title`          | string     | Title (3–200 chars, required)              |
| `Description`    | string     | Description (up to 4000 chars)             |
| `Type`           | string     | Scholarship, Training, Competition, etc.   |
| `Country`        | string     | Country or comma-separated list of countries |
| `Deadline`       | DateTime   | Application deadline                       |
| `IsFullyFunded`  | bool       | Fully funded?                              |
| `CreatedAt`      | DateTime   | Date added (UTC, auto-set)                 |

### `ApplicationUser`

| Field          | Type       | Description                                   |
|----------------|------------|-----------------------------------------------|
| `Id`           | int        | Unique identifier                            |
| `Username`     | string     | Username (unique)                             |
| `Email`        | string     | Email (unique)                                |
| `PasswordHash` | string     | BCrypt hash                                   |
| `Role`         | string     | `Admin` or `Editor`                           |
| `CreatedAt`    | DateTime   | Account creation date                         |

---

## 🗄️ Database

- **SQLite** — file `greenopportunities.db` is created on first run
- **EF Core Migrations** live under `Data/Migrations/`
- **Auto-seed** at startup (5 opportunities + 1 admin)

To reset the database completely:

```bash
rm greenopportunities.db greenopportunities.db-wal greenopportunities.db-shm
dotnet run
```

---

## ✅ Spec compliance

| Requirement                                             | Status |
|---------------------------------------------------------|--------|
| `Opportunity` model with 8 fields                       | ✅     |
| CRUD endpoints (list, detail, POST, PUT, DELETE)         | ✅     |
| Pagination                                              | ✅     |
| Keyword search (title / description)                    | ✅     |
| Filtering by Type / Country / IsFullyFunded             | ✅     |
| EF Core + SQLite + Migrations                           | ✅     |
| JWT authentication                                      | ✅     |
| Public read, protected write                            | ✅     |
| Layered architecture (Controllers / Services / Repos)   | ✅     |
| Correct HTTP codes (200/201/204/400/401/404/409)        | ✅     |
| Swagger / OpenAPI                                       | ✅     |

---

## 📦 Repository layout

```
GreenOpportunities/
├── README.md
├── .gitignore
└── src/
    └── GreenOpportunities.API/
        ├── GreenOpportunities.API.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── Controllers/
        ├── Services/
        ├── Repositories/
        ├── Models/
        ├── DTOs/
        ├── Data/
        │   └── Migrations/
        └── Helpers/
```

---

## 🧰 Bonus: Postman Collection

A ready-to-use Postman collection is included at:
`src/GreenOpportunities.API/GreenOpportunities.postman_collection.json`

Import it into Postman → it has three folders (Auth / Public / Protected) and a test script that automatically saves the JWT into the `{{jwt_token}}` variable after a successful login.

---

## 📝 Notes

- The API is **idempotent for reads**: restarting the app regenerates the DB if it doesn't exist, but never wipes existing data.
- The `Admin` role is used by default on `POST /api/auth/register` (for demo purposes).
- For a real production deployment you would also want to:
  - Switch SQLite → PostgreSQL or SQL Server
  - Add a refresh-token system
  - Add FluentValidation or stricter business rules
  - Write unit / integration tests
  - Configure CORS + HTTPS + rate limiting

---

## 🌱 About **فرص خضراء**

> A platform publishing **800+ green opportunities and scholarships**, reaching **1M+ monthly visitors**.

📧 info@foraskhadra.com · 🌐 https://www.foraskhadra.com
