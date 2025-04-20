using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBot.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SyncMode",
                table: "RoleMirrorMappings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SyncMode",
                table: "RoleMirrorMappings");
        }
    }
}
