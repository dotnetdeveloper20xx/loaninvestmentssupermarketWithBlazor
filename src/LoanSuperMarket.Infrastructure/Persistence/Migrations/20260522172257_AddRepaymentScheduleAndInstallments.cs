using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanSuperMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRepaymentScheduleAndInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "LoanApplications",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "LoanApplications",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 2);

            migrationBuilder.AlterColumn<Guid>(
                name: "LoanProductId",
                table: "LoanApplications",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "DocumentRequestNote",
                table: "LoanApplications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewReason",
                table: "LoanApplications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "LoanApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "LoanApplications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Lenders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreditTier",
                table: "Borrowers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Borrowers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StorageReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    VerifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationDocuments_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepaymentSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoanApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FundedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualInterestRate = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    TermMonths = table.Column<int>(type: "int", nullable: false),
                    MonthlyEmi = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalInterestPayable = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Performance = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepaymentSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepaymentSchedules_Lenders_LenderId",
                        column: x => x.LenderId,
                        principalTable: "Lenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepaymentSchedules_LoanApplications_LoanApplicationId",
                        column: x => x.LoanApplicationId,
                        principalTable: "LoanApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Installments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepaymentScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrincipalPortion = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestPortion = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LateFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReminderSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LateNoticeSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Installments_RepaymentSchedules_RepaymentScheduleId",
                        column: x => x.RepaymentScheduleId,
                        principalTable: "RepaymentSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lenders_UserId",
                table: "Lenders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Borrowers_UserId",
                table: "Borrowers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDocuments_LoanApplicationId",
                table: "ApplicationDocuments",
                column: "LoanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDocuments_Status",
                table: "ApplicationDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_DueDate",
                table: "Installments",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_RepaymentScheduleId",
                table: "Installments",
                column: "RepaymentScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_RepaymentScheduleId_InstallmentNumber",
                table: "Installments",
                columns: new[] { "RepaymentScheduleId", "InstallmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Installments_Status",
                table: "Installments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RepaymentSchedules_LenderId",
                table: "RepaymentSchedules",
                column: "LenderId");

            migrationBuilder.CreateIndex(
                name: "IX_RepaymentSchedules_LoanApplicationId",
                table: "RepaymentSchedules",
                column: "LoanApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RepaymentSchedules_Performance",
                table: "RepaymentSchedules",
                column: "Performance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationDocuments");

            migrationBuilder.DropTable(
                name: "Installments");

            migrationBuilder.DropTable(
                name: "RepaymentSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Lenders_UserId",
                table: "Lenders");

            migrationBuilder.DropIndex(
                name: "IX_Borrowers_UserId",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "DocumentRequestNote",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "ReviewReason",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Lenders");

            migrationBuilder.DropColumn(
                name: "CreditTier",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Borrowers");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "LoanApplications",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "LoanApplications",
                type: "int",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<Guid>(
                name: "LoanProductId",
                table: "LoanApplications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
