using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatGridFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GridCol",
                table: "Seats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GridRow",
                table: "Seats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservedUntil",
                table: "Seats",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeatType",
                table: "Seats",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GridCol",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "GridRow",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "ReservedUntil",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "SeatType",
                table: "Seats");
        }
    }
}
