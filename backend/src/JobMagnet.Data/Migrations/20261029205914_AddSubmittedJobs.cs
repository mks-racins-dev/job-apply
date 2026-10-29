using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobMagnet.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmittedJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "Jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_SubmittedByUserId",
                table: "Jobs",
                column: "SubmittedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_SubmittedByUserId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "Jobs");
        }
    }
}
