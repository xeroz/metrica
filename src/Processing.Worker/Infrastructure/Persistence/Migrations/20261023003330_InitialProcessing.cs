using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Processing.Worker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "code",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_code", x => x.id);
                });

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
                name: "period",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_period", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processing_existing",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    load_id = table.Column<long>(type: "bigint", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_existing", x => x.id);
                    table.ForeignKey(
                        name: "FK_processing_existing_StatusFile_load_id",
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
                name: "processing_result",
                columns: table => new
                {
                    load_id = table.Column<long>(type: "bigint", nullable: false),
                    outcome = table.Column<string>(type: "text", nullable: false),
                    inserted_count = table.Column<int>(type: "integer", nullable: false),
                    existing_count = table.Column<int>(type: "integer", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    code_id = table.Column<long>(type: "bigint", nullable: false),
                    period_id = table.Column<long>(type: "bigint", nullable: false),
                    load_id = table.Column<long>(type: "bigint", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_StatusFile_load_id",
                        column: x => x.load_id,
                        principalTable: "StatusFile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_code_code_id",
                        column: x => x.code_id,
                        principalTable: "code",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_period_period_id",
                        column: x => x.period_id,
                        principalTable: "period",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_code_name",
                table: "code",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_load_id",
                table: "notification_outbox",
                column: "load_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_period_name",
                table: "period",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processing_existing_load_id_row_number",
                table: "processing_existing",
                columns: new[] { "load_id", "row_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processing_failure_load_id_row_number_reason",
                table: "processing_failure",
                columns: new[] { "load_id", "row_number", "reason" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_code_id",
                table: "product",
                column: "code_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_load_id_row_number",
                table: "product",
                columns: new[] { "load_id", "row_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_period_id_code_id_name_price",
                table: "product",
                columns: new[] { "period_id", "code_id", "name", "price" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_outbox");

            migrationBuilder.DropTable(
                name: "processing_existing");

            migrationBuilder.DropTable(
                name: "processing_failure");

            migrationBuilder.DropTable(
                name: "processing_result");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "code");

            migrationBuilder.DropTable(
                name: "period");
        }
    }
}
