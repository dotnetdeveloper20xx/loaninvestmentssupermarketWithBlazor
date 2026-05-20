using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanSuperMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lenders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AvailableFunds = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lenders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lenders_CreatedAtUtc",
                table: "Lenders",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Lenders_Email",
                table: "Lenders",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lenders_Status",
                table: "Lenders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lenders");
        }
    }
}
