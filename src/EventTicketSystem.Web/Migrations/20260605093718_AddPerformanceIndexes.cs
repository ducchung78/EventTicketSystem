using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_TicketTypes_EventId",
                table: "TicketTypes",
                newName: "IX_TicketType_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Seats_TicketTypeId",
                table: "Seats",
                newName: "IX_Seat_TicketTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_RefundRequests_OrderId",
                table: "RefundRequests",
                newName: "IX_RefundRequest_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_ApplicationUserId",
                table: "Orders",
                newName: "IX_Order_ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_TicketTypeId",
                table: "OrderItems",
                newName: "IX_OrderItem_TicketTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                newName: "IX_OrderItem_OrderId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Seats",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "RefundRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seat_EventId_Status",
                table: "Seats",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_CreatedAt",
                table: "RefundRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequest_Status",
                table: "RefundRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Order_ApplicationUserId_OrderDate",
                table: "Orders",
                columns: new[] { "ApplicationUserId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Order_OrderDate",
                table: "Orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Event_Category",
                table: "Events",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Event_Category_StartDate",
                table: "Events",
                columns: new[] { "Category", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Event_IsActive",
                table: "Events",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Event_IsActive_StartDate",
                table: "Events",
                columns: new[] { "IsActive", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Event_StartDate",
                table: "Events",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Event_Venue",
                table: "Events",
                column: "Venue");

            migrationBuilder.CreateIndex(
                name: "IX_Coupon_ExpiryDate",
                table: "Coupons",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessage_CreatedAt",
                table: "ContactMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessage_IsRead",
                table: "ContactMessages",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUser_PhoneNumber",
                table: "AspNetUsers",
                column: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seat_EventId_Status",
                table: "Seats");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequest_CreatedAt",
                table: "RefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_RefundRequest_Status",
                table: "RefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_Order_ApplicationUserId_OrderDate",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Order_OrderDate",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Order_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Event_Category",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Event_Category_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Event_IsActive",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Event_IsActive_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Event_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Event_Venue",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Coupon_ExpiryDate",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_ContactMessage_CreatedAt",
                table: "ContactMessages");

            migrationBuilder.DropIndex(
                name: "IX_ContactMessage_IsRead",
                table: "ContactMessages");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationUser_PhoneNumber",
                table: "AspNetUsers");

            migrationBuilder.RenameIndex(
                name: "IX_TicketType_EventId",
                table: "TicketTypes",
                newName: "IX_TicketTypes_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Seat_TicketTypeId",
                table: "Seats",
                newName: "IX_Seats_TicketTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_RefundRequest_OrderId",
                table: "RefundRequests",
                newName: "IX_RefundRequests_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_Order_ApplicationUserId",
                table: "Orders",
                newName: "IX_Orders_ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_TicketTypeId",
                table: "OrderItems",
                newName: "IX_OrderItems_TicketTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_OrderId",
                table: "OrderItems",
                newName: "IX_OrderItems_OrderId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Seats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "RefundRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
