using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeatId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasSeatMap",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Seats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    TicketTypeId = table.Column<int>(type: "int", nullable: true),
                    Zone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RowLabel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SeatNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seats_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Seats_TicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalTable: "TicketTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 1,
                column: "HasSeatMap",
                value: false);

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 2,
                column: "HasSeatMap",
                value: false);

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 3,
                column: "HasSeatMap",
                value: false);

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 4,
                column: "HasSeatMap",
                value: false);

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 5,
                column: "HasSeatMap",
                value: false);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SeatId",
                table: "OrderItems",
                column: "SeatId");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_EventId_Zone_RowLabel_SeatNumber",
                table: "Seats",
                columns: new[] { "EventId", "Zone", "RowLabel", "SeatNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seats_TicketTypeId",
                table: "Seats",
                column: "TicketTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Seats_SeatId",
                table: "OrderItems",
                column: "SeatId",
                principalTable: "Seats",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Seats_SeatId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "Seats");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_SeatId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SeatId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "HasSeatMap",
                table: "Events");
        }
    }
}
