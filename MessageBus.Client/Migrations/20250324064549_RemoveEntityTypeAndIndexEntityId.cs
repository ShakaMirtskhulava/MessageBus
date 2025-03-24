using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageBus.Client.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEntityTypeAndIndexEntityId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "FailedMessageChains");

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "FailedMessageChains",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_FailedMessageChains_EntityId",
                table: "FailedMessageChains",
                column: "EntityId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FailedMessageChains_EntityId",
                table: "FailedMessageChains");

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "FailedMessageChains",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "FailedMessageChains",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
