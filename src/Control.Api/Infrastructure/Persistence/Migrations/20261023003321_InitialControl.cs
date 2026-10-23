using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Control.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "control_outbox",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "StatusFile",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    unique_identifier = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusFile", x => x.id);
                    table.CheckConstraint("CK_StatusFile_status", "status BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatusFile_unique_identifier",
                table: "StatusFile",
                column: "unique_identifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "control_outbox");

            migrationBuilder.DropTable(
                name: "StatusFile");
        }
    }
}
