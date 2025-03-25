using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageBus.Client.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChianVersionAndIntroduceBoolena : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPublishedVersion",
                table: "FailedMessageChains");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "FailedMessageChains");

            migrationBuilder.AddColumn<bool>(
                name: "ShouldRepublish",
                table: "FailedMessageChains",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShouldRepublish",
                table: "FailedMessageChains");

            migrationBuilder.AddColumn<int>(
                name: "LastPublishedVersion",
                table: "FailedMessageChains",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "FailedMessageChains",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
