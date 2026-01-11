using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankTransfer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScopeIdempotencyByOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_idempotency",
                table: "idempotency");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "idempotency",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_idempotency",
                table: "idempotency",
                columns: new[] { "OwnerId", "Key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_idempotency",
                table: "idempotency");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "idempotency");

            migrationBuilder.AddPrimaryKey(
                name: "PK_idempotency",
                table: "idempotency",
                column: "Key");
        }
    }
}
