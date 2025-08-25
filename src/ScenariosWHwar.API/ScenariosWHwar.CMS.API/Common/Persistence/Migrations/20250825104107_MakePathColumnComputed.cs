using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScenariosWHwar.CMS.API.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakePathColumnComputed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BlobPath",
                table: "Episodes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                computedColumnSql: "CONCAT([Id], '.', [Format])",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BlobPath",
                table: "Episodes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComputedColumnSql: "CONCAT([Id], '.', [Format])");
        }
    }
}
