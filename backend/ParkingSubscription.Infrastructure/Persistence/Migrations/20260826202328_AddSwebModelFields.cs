using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ParkingSubscription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSwebModelFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CheckLp",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MatchEntryPlate",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PassageLp",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BlockDate",
                table: "ParkingCards",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Canceled",
                table: "ParkingCards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "ParkingCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "ParkingCards",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionReason",
                table: "ParkingCards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SingleNeutral",
                table: "ParkingCards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SuspensionEndDate",
                table: "ParkingCards",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SuspensionStartDate",
                table: "ParkingCards",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "Customers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ParkingCardCarParks",
                columns: table => new
                {
                    ParkingCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CarParkNumber = table.Column<int>(type: "integer", nullable: false),
                    EntryType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingCardCarParks", x => new { x.ParkingCardId, x.Id });
                    table.ForeignKey(
                        name: "FK_ParkingCardCarParks_ParkingCards_ParkingCardId",
                        column: x => x.ParkingCardId,
                        principalTable: "ParkingCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParkingCardSecondaryIds",
                columns: table => new
                {
                    ParkingCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SubType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingCardSecondaryIds", x => new { x.ParkingCardId, x.Id });
                    table.ForeignKey(
                        name: "FK_ParkingCardSecondaryIds_ParkingCards_ParkingCardId",
                        column: x => x.ParkingCardId,
                        principalTable: "ParkingCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingCardCarParks");

            migrationBuilder.DropTable(
                name: "ParkingCardSecondaryIds");

            migrationBuilder.DropColumn(
                name: "CheckLp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MatchEntryPlate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PassageLp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BlockDate",
                table: "ParkingCards");

            migrationBuilder.DropColumn(
                name: "Canceled",
                table: "ParkingCards");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ParkingCards");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "ParkingCards");

            migrationBuilder.DropColumn(
                name: "ProductionReason",
                table: "ParkingCards");

            migrationBuilder.DropColumn(
                name: "SingleNeutral",
                table: "ParkingCards");

            migrationBuilder.DropColumn(
                name: "SuspensionEndDate",
                table: "ParkingCards");

            migrationBuilder.DropColumn(
                name: "SuspensionStartDate",
                table: "ParkingCards");

            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "Customers");
        }
    }
}
