using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSpecialToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSpecial",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Category", "IsHot", "IsSpecial" },
                values: new object[] { "Âm nhạc", true, false });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Category", "IsHot", "IsSpecial" },
                values: new object[] { "Công nghệ", true, true });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Category", "IsSpecial" },
                values: new object[] { "Hài kịch", false });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Category", "IsSpecial" },
                values: new object[] { "Nghệ thuật", true });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Category", "IsHot", "IsSpecial" },
                values: new object[] { "Thể thao", true, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSpecial",
                table: "Events");

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Category", "IsHot" },
                values: new object[] { "Âm Nhạc", false });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Category", "IsHot" },
                values: new object[] { "Công Nghệ", false });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 3,
                column: "Category",
                value: "Hài Kịch");

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 4,
                column: "Category",
                value: "Nghệ Thuật");

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Category", "IsHot" },
                values: new object[] { "Thể Thao", false });
        }
    }
}
