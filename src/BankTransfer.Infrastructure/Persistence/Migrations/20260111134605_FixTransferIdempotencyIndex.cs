using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankTransfer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixTransferIdempotencyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transfers_IdempotencyKey",
                table: "transfers");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_FromAccountId_IdempotencyKey",
                table: "transfers",
                columns: new[] { "FromAccountId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transfers_FromAccountId_IdempotencyKey",
                table: "transfers");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_IdempotencyKey",
                table: "transfers",
                column: "IdempotencyKey",
                unique: true);
        }
    }
}
