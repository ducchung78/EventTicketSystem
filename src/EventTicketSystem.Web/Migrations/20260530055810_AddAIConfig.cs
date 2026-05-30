using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketSystem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAIConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutoRefundThresholdMinutes = table.Column<int>(type: "int", nullable: false),
                    MinTrainingSamples = table.Column<int>(type: "int", nullable: false),
                    Iterations = table.Column<int>(type: "int", nullable: false),
                    L1Regularization = table.Column<float>(type: "real", nullable: false),
                    L2Regularization = table.Column<float>(type: "real", nullable: false),
                    LastTrainedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModelMAE = table.Column<double>(type: "float", nullable: true),
                    LastModelRMSE = table.Column<double>(type: "float", nullable: true),
                    LastModelR2 = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConfigs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AIConfigs",
                columns: new[] { "Id", "AutoRefundThresholdMinutes", "Iterations", "L1Regularization", "L2Regularization", "LastModelMAE", "LastModelR2", "LastModelRMSE", "LastTrainedAt", "MinTrainingSamples" },
                values: new object[] { 1, 60, 100, 0f, 0.1f, null, null, null, null, 60 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIConfigs");
        }
    }
}
