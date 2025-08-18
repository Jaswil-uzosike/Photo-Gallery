using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoGallery.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoStorageKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Photos",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageProvider",
                table: "Photos",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbStorageKey",
                table: "Photos",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "StorageProvider",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "ThumbStorageKey",
                table: "Photos");
        }
    }
}
