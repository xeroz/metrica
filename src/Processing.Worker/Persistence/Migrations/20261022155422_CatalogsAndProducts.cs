using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Processing.Worker.Persistence.Migrations;

public partial class CatalogsAndProducts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE code (
                id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name varchar(255) NOT NULL,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL
            );
            CREATE UNIQUE INDEX "IX_code_name" ON code (name);

            CREATE TABLE period (
                id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name varchar(255) NOT NULL,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL
            );
            CREATE UNIQUE INDEX "IX_period_name" ON period (name);

            INSERT INTO code (name, created_at, updated_at)
            SELECT DISTINCT code, now(), now() FROM processed_product;
            INSERT INTO period (name, created_at, updated_at)
            SELECT name, now(), now()
            FROM (
                SELECT period AS name FROM processing_period
                UNION
                SELECT period AS name FROM processed_product
            ) AS existing_periods;

            ALTER TABLE processed_product RENAME TO product;
            ALTER TABLE product RENAME CONSTRAINT "PK_processed_product" TO "PK_product";
            ALTER TABLE product RENAME CONSTRAINT "FK_processed_product_StatusFile_load_id" TO "FK_product_StatusFile_load_id";
            ALTER INDEX "IX_processed_product_load_id_row_number" RENAME TO "IX_product_load_id_row_number";
            DROP INDEX "IX_processed_product_period_code_name_price";
            ALTER TABLE product ALTER COLUMN name TYPE varchar(255);
            ALTER TABLE product ADD COLUMN code_id bigint;
            ALTER TABLE product ADD COLUMN period_id bigint;
            ALTER TABLE product ADD COLUMN created_at timestamptz NOT NULL DEFAULT now();
            ALTER TABLE product ADD COLUMN updated_at timestamptz NOT NULL DEFAULT now();
            UPDATE product AS p SET code_id = c.id FROM code AS c WHERE p.code = c.name;
            UPDATE product AS p SET period_id = q.id FROM period AS q WHERE p.period = q.name;
            ALTER TABLE product ALTER COLUMN code_id SET NOT NULL;
            ALTER TABLE product ALTER COLUMN period_id SET NOT NULL;
            ALTER TABLE product ALTER COLUMN created_at DROP DEFAULT;
            ALTER TABLE product ALTER COLUMN updated_at DROP DEFAULT;
            ALTER TABLE product DROP COLUMN code;
            ALTER TABLE product DROP COLUMN period;
            CREATE INDEX "IX_product_code_id" ON product (code_id);
            CREATE UNIQUE INDEX "IX_product_period_id_code_id_name_price"
                ON product (period_id, code_id, name, price);
            ALTER TABLE product ADD CONSTRAINT "FK_product_code_code_id"
                FOREIGN KEY (code_id) REFERENCES code (id) ON DELETE RESTRICT;
            ALTER TABLE product ADD CONSTRAINT "FK_product_period_period_id"
                FOREIGN KEY (period_id) REFERENCES period (id) ON DELETE RESTRICT;
            DROP TABLE processing_period;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Reverting this migration would discard catalog relationships and reused periods.");
    }
}
