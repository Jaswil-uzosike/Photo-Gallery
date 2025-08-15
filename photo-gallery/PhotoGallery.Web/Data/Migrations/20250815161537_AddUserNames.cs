using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoGallery.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_GalleryId_CreatedUtc",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Galleries_OwnerId_CreatedUtc",
                table: "Galleries");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_GalleryId",
                table: "Photos",
                column: "GalleryId");

            migrationBuilder.CreateIndex(
                name: "IX_Galleries_OwnerId",
                table: "Galleries",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_GalleryId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Galleries_OwnerId",
                table: "Galleries");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_GalleryId_CreatedUtc",
                table: "Photos",
                columns: new[] { "GalleryId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Galleries_OwnerId_CreatedUtc",
                table: "Galleries",
                columns: new[] { "OwnerId", "CreatedUtc" });
        }
    }
}
