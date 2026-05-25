using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EventTicketSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponsAndCartSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CouponId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinOrderValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxUses = table.Column<int>(type: "int", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Coupons",
                columns: new[] { "Id", "Code", "Description", "DiscountAmount", "DiscountPercent", "ExpiryDate", "IsActive", "MaxUses", "MinOrderValue", "UsedCount" },
                values: new object[,]
                {
                    { 1, "WELCOME10", "Giảm 10% cho đơn hàng đầu tiên", 0m, 10m, new DateTime(2027, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 100, 50000m, 0 },
                    { 2, "SUMMER50K", "Giảm 50,000đ cho đơn từ 200,000đ", 50000m, 0m, new DateTime(2027, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 50, 200000m, 0 },
                    { 3, "VIP20", "Giảm 20% dành cho thành viên VIP", 0m, 20m, new DateTime(2027, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 30, 100000m, 0 },
                    { 4, "TICKET100K", "Giảm 100,000đ cho đơn từ 500,000đ", 100000m, 0m, new DateTime(2027, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 20, 500000m, 0 },
                    { 5, "FREESHIP", "Giảm 5% không giới hạn đơn tối thiểu", 0m, 5m, new DateTime(2027, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 200, 0m, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CouponId",
                table: "Orders",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                table: "Coupons",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Coupons_CouponId",
                table: "Orders",
                column: "CouponId",
                principalTable: "Coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Coupons_CouponId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CouponId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CouponId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "Orders");
        }
    }
}
