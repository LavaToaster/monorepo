using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class RoleMirror : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleMirrorCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMirrorCandidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleMirrorMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceRoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetRoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMirrorMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleMirrorMappings_RoleMirrorCandidates_SourceRoleId",
                        column: x => x.SourceRoleId,
                        principalTable: "RoleMirrorCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleMirrorMappings_RoleMirrorCandidates_TargetRoleId",
                        column: x => x.TargetRoleId,
                        principalTable: "RoleMirrorCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleMirrorCandidates_GuildId_RoleId",
                table: "RoleMirrorCandidates",
                columns: new[] { "GuildId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleMirrorMappings_SourceRoleId_TargetRoleId",
                table: "RoleMirrorMappings",
                columns: new[] { "SourceRoleId", "TargetRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleMirrorMappings_TargetRoleId",
                table: "RoleMirrorMappings",
                column: "TargetRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleMirrorMappings");

            migrationBuilder.DropTable(
                name: "RoleMirrorCandidates");
        }
    }
}
