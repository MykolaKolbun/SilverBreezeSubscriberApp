using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSubscription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingIntegrationConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingIntegrationConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UsernameEncrypted = table.Column<string>(type: "text", nullable: true),
                    PasswordEncrypted = table.Column<string>(type: "text", nullable: true),
                    FacilityNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ParkingProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValueProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultCountry = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    QrIdentificationType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    QrIdentificationSubType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CustomerLinkField = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingIntegrationConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingIntegrationConfigs");
        }
    }
}
