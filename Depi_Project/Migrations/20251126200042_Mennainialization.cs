using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Depi_Project.Migrations
{
    /// <inheritdoc />
    public partial class Mennainialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Favorites_GymId",
                table: "Favorites",
                column: "GymId");

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Gyms_GymId",
                table: "Favorites",
                column: "GymId",
                principalTable: "Gyms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_Gyms_GymId",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_GymId",
                table: "Favorites");
        }
    }
}
