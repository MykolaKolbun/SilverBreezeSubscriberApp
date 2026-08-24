using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSubscription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalConfigAndReceiptUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiscalReceiptUrl",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FiscalGatewayConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    PinCodeEncrypted = table.Column<string>(type: "text", nullable: true),
                    LicenseKeyEncrypted = table.Column<string>(type: "text", nullable: true),
                    BaseUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TaxCode = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalGatewayConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiscalGatewayConfigs");

            migrationBuilder.DropColumn(
                name: "FiscalReceiptUrl",
                table: "Payments");
        }
    }
}
