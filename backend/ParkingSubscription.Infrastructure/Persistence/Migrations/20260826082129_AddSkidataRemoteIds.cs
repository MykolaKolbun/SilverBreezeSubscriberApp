using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSubscription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkidataRemoteIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SkidataCardId",
                table: "ValueCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SkidataUserId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SkidataCardId",
                table: "ParkingCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SkidataCustomerId",
                table: "Customers",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkidataCardId",
                table: "ValueCards");

            migrationBuilder.DropColumn(
                name: "SkidataUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SkidataCardId",
                table: "ParkingCards");

            migrationBuilder.DropColumn(
                name: "SkidataCustomerId",
                table: "Customers");
        }
    }
}
