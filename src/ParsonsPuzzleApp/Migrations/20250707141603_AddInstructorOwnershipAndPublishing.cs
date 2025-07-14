using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParsonsPuzzleApp.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorOwnershipAndPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Puzzles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "InstructorId",
                table: "Puzzles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "Puzzles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Bundles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "InstructorId",
                table: "Bundles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Bundles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "Bundles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShareableLink",
                table: "Bundles",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Puzzles_InstructorId",
                table: "Puzzles",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_InstructorId",
                table: "Bundles",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_ShareableLink",
                table: "Bundles",
                column: "ShareableLink",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bundles_AspNetUsers_InstructorId",
                table: "Bundles",
                column: "InstructorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Puzzles_AspNetUsers_InstructorId",
                table: "Puzzles",
                column: "InstructorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bundles_AspNetUsers_InstructorId",
                table: "Bundles");

            migrationBuilder.DropForeignKey(
                name: "FK_Puzzles_AspNetUsers_InstructorId",
                table: "Puzzles");

            migrationBuilder.DropIndex(
                name: "IX_Puzzles_InstructorId",
                table: "Puzzles");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_InstructorId",
                table: "Bundles");

            migrationBuilder.DropIndex(
                name: "IX_Bundles_ShareableLink",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Puzzles");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Puzzles");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Puzzles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "ShareableLink",
                table: "Bundles");
        }
    }
}
