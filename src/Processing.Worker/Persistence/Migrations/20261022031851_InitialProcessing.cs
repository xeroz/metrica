using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Processing.Worker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_outbox",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    load_id = table.Column<long>(type: "bigint", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_outbox", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_outbox_StatusFile_load_id",
                        column: x => x.load_id,
                        principalTable: "StatusFile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "processed_product",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    load_id = table.Column<long>(type: "bigint", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_product", x => x.code);
                    table.ForeignKey(
                        name: "FK_processed_product_StatusFile_load_id",
                        column: x => x.load_id,
                        principalTable: "StatusFile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "processing_failure",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    load_id = table.Column<long>(type: "bigint", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_failure", x => x.id);
                    table.ForeignKey(
                        name: "FK_processing_failure_StatusFile_load_id",
                        column: x => x.load_id,
                        principalTable: "StatusFile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "processing_period",
                columns: table => new
                {
                    period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    load_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_period", x => x.period);
                    table.ForeignKey(
                        name: "FK_processing_period_StatusFile_load_id",
                        column: x => x.load_id,
                        principalTable: "StatusFile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "processing_result",
                columns: table => new
                {
                    load_id = table.Column<long>(type: "bigint", nullable: false),
                    outcome = table.Column<string>(type: "text", nullable: false),
                    inserted_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_result", x => x.load_id);
                    table.ForeignKey(
                        name: "FK_processing_result_StatusFile_load_id",
                        column: x => x.load_id,
                        principalTable: "StatusFile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_load_id",
                table: "notification_outbox",
                column: "load_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processed_product_load_id",
                table: "processed_product",
                column: "load_id");

            migrationBuilder.CreateIndex(
                name: "IX_processing_failure_load_id_row_number_reason",
                table: "processing_failure",
                columns: new[] { "load_id", "row_number", "reason" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processing_period_load_id",
                table: "processing_period",
                column: "load_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_outbox");

            migrationBuilder.DropTable(
                name: "processed_product");

            migrationBuilder.DropTable(
                name: "processing_failure");

            migrationBuilder.DropTable(
                name: "processing_period");

            migrationBuilder.DropTable(
                name: "processing_result");
        }
    }
}
