# 📚 Digital Book Library — ASP.NET Core 8 Web API

![.NET 8.0](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?style=flat-square&logo=dotnet)
![C# 12](https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=c-sharp)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=flat-square&logo=microsoft-sql-server)
![EF Core 8](https://img.shields.io/badge/EF%20Core-8.0-512BD4?style=flat-square)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=flat-square&logo=docker)
![xUnit](https://img.shields.io/badge/Tests-xUnit%20%26%20FluentAssertions-28A745?style=flat-square)

A secure, pragmatic, production-ready REST API for a digital book library inspired by the simplicity of **OceanofPDF**. Users can discover books, browse authors and hierarchical genres, search and filter the catalog, request missing titles, maintain personal reading bookmarks, and download available **PDF** or **EPUB** editions hosted on **Google Drive**.

---

## ✨ Key Features

- **Zero-Binary Google Drive Integration:** Book files remain hosted on Google Drive (`Anyone with the link`). SQL Server stores only catalog metadata and validated Google Drive File IDs—no book binaries are uploaded to or streamed through the API server.
- **SSRF-Safe URL Parser (`IBookFileLinkService`):** Strictly validates Google Drive URLs (`drive.google.com`, `docs.google.com` over HTTPS), extracts canonical file IDs (`^[a-zA-Z0-9_-]{15,80}$`), rejects malformed/malicious hosts, and generates direct view and download links dynamically.
- **Rich Catalog & Taxonomy Management:**
  - Multi-author and multi-genre book associations with author ordering.
  - Hierarchical genres (`ParentGenreId` / sub-genres).
  - Distinction between a book's `OriginalLanguage` and individual `BookFile` edition languages.
  - Lifecycle workflow (`Draft` ➔ `Published` ➔ `Archived`) ensuring only `Published` books appear in public search or download endpoints.
  - Automatic SEO slug generation and related-book recommendations.
- **Database-Side Search, Filtering & Pagination:** Case-insensitive search across titles, synopses, ISBN-10/ISBN-13, and author names; filterable by genre, author, language code, format (`PDF`/`EPUB`), and publication year.
- **Identity, JWT & Refresh Token Rotation:** ASP.NET Core Identity with short-lived HMAC-SHA256 JWT access tokens, SHA-256 hashed refresh token rotation, account lockout protection, and strict Role-Based Access Control (`User` vs. `Admin`).
- **User Interaction Workflows:** Book requests with strict ownership/IDOR protection, personal reading list bookmarks, broken Drive link reporting, and rate-limited contact messages.
- **Production Hardening:** RFC 7807 `ProblemDetails` global exception handling with `X-Correlation-ID` tracing, FluentValidation pipelines, fixed-window rate limiting (`429 TooManyRequests`), health checks (`/health`), and admin audit logging.

---

## 🏗️ Architecture & Project Structure

The solution follows a clean, pragmatic 4-layer architecture without unnecessary bloat (no generic repository wrappers, MediatR, or over-engineered abstractions):

```text
BookLibrary/
├── BookLibrary.sln
├── Dockerfile
├── docker-compose.yml
├── .env.example
├── src/
│   ├── BookLibrary.Domain/          # Core entities, enums, role constants (zero external dependencies)
│   ├── BookLibrary.Application/     # Service contracts, DTOs, FluentValidation validators, slug generator
│   ├── BookLibrary.Infrastructure/  # AppDbContext, EF Core configurations & migrations, Identity, Drive parser, services
│   └── BookLibrary.Api/             # REST controllers (/api/v1/*), GlobalExceptionMiddleware, JWT, Rate Limiting, Swagger
└── tests/
    ├── BookLibrary.Tests.Unit/        # xUnit unit tests (URL parser, SSRF prevention, catalog visibility, IDOR)
    └── BookLibrary.Tests.Integration/ # WebApplicationFactory<Program> end-to-end API integration tests
```

---

## 🚀 Getting Started

### Prerequisites

- **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** (v8.0+)
- **Microsoft SQL Server** (LocalDB, SQL Server 2022 Express/Developer, or Docker container)
- **Visual Studio 2022** (v17.8+), JetBrains Rider, or VS Code
- **Docker Desktop** *(optional, for containerized setup)*

---

### Option 1: Quick Start with Docker Compose (Recommended)

Run both SQL Server 2022 and the ASP.NET Core Web API in isolated containers:

```bash
# 1. Copy the environment template
cp .env.example .env

# 2. Build and start SQL Server + API
docker compose up --build
```

- On startup, the API waits for SQL Server's health check, automatically applies EF Core migrations, seeds reference languages (`en`, `ka`, `es`, `fr`, `de`) and roles (`Admin`, `User`), and bootstraps the initial administrator account.
- **Swagger UI:** `http://localhost:5080/swagger`
- **Health Check:** `http://localhost:5080/health`

---

### Option 2: Local Development (Visual Studio / .NET CLI)

#### 1. Configure Connection String & Admin Bootstrap
In `src/BookLibrary.Api/appsettings.Development.json` (or via **.NET User Secrets**), configure your SQL Server connection string, JWT signing key, and initial administrator credentials:

```bash
cd src/BookLibrary.Api

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=BookLibraryDb_Dev;User Id=sa;Password=Dev_Strong_Password_2026!;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet user-secrets set "Jwt:SecretKey" "DevOnly_JwtSecretKey_MustBeOverriddenInProduction_2026!"
dotnet user-secrets set "AdminBootstrap:Email" "admin@booklibrary.local"
dotnet user-secrets set "AdminBootstrap:Password" "AdminStrongPass_2026!"
dotnet user-secrets set "AdminBootstrap:DisplayName" "System Administrator"
```

> **Note:** Passwords must be at least 8 characters and include an uppercase letter, lowercase letter, digit, and special character.

#### 2. Apply EF Core Migrations & Run
From the solution root (`BookLibrary/`):

```bash
# Restore packages and build
dotnet restore BookLibrary.sln
dotnet build BookLibrary.sln -c Release

# Apply migrations (also runs automatically on startup via DatabaseInitializer)
dotnet ef database update --project src/BookLibrary.Infrastructure --startup-project src/BookLibrary.Api

# Run the API
dotnet run --project src/BookLibrary.Api --launch-profile https
```

- **Swagger UI (HTTPS):** `https://localhost:7080/swagger`
- **Swagger UI (HTTP):** `http://localhost:5080/swagger`

---

## 🔐 Authentication & Using Swagger UI

1. **Public Registration vs. Admin Bootstrap:**
   - Public registration (`POST /api/v1/auth/register`) strictly assigns the `User` role.
   - The first `Admin` account is created on startup via `AdminBootstrap:Email` and `AdminBootstrap:Password`.
2. **Generate an Admin JWT Token:**
   Call `POST /api/v1/auth/login`:
   ```json
   {
     "email": "admin@booklibrary.local",
     "password": "AdminStrongPass_2026!"
   }
   ```
3. **Authorize in Swagger UI:**
   - Copy the `accessToken` string from the login response (`eyJhbGci...`).
   - Click the **`Authorize`** button at the top right of Swagger UI.
   - Paste **only the raw token string** into the **Value** field *(do **not** type `Bearer ` before the token—Swagger's HTTP Bearer scheme adds the `Bearer ` prefix automatically)*.
   - Click **Authorize**, then **Close**.

---

## 📖 End-to-End Workflow: Adding & Publishing a Book

Once authorized as `Admin`, follow these 5 steps to publish a downloadable book:

### 1. Get a Language ID
Call `GET /api/v1/languages` and copy the `id` for your desired language (e.g., English `en` or Georgian `ka`).

### 2. Create an Author
`POST /api/v1/admin/authors`
```json
{
  "displayName": "J.R.R. Tolkien",
  "biography": "English writer, poet, philologist, and academic.",
  "birthDate": "1892-01-03",
  "deathDate": "1973-09-02"
}
```

### 3. Create a Genre
`POST /api/v1/admin/genres`
```json
{
  "name": "Fantasy",
  "description": "Speculative fiction set in a fictional universe."
}
```

### 4. Create the Book (Starts as `Draft`)
`POST /api/v1/admin/books`
```json
{
  "title": "The Hobbit",
  "synopsis": "Bilbo Baggins, a respectable hobbit, embarks on an unexpected journey...",
  "isbn10": "0261102214",
  "isbn13": "9780261102217",
  "publicationYear": 1937,
  "publicationDate": "1937-09-21",
  "publisher": "George Allen & Unwin",
  "originalLanguageId": "<LANGUAGE_GUID>",
  "seriesName": "Middle-earth",
  "seriesOrder": 1,
  "edition": "First Edition",
  "authorIds": ["<AUTHOR_GUID>"],
  "genreIds": ["<GENRE_GUID>"]
}
```

### 5. Link a Google Drive File & Publish
1. **Associate Google Drive URL:** `POST /api/v1/admin/books/{bookId}/files`
   ```json
   {
     "sharingUrl": "https://drive.google.com/file/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/view?usp=sharing",
     "format": "Epub",
     "originalFileName": "The_Hobbit.epub",
     "fileSizeBytes": 1850000,
     "languageId": "<LANGUAGE_GUID>",
     "editionNote": "Illustrated Edition",
     "isActive": true
   }
   ```
2. **Publish Book:** `POST /api/v1/admin/books/{bookId}/publish`
3. **Browse & Download Publicly:**
   - Search: `GET /api/v1/books?search=hobbit&genre=fantasy&format=Epub`
   - Download Redirect (`302 Found`): `GET /api/v1/books/the-hobbit/files/{fileId}/download`

---

## 🛣️ API Endpoints Reference

### Public Catalog Endpoints
| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/books` | Search, filter, sort, and paginate published books |
| `GET` | `/api/v1/books/recent` | Retrieve recently published books |
| `GET` | `/api/v1/books/{idOrSlug}` | Get published book details, files, and related books |
| `GET` | `/api/v1/books/{idOrSlug}/files` | List active PDF/EPUB files and Google Drive links |
| `GET` | `/api/v1/books/{idOrSlug}/files/{fileId}/download` | Redirect (`302`) to Google Drive download link (or return DTO if `?redirect=false`) |
| `POST` | `/api/v1/books/{bookId}/files/{fileId}/report-broken` | Report a broken or inaccessible Google Drive link |
| `GET` | `/api/v1/authors` | Paginate and search authors |
| `GET` | `/api/v1/authors/{idOrSlug}` | Get author biography and details |
| `GET` | `/api/v1/authors/{idOrSlug}/books` | List published books by author |
| `GET` | `/api/v1/genres` | List all genres |
| `GET` | `/api/v1/genres/{idOrSlug}` | Get genre details and sub-genres |
| `GET` | `/api/v1/genres/{idOrSlug}/books` | List published books by genre |
| `GET` | `/api/v1/languages` | List active languages |
| `POST` | `/api/v1/contact` | Submit a contact message *(rate-limited)* |

### Authentication & User Endpoints
| Method | Endpoint | Auth | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/auth/register` | Public | Register a new `User` account *(rate-limited)* |
| `POST` | `/api/v1/auth/login` | Public | Authenticate and receive JWT + Refresh Token *(rate-limited)* |
| `POST` | `/api/v1/auth/refresh` | Public | Rotate refresh token and issue new JWT |
| `POST` | `/api/v1/auth/logout` | `User`/`Admin` | Revoke active refresh token(s) |
| `POST` | `/api/v1/auth/forgot-password` | Public | Request a password reset token |
| `POST` | `/api/v1/auth/reset-password` | Public | Reset password using token |
| `POST` | `/api/v1/auth/verify-email` | Public | Confirm email address |
| `GET` | `/api/v1/users/me` | `User`/`Admin` | Get current user profile and roles |
| `PUT` | `/api/v1/users/me` | `User`/`Admin` | Update display name |
| `POST` | `/api/v1/users/me/change-password` | `User`/`Admin` | Change current password |
| `POST` | `/api/v1/book-requests` | `User`/`Admin` | Submit a new book request *(rate-limited)* |
| `GET` | `/api/v1/book-requests/me` | `User`/`Admin` | List current user's book requests |
| `GET` | `/api/v1/book-requests/me/{requestId}` | `User`/`Admin` | View a specific request (ownership enforced) |
| `POST` | `/api/v1/book-requests/me/{requestId}/cancel` | `User`/`Admin` | Cancel a `Pending` book request |
| `GET` | `/api/v1/bookmarks` | `User`/`Admin` | List personal bookmarked books |
| `POST` | `/api/v1/bookmarks/{bookId}` | `User`/`Admin` | Add a published book to bookmarks |
| `DELETE` | `/api/v1/bookmarks/{bookId}` | `User`/`Admin` | Remove a book from bookmarks |

### Administrator Endpoints (`[Authorize(Roles = "Admin")]`)
| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/v1/admin/statistics` | View catalog counts, pending requests, and broken link reports |
| `GET` | `/api/v1/admin/books` | List all books (`Draft`, `Published`, `Archived`) |
| `GET` | `/api/v1/admin/books/{id}` | Get full book details including inactive file associations |
| `POST` | `/api/v1/admin/books` | Create a new book in `Draft` status |
| `PUT` | `/api/v1/admin/books/{id}` | Update book metadata, authors, and genres |
| `POST` | `/api/v1/admin/books/{id}/publish` | Publish a book to the public catalog |
| `POST` | `/api/v1/admin/books/{id}/archive` | Archive a book (removes from public discovery) |
| `DELETE` | `/api/v1/admin/books/{id}` | Delete a book record |
| `POST` | `/api/v1/admin/books/{id}/files` | Parse a Google Drive sharing link and associate it with a book |
| `DELETE` | `/api/v1/admin/books/{id}/files/{fileId}` | Remove a Google Drive file association |
| `POST/PUT/DELETE` | `/api/v1/admin/authors/*` | Create, update, and safely delete authors |
| `POST/PUT/DELETE` | `/api/v1/admin/genres/*` | Create, update, and safely delete genres |
| `GET/POST/PUT` | `/api/v1/admin/languages/*` | Manage active and inactive languages |
| `GET/PATCH` | `/api/v1/admin/book-requests/*` | Review book requests, update status, and leave notes |
| `GET/PATCH` | `/api/v1/admin/contact-messages/*` | Review and resolve contact messages |
| `GET/PUT` | `/api/v1/admin/users/*` | List users and update user roles (`User` / `Admin`) |

---

## 🧪 Running Automated Tests

The solution includes unit and integration test suites built with **xUnit**, **FluentAssertions**, and **`WebApplicationFactory<Program>`**:

```bash
dotnet test BookLibrary.sln --configuration Release --verbosity normal
```

### What Is Tested:
- **`GoogleDriveBookFileLinkServiceTests`:** Validates extraction of file IDs across `/file/d/{id}/view`, `/open?id={id}`, `/uc?id={id}`, and `docs.google.com` links; verifies rejection of HTTP links, malformed IDs, phishing subdomains, and internal SSRF metadata addresses.
- **`BookServiceUnitTests`:** Verifies that `Draft` and `Archived` books are excluded from public search and detail endpoints, prevents duplicate Google Drive file IDs per book, and verifies IDOR ownership checks on `BookRequestService`.
- **`ApiIntegrationTests`:** Full HTTP pipeline tests verifying `403 Forbidden` when regular users call `/api/v1/admin/*`, admin authentication, author/genre/book creation, Google Drive file association, publishing, and `302 Found` download redirects.

---

## 🛡️ Security & Operational Notes

- **Google Drive Link Visibility:** Files shared on Google Drive with *"Anyone with the link"* are publicly accessible to anyone who possesses the underlying Drive URL. JWT authentication in this API protects administrative actions and user-specific data, not the public Google Drive file itself.
- **No Arbitrary URL Fetching (SSRF Protection):** The backend never issues outbound HTTP requests to user-supplied URLs. It strictly validates the URL syntax and extracts the Google Drive File ID locally.
