using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobMagnet.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobFirstSeenAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstSeenAt",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // Existing rows were never stamped: the site's posting date is the best estimate where we have one.
            migrationBuilder.Sql("""
                UPDATE "Jobs" SET "FirstSeenAt" = LEAST(COALESCE("PostedAt", "LastSeenAt"), "LastSeenAt");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Source_FirstSeenAt",
                table: "Jobs",
                columns: new[] { "Source", "FirstSeenAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_Source_FirstSeenAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FirstSeenAt",
                table: "Jobs");
        }
    }
}
