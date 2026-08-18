using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CliniSys.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthPlanManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HealthPlanId",
                table: "Patients",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HealthPlanNumber",
                table: "Patients",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HealthPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_HealthPlanId",
                table: "Patients",
                column: "HealthPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_HealthPlans_HealthPlanId",
                table: "Patients",
                column: "HealthPlanId",
                principalTable: "HealthPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_HealthPlans_HealthPlanId",
                table: "Patients");

            migrationBuilder.DropTable(
                name: "HealthPlans");

            migrationBuilder.DropIndex(
                name: "IX_Patients_HealthPlanId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "HealthPlanId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "HealthPlanNumber",
                table: "Patients");
        }
    }
}
