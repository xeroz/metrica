using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Processing.Worker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_processed_product",
                table: "processed_product");

            migrationBuilder.DropIndex(
                name: "IX_processed_product_load_id",
                table: "processed_product");

            migrationBuilder.AddColumn<long>(
                name: "id",
                table: "processed_product",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_processed_product",
                table: "processed_product",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_processed_product_load_id_row_number",
                table: "processed_product",
                columns: new[] { "load_id", "row_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processed_product_period_code_name_price",
                table: "processed_product",
                columns: new[] { "period", "code", "name", "price" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_processed_product",
                table: "processed_product");

            migrationBuilder.DropIndex(
                name: "IX_processed_product_load_id_row_number",
                table: "processed_product");

            migrationBuilder.DropIndex(
                name: "IX_processed_product_period_code_name_price",
                table: "processed_product");

            migrationBuilder.DropColumn(
                name: "id",
                table: "processed_product");

            migrationBuilder.AddPrimaryKey(
                name: "PK_processed_product",
                table: "processed_product",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "IX_processed_product_load_id",
                table: "processed_product",
                column: "load_id");
        }
    }
}
