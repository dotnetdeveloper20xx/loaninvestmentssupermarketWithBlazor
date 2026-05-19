using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanSuperMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoanProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MaximumAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumAmountCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MinimumTermMonths = table.Column<int>(type: "int", nullable: false),
                    MaximumTermMonths = table.Column<int>(type: "int", nullable: false),
                    LenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanProducts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanProducts_CreatedAtUtc",
                table: "LoanProducts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LoanProducts_LenderId",
                table: "LoanProducts",
                column: "LenderId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanProducts_Status",
                table: "LoanProducts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanProducts");
        }
    }
}
