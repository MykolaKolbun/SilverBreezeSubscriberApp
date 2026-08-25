using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSubscription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppAccounts_Email",
                table: "AppAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AppAccounts",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "AppAccounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneConfirmed",
                table: "AppAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PhoneOtps",
                columns: table => new
                {
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneOtps", x => x.Phone);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppAccounts_Email",
                table: "AppAccounts",
                column: "Email",
                unique: true,
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppAccounts_Phone",
                table: "AppAccounts",
                column: "Phone",
                unique: true,
                filter: "\"Phone\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneOtps");

            migrationBuilder.DropIndex(
                name: "IX_AppAccounts_Email",
                table: "AppAccounts");

            migrationBuilder.DropIndex(
                name: "IX_AppAccounts_Phone",
                table: "AppAccounts");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "AppAccounts");

            migrationBuilder.DropColumn(
                name: "PhoneConfirmed",
                table: "AppAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AppAccounts",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAccounts_Email",
                table: "AppAccounts",
                column: "Email",
                unique: true);
        }
    }
}
