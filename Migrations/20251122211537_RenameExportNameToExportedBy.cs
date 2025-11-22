using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UiDesktopApp1.Migrations
{
    /// <inheritdoc />
    public partial class RenameExportNameToExportedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExportName",
                table: "Exports",
                newName: "ExportedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExportedBy",
                table: "Exports",
                newName: "ExportName");
        }
    }
}
