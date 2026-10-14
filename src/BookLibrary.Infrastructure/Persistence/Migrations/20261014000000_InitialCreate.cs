using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookLibrary.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetRoles",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                AccessFailedCount = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUsers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Authors",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                Biography = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                PortraitImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                DeathDate = table.Column<DateOnly>(type: "date", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Authors", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "BookRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BookTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                AuthorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                PreferredFormat = table.Column<int>(type: "int", nullable: false),
                Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                IsbnOrEdition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                AdditionalDetails = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                RequesterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                UserVisibleResponse = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                AdminNotes = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BookRequests", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ContactMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                AdminNotes = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContactMessages", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Genres",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Slug = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                ParentGenreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Genres", x => x.Id);
                table.ForeignKey(
                    name: "FK_Genres_Genres_ParentGenreId",
                    column: x => x.ParentGenreId,
                    principalTable: "Genres",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Languages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                NativeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Languages", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                ReplacedByTokenHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AspNetRoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserRoles",
            columns: table => new
            {
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Books",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Slug = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                Synopsis = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                Isbn10 = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                Isbn13 = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: true),
                CoverImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                PublicationYear = table.Column<int>(type: "int", nullable: true),
                PublicationDate = table.Column<DateOnly>(type: "date", nullable: true),
                Publisher = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                OriginalLanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SeriesName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                SeriesOrder = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                Edition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Books", x => x.Id);
                table.ForeignKey(
                    name: "FK_Books_Languages_OriginalLanguageId",
                    column: x => x.OriginalLanguageId,
                    principalTable: "Languages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "BookAuthors",
            columns: table => new
            {
                BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DisplayOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BookAuthors", x => new { x.BookId, x.AuthorId });
                table.ForeignKey(
                    name: "FK_BookAuthors_Authors_AuthorId",
                    column: x => x.AuthorId,
                    principalTable: "Authors",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_BookAuthors_Books_BookId",
                    column: x => x.BookId,
                    principalTable: "Books",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BookFiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Format = table.Column<int>(type: "int", nullable: false),
                GoogleDriveFileId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                OriginalSharingUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                EditionNote = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BookFiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_BookFiles_Books_BookId",
                    column: x => x.BookId,
                    principalTable: "Books",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BookFiles_Languages_LanguageId",
                    column: x => x.LanguageId,
                    principalTable: "Languages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "BookGenres",
            columns: table => new
            {
                BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                GenreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BookGenres", x => new { x.BookId, x.GenreId });
                table.ForeignKey(
                    name: "FK_BookGenres_Books_BookId",
                    column: x => x.BookId,
                    principalTable: "Books",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BookGenres_Genres_GenreId",
                    column: x => x.GenreId,
                    principalTable: "Genres",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "UserBookmarks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserBookmarks", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserBookmarks_Books_BookId",
                    column: x => x.BookId,
                    principalTable: "Books",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BrokenLinkReports",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BookFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReporterUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                IsResolved = table.Column<bool>(type: "bit", nullable: false),
                ReportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BrokenLinkReports", x => x.Id);
                table.ForeignKey(
                    name: "FK_BrokenLinkReports_BookFiles_BookFileId",
                    column: x => x.BookFileId,
                    principalTable: "BookFiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_AspNetRoleClaims_RoleId", table: "AspNetRoleClaims", column: "RoleId");
        migrationBuilder.CreateIndex(name: "RoleNameIndex", table: "AspNetRoles", column: "NormalizedName", unique: true, filter: "[NormalizedName] IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_AspNetUserClaims_UserId", table: "AspNetUserClaims", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_AspNetUserLogins_UserId", table: "AspNetUserLogins", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_AspNetUserRoles_RoleId", table: "AspNetUserRoles", column: "RoleId");
        migrationBuilder.CreateIndex(name: "EmailIndex", table: "AspNetUsers", column: "NormalizedEmail");
        migrationBuilder.CreateIndex(name: "UserNameIndex", table: "AspNetUsers", column: "NormalizedUserName", unique: true, filter: "[NormalizedUserName] IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_CreatedAtUtc", table: "AuditLogs", column: "CreatedAtUtc");
        migrationBuilder.CreateIndex(name: "IX_Authors_DisplayName", table: "Authors", column: "DisplayName");
        migrationBuilder.CreateIndex(name: "IX_Authors_Slug", table: "Authors", column: "Slug", unique: true);
        migrationBuilder.CreateIndex(name: "IX_BookAuthors_AuthorId", table: "BookAuthors", column: "AuthorId");
        migrationBuilder.CreateIndex(name: "IX_BookFiles_BookId_Format_IsActive", table: "BookFiles", columns: new[] { "BookId", "Format", "IsActive" });
        migrationBuilder.CreateIndex(name: "IX_BookFiles_BookId_GoogleDriveFileId", table: "BookFiles", columns: new[] { "BookId", "GoogleDriveFileId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_BookFiles_LanguageId", table: "BookFiles", column: "LanguageId");
        migrationBuilder.CreateIndex(name: "IX_BookGenres_GenreId", table: "BookGenres", column: "GenreId");
        migrationBuilder.CreateIndex(name: "IX_BookRequests_RequesterId", table: "BookRequests", column: "RequesterId");
        migrationBuilder.CreateIndex(name: "IX_BookRequests_Status", table: "BookRequests", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_Books_CreatedAtUtc", table: "Books", column: "CreatedAtUtc");
        migrationBuilder.CreateIndex(name: "IX_Books_OriginalLanguageId", table: "Books", column: "OriginalLanguageId");
        migrationBuilder.CreateIndex(name: "IX_Books_PublicationYear", table: "Books", column: "PublicationYear");
        migrationBuilder.CreateIndex(name: "IX_Books_Slug", table: "Books", column: "Slug", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Books_Status", table: "Books", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_Books_Title", table: "Books", column: "Title");
        migrationBuilder.CreateIndex(name: "IX_BrokenLinkReports_BookFileId", table: "BrokenLinkReports", column: "BookFileId");
        migrationBuilder.CreateIndex(name: "IX_ContactMessages_Status", table: "ContactMessages", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_Genres_ParentGenreId", table: "Genres", column: "ParentGenreId");
        migrationBuilder.CreateIndex(name: "IX_Genres_Slug", table: "Genres", column: "Slug", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Languages_Code", table: "Languages", column: "Code", unique: true);
        migrationBuilder.CreateIndex(name: "IX_RefreshTokens_TokenHash", table: "RefreshTokens", column: "TokenHash", unique: true);
        migrationBuilder.CreateIndex(name: "IX_RefreshTokens_UserId", table: "RefreshTokens", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_UserBookmarks_BookId", table: "UserBookmarks", column: "BookId");
        migrationBuilder.CreateIndex(name: "IX_UserBookmarks_UserId_BookId", table: "UserBookmarks", columns: new[] { "UserId", "BookId" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AspNetRoleClaims");
        migrationBuilder.DropTable(name: "AspNetUserClaims");
        migrationBuilder.DropTable(name: "AspNetUserLogins");
        migrationBuilder.DropTable(name: "AspNetUserRoles");
        migrationBuilder.DropTable(name: "AspNetUserTokens");
        migrationBuilder.DropTable(name: "AuditLogs");
        migrationBuilder.DropTable(name: "BookAuthors");
        migrationBuilder.DropTable(name: "BookGenres");
        migrationBuilder.DropTable(name: "BookRequests");
        migrationBuilder.DropTable(name: "BrokenLinkReports");
        migrationBuilder.DropTable(name: "ContactMessages");
        migrationBuilder.DropTable(name: "RefreshTokens");
        migrationBuilder.DropTable(name: "UserBookmarks");
        migrationBuilder.DropTable(name: "AspNetRoles");
        migrationBuilder.DropTable(name: "AspNetUsers");
        migrationBuilder.DropTable(name: "Authors");
        migrationBuilder.DropTable(name: "Genres");
        migrationBuilder.DropTable(name: "BookFiles");
        migrationBuilder.DropTable(name: "Books");
        migrationBuilder.DropTable(name: "Languages");
    }
}
