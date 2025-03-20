using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageBus.Client.Migrations
{
    /// <inheritdoc />
    public partial class RenameIntegrationEventLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EFCoreIntegrationEventLog",
                table: "EFCoreIntegrationEventLog");

            migrationBuilder.RenameTable(
                name: "EFCoreIntegrationEventLog",
                newName: "INTEGRATION_EVNET_LOG_TABLE_NAME");

            migrationBuilder.AddPrimaryKey(
                name: "PK_INTEGRATION_EVNET_LOG_TABLE_NAME",
                table: "INTEGRATION_EVNET_LOG_TABLE_NAME",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_INTEGRATION_EVNET_LOG_TABLE_NAME",
                table: "INTEGRATION_EVNET_LOG_TABLE_NAME");

            migrationBuilder.RenameTable(
                name: "INTEGRATION_EVNET_LOG_TABLE_NAME",
                newName: "EFCoreIntegrationEventLog");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EFCoreIntegrationEventLog",
                table: "EFCoreIntegrationEventLog",
                column: "EventId");
        }
    }
}
