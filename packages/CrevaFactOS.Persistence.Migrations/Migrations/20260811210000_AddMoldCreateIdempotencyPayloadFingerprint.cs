using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMoldCreateIdempotencyPayloadFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE molds
                    ADD COLUMN IF NOT EXISTS create_idempotency_payload_fingerprint VARCHAR(64) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE molds
                    DROP COLUMN IF EXISTS create_idempotency_payload_fingerprint;
                """);
        }
    }
}
