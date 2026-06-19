using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityBoost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriorityBoost",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Update seed SoldQuantity for demo/test data
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 1, column: "SoldQuantity", value: 280);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 2, column: "SoldQuantity", value: 65);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 3, column: "SoldQuantity", value: 190);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 4, column: "SoldQuantity", value: 38);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 5, column: "SoldQuantity", value: 95);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 6, column: "SoldQuantity", value: 22);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 7, column: "SoldQuantity", value: 210);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 8, column: "SoldQuantity", value: 45);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 9, column: "SoldQuantity", value: 520);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 10, column: "SoldQuantity", value: 60);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PriorityBoost", table: "Events");

            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 1, column: "SoldQuantity", value: 0);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 2, column: "SoldQuantity", value: 0);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 3, column: "SoldQuantity", value: 0);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 4, column: "SoldQuantity", value: 0);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 5, column: "SoldQuantity", value: 0);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 6, column: "SoldQuantity", value: 0);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 7, column: "SoldQuantity", value: 0);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 8, column: "SoldQuantity", value: 0);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 9, column: "SoldQuantity", value: 0);
            migrationBuilder.UpdateData(table: "TicketTypes", keyColumn: "Id", keyValue: 10, column: "SoldQuantity", value: 0);
        }
    }
}
