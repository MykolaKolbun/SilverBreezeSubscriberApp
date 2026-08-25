using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSubscription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminMaxActiveSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxActiveSubscriptions",
                table: "AdminConfigs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxActiveSubscriptions",
                table: "AdminConfigs");
        }
    }
}
