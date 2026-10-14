# Digital Book Library — ASP.NET Core 8 Web API

A secure, maintainable, production-ready REST API for a digital book library inspired by the simplicity of OceanofPDF. Users can discover books, browse authors and genres, search the catalog, and download available **PDF** or **EPUB** editions hosted on **Google Drive**.

---

## 1. Architecture & Technology Stack

- **Runtime & Framework:** C# 12, .NET 8 LTS (`net8.0`), ASP.NET Core Web API
- **Database & ORM:** Microsoft SQL Server 2022, Entity Framework Core 8
- **Authentication & Authorization:** ASP.NET Core Identity, JWT Bearer Authentication, Refresh Token Rotation, Role-Based Access Control (`User` and `Admin`)
- **Validation & Error Handling:** FluentValidation, RFC 7807 `ProblemDetails` with `X-Correlation-ID` tracing
- **Security Hardening:** Strict Google Drive URL parser (`IBookFileLinkService`) preventing SSRF, fixed-window rate limiting on auth & submission endpoints, account lockout, explicit CORS policy
- **Testing:** xUnit, FluentAssertions, `WebApplicationFactory<Program>` integration test suite
- **Containerization:** Multi-stage `Dockerfile` and `docker-compose.yml` (SQL Server + API)

### Solution Structure
- `BookLibrary.sln` — Visual Studio 2022 Solution
- `src/BookLibrary.Domain` — Core entities, enums, role constants
- `src/BookLibrary.Application` — Service interfaces, DTOs, FluentValidation validators, slug generator, pagination models
- `src/BookLibrary.Infrastructure` — `AppDbContext`, EF Core entity configurations, SQL Server migrations, `GoogleDriveBookFileLinkService`, domain services, Identity & JWT management
- `src/BookLibrary.Api` — REST controllers (`/api/v1/*`), `GlobalExceptionMiddleware`, rate limiting, health checks, Swagger configuration
- `tests/BookLibrary.Tests.Unit` — Unit tests for Google Drive URL parsing, SSRF protection, catalog visibility, and IDOR prevention
- `tests/BookLibrary.Tests.Integration` — End-to-end API integration tests covering role enforcement, book creation, Drive file linking, publishing, and download redirects

---

## 2. Opening the Solution in Visual Studio

1. Extract `BookLibrary.zip` to your machine.
2. Open `BookLibrary.sln` in **Visual Studio 2022** (version 17.8 or newer) with the **ASP.NET and web development** workload installed.
3. Visual Studio will automatically restore NuGet packages across all 6 projects (`src` and `tests` solution folders).
4. Set `BookLibrary.Api` as the **Startup Project**.

---

## 3. Configuring SQL Server & Secrets

### Option A: Local Development with User Secrets / `appsettings.Development.json`
Update `ConnectionStrings:DefaultConnection` in `src/BookLibrary.Api/appsettings.Development.json` or via .NET User Secrets:

```bash
cd src/BookLibrary.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=BookLibraryDb_Dev;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet user-secrets set "Jwt:SecretKey" "YourSuperSecretDevelopmentSigningKey_AtLeast32Chars!"
dotnet user-secrets set "AdminBootstrap:Email" "admin@booklibrary.local"
dotnet user-secrets set "AdminBootstrap:Password" "AdminStrongPass_2026!"
```

### Option B: Docker Compose (Recommended Reproducible Local Setup)
1. Copy `.env.example` to `.env` and fill in your local passwords.
2. Run:
   ```bash
   docker compose up --build
   ```
3. The API will wait for SQL Server health checks to pass, apply EF Core migrations automatically via `DatabaseInitializer`, seed reference languages (`en`, `ka`, `es`, `fr`, `de`) and roles (`Admin`, `User`), and listen on `http://localhost:5080`.

---

## 4. Applying Database Migrations & Running the API

### Using the .NET CLI
```bash
# 1. Restore dependencies
dotnet restore BookLibrary.sln

# 2. Build in Release mode
dotnet build BookLibrary.sln -c Release

# 3. Apply EF Core migrations to SQL Server
dotnet ef database update --project src/BookLibrary.Infrastructure --startup-project src/BookLibrary.Api

# 4. Run the Web API
dotnet run --project src/BookLibrary.Api
```

- **Swagger UI URL:** `http://localhost:5080/swagger` (or `https://localhost:7080/swagger`)
- **Health Check URL:** `http://localhost:5080/health`

---

## 5. Secure Administrator Bootstrap Procedure

Public registration (`POST /api/v1/auth/register`) strictly assigns the `User` role and can **never** grant `Admin` privileges.

To bootstrap the first administrator securely:
1. Set `AdminBootstrap__Email` and `AdminBootstrap__Password` via environment variables or `.NET User Secrets` prior to starting the application.
2. On startup, `DatabaseInitializer` checks if the configured email exists; if not, it creates the account with `EmailConfirmed = true` and assigns the `Admin` role.
3. Once bootstrapped, you may remove the `AdminBootstrap__Password` environment variable and promote additional administrators via `PUT /api/v1/admin/users/{userId}/role`.

---

## 6. Example API Workflows (Swagger / cURL)

### 6.1 Register & Log In
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@booklibrary.local",
  "password": "AdminStrongPass_2026!"
}
```
Copy the `accessToken` from the response and click **Authorize** in Swagger (`Bearer <accessToken>`).

### 6.2 Create Author, Genre, and Book (Admin)
```http
POST /api/v1/admin/books
Authorization: Bearer <ADMIN_TOKEN>
Content-Type: application/json

{
  "title": "The Lord of the Rings: The Fellowship of the Ring",
  "synopsis": "One Ring to rule them all...",
  "isbn13": "9780261102354",
  "publicationYear": 1954,
  "publisher": "George Allen & Unwin",
  "originalLanguageId": "<ENGLISH_LANGUAGE_GUID>",
  "seriesName": "The Lord of the Rings",
  "seriesOrder": 1,
  "authorIds": ["<TOLKIEN_AUTHOR_GUID>"],
  "genreIds": ["<FANTASY_GENRE_GUID>"]
}
```

### 6.3 Associate an Existing Google Drive Sharing Link (Admin)
No file binaries are uploaded to the server. Paste the public Google Drive sharing URL:
```http
POST /api/v1/admin/books/{bookId}/files
Authorization: Bearer <ADMIN_TOKEN>
Content-Type: application/json

{
  "sharingUrl": "https://drive.google.com/file/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/view?usp=sharing",
  "format": "Epub",
  "originalFileName": "Fellowship_of_the_Ring.epub",
  "fileSizeBytes": 2458192,
  "languageId": "<ENGLISH_LANGUAGE_GUID>",
  "editionNote": "50th Anniversary Edition"
}
```

### 6.4 Publish the Book (Admin)
```http
POST /api/v1/admin/books/{bookId}/publish
Authorization: Bearer <ADMIN_TOKEN>
```

### 6.5 Search Catalog & Download File (Public / User)
```http
GET /api/v1/books?page=1&pageSize=20&search=tolkien&genre=fantasy&language=en&format=Epub&sortBy=title&sortDirection=asc
```
To list formats or download:
- `GET /api/v1/books/{idOrSlug}/files` — Returns available formats and derived Google Drive links.
- `GET /api/v1/books/{idOrSlug}/files/{fileId}/download` — Validates that the book is `Published` and the file is `IsActive`, then issues an HTTP `302 Found` redirect to `https://drive.google.com/uc?export=download&id={GoogleDriveFileId}` (or returns JSON metadata if `?redirect=false` is passed).

---

## 7. Running Unit and Integration Tests

```bash
# Run all Unit and Integration tests
dotnet test BookLibrary.sln --configuration Release --verbosity normal
```

---

## 8. Security Notes on Google Drive Hosting

- All book files hosted on Google Drive with `"Anyone with the link"` are publicly accessible to anyone who possesses the Drive link. JWT authentication on the API protects catalog administration and user workflows, not the underlying public Google Drive file itself.
- The backend strictly validates Google Drive URLs against `drive.google.com` and `docs.google.com` over HTTPS and extracts only validated File IDs (`^[a-zA-Z0-9_-]{15,80}$`), completely preventing Server-Side Request Forgery (SSRF).
