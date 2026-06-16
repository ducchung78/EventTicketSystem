using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixTicketPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Price",
                value: 50000m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Price",
                value: 150000m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Price",
                value: 299000m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Price",
                value: 499000m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Price",
                value: 25000m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Price",
                value: 75000m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "Price",
                value: 10000m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "Price",
                value: 35000m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 9,
                column: "Price",
                value: 15000m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 10,
                column: "Price",
                value: 50000m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Price",
                value: 50m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Price",
                value: 150m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Price",
                value: 299m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Price",
                value: 499m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Price",
                value: 25m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Price",
                value: 75m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "Price",
                value: 10m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 8,
                column: "Price",
                value: 35m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 9,
                column: "Price",
                value: 15m);

            migrationBuilder.UpdateData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 10,
                column: "Price",
                value: 50m);
        }
    }
}
