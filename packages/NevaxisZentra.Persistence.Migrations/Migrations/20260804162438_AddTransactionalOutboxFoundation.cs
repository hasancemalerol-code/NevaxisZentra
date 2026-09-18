using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisZentra.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionalOutboxFoundation : Migration
    {
        private static readonly string[] MessageSchemaColumns = ["message_type", "schema_version"];
        private static readonly string[] StatusAvailableColumns = ["status", "available_at_utc"];
        private static readonly string[] TenantIdempotencyColumns = ["tenant_id", "idempotency_key"];
        private static readonly string[] TenantOccurredColumns = ["tenant_id", "occurred_at_utc"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    aggregate_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    causation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    headers_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    available_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lock_id = table.Column<Guid>(type: "uuid", nullable: true),
                    locked_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_messages_aggregate", "(aggregate_type IS NULL) = (aggregate_id IS NULL)");
                    table.CheckConstraint("ck_outbox_messages_attempts", "attempt_count >= 0");
                    table.CheckConstraint("ck_outbox_messages_correlation", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_outbox_messages_headers", "jsonb_typeof(headers_json) = 'object' AND octet_length(headers_json::text) <= 2048");
                    table.CheckConstraint("ck_outbox_messages_idempotency", "idempotency_key <> ''");
                    table.CheckConstraint("ck_outbox_messages_payload", "jsonb_typeof(payload_json) = 'object' AND octet_length(payload_json::text) <= 4096");
                    table.CheckConstraint("ck_outbox_messages_processing", "(status = 'Pending' AND lock_id IS NULL AND locked_until_utc IS NULL AND processed_at_utc IS NULL AND failed_at_utc IS NULL) OR (status = 'Processing' AND lock_id IS NOT NULL AND locked_until_utc IS NOT NULL AND last_attempt_at_utc IS NOT NULL AND processed_at_utc IS NULL AND failed_at_utc IS NULL) OR (status = 'Processed' AND lock_id IS NULL AND locked_until_utc IS NULL AND processed_at_utc IS NOT NULL AND failed_at_utc IS NULL) OR (status = 'Failed' AND lock_id IS NULL AND locked_until_utc IS NULL AND processed_at_utc IS NULL AND failed_at_utc IS NOT NULL AND failure_code IS NOT NULL)");
                    table.CheckConstraint("ck_outbox_messages_schema", "schema_version = 1");
                    table.CheckConstraint("ck_outbox_messages_status", "status IN ('Pending', 'Processing', 'Processed', 'Failed')");
                    table.CheckConstraint("ck_outbox_messages_timestamps", "available_at_utc >= occurred_at_utc AND recorded_at_utc >= occurred_at_utc");
                    table.CheckConstraint("ck_outbox_messages_type", "message_type IN ('infrastructure.audit.recorded', 'infrastructure.ai-decision.recorded', 'infrastructure.outbox.probe')");
                    table.ForeignKey(
                        name: "FK_outbox_messages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_correlation_id",
                table: "outbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_locked_until_utc",
                table: "outbox_messages",
                column: "locked_until_utc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_message_type_schema_version",
                table: "outbox_messages",
                columns: MessageSchemaColumns);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_status_available_at_utc",
                table: "outbox_messages",
                columns: StatusAvailableColumns);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_idempotency_key",
                table: "outbox_messages",
                columns: TenantIdempotencyColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_occurred_at_utc",
                table: "outbox_messages",
                columns: TenantOccurredColumns);

            migrationBuilder.Sql(
                """
                INSERT INTO permission_definitions (id, key, description, status, created_at_utc)
                VALUES
                    ('10000000-0000-4000-8000-000000000007', 'system.outbox.write', 'Append bounded tenant-scoped infrastructure outbox messages.', 'Active', '2026-08-04T12:00:00Z'),
                    ('10000000-0000-4000-8000-000000000008', 'system.outbox.read', 'Read recent tenant-scoped infrastructure outbox summaries.', 'Active', '2026-08-04T12:00:00Z');

                CREATE FUNCTION append_outbox_message(
                    message_id_value uuid, tenant_value uuid, identity_subject_value uuid,
                    tenant_membership_value uuid, message_type_value text, schema_version_value integer,
                    aggregate_type_value text, aggregate_id_value text, correlation_value uuid,
                    causation_value uuid, idempotency_key_value text, payload_value jsonb,
                    headers_value jsonb, occurred_at_value timestamp with time zone,
                    available_at_value timestamp with time zone)
                RETURNS TABLE(message_id uuid, was_duplicate boolean)
                LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path = pg_catalog
                AS $function$
                DECLARE existing_id uuid;
                BEGIN
                    IF public.current_tenant_id() IS DISTINCT FROM tenant_value THEN
                        RAISE EXCEPTION 'Outbox tenant context mismatch.' USING ERRCODE = '42501';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM public.identity_subjects AS identity_subject
                        INNER JOIN public.tenant_memberships AS membership
                            ON membership.identity_subject_id = identity_subject.id
                           AND membership.tenant_id = tenant_value
                        INNER JOIN public.tenants AS tenant ON tenant.id = membership.tenant_id
                        WHERE identity_subject.id = identity_subject_value
                          AND identity_subject.status = 'Active'
                          AND membership.id = tenant_membership_value
                          AND membership.status = 'Active'
                          AND tenant.status = 'Active') THEN
                        RAISE EXCEPTION 'Outbox actor context is invalid.' USING ERRCODE = '42501';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM public.tenant_membership_roles AS membership_role
                        INNER JOIN public.tenant_roles AS tenant_role
                            ON tenant_role.id = membership_role.tenant_role_id
                           AND tenant_role.tenant_id = tenant_value
                           AND tenant_role.status = 'Active'
                        INNER JOIN public.tenant_role_permissions AS role_permission
                            ON role_permission.tenant_role_id = tenant_role.id
                           AND role_permission.tenant_id = tenant_value
                        INNER JOIN public.permission_definitions AS permission_definition
                            ON permission_definition.id = role_permission.permission_definition_id
                           AND permission_definition.key = 'system.outbox.write'
                           AND permission_definition.status = 'Active'
                        WHERE membership_role.tenant_id = tenant_value
                          AND membership_role.tenant_membership_id = tenant_membership_value
                          AND membership_role.status = 'Active') THEN
                        RAISE EXCEPTION 'Outbox write permission is not assigned.' USING ERRCODE = '42501';
                    END IF;

                    SELECT id INTO existing_id FROM public.outbox_messages
                    WHERE tenant_id = tenant_value AND idempotency_key = idempotency_key_value;
                    IF existing_id IS NOT NULL THEN
                        RETURN QUERY SELECT existing_id, true;
                        RETURN;
                    END IF;

                    INSERT INTO public.outbox_messages (
                        id, tenant_id, message_type, schema_version, aggregate_type, aggregate_id,
                        correlation_id, causation_id, idempotency_key, payload_json, headers_json,
                        status, occurred_at_utc, recorded_at_utc, available_at_utc, attempt_count)
                    VALUES (
                        message_id_value, tenant_value, message_type_value, schema_version_value,
                        aggregate_type_value, aggregate_id_value, correlation_value, causation_value,
                        idempotency_key_value, payload_value, headers_value, 'Pending', occurred_at_value,
                        clock_timestamp(), available_at_value, 0)
                    ON CONFLICT (tenant_id, idempotency_key) DO NOTHING
                    RETURNING id INTO existing_id;

                    IF existing_id IS NULL THEN
                        SELECT id INTO existing_id FROM public.outbox_messages
                        WHERE tenant_id = tenant_value AND idempotency_key = idempotency_key_value;
                        RETURN QUERY SELECT existing_id, true;
                    ELSE
                        RETURN QUERY SELECT existing_id, false;
                    END IF;
                END
                $function$;

                CREATE FUNCTION claim_outbox_messages(
                    tenant_value uuid, lock_value uuid, now_value timestamp with time zone,
                    locked_until_value timestamp with time zone, batch_size_value integer)
                RETURNS TABLE(message_id uuid)
                LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path = pg_catalog
                AS $function$
                BEGIN
                    IF public.current_tenant_id() IS DISTINCT FROM tenant_value THEN
                        RAISE EXCEPTION 'Outbox tenant context mismatch.' USING ERRCODE = '42501';
                    END IF;
                    IF lock_value = '00000000-0000-0000-0000-000000000000'::uuid
                       OR locked_until_value <= now_value OR batch_size_value NOT BETWEEN 1 AND 100 THEN
                        RAISE EXCEPTION 'Outbox lease parameters are invalid.' USING ERRCODE = '22023';
                    END IF;

                    RETURN QUERY
                    WITH eligible AS (
                        SELECT id FROM public.outbox_messages
                        WHERE tenant_id = tenant_value
                          AND ((status = 'Pending' AND available_at_utc <= now_value)
                            OR (status = 'Processing' AND locked_until_utc < now_value))
                        ORDER BY available_at_utc, occurred_at_utc, id
                        LIMIT batch_size_value
                        FOR UPDATE SKIP LOCKED)
                    UPDATE public.outbox_messages AS message
                    SET status = 'Processing', lock_id = lock_value,
                        locked_until_utc = locked_until_value,
                        attempt_count = message.attempt_count + 1,
                        last_attempt_at_utc = now_value,
                        processed_at_utc = NULL, failed_at_utc = NULL, failure_code = NULL
                    FROM eligible WHERE message.id = eligible.id
                    RETURNING message.id;
                END
                $function$;

                CREATE FUNCTION mark_outbox_message_processed(
                    tenant_value uuid, message_id_value uuid, lock_value uuid,
                    processed_at_value timestamp with time zone)
                RETURNS boolean LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path = pg_catalog
                AS $function$
                DECLARE changed integer;
                BEGIN
                    IF public.current_tenant_id() IS DISTINCT FROM tenant_value THEN
                        RAISE EXCEPTION 'Outbox tenant context mismatch.' USING ERRCODE = '42501';
                    END IF;
                    UPDATE public.outbox_messages SET status = 'Processed', processed_at_utc = processed_at_value,
                        lock_id = NULL, locked_until_utc = NULL, failure_code = NULL
                    WHERE id = message_id_value AND tenant_id = tenant_value
                      AND status = 'Processing' AND lock_id = lock_value;
                    GET DIAGNOSTICS changed = ROW_COUNT;
                    RETURN changed = 1;
                END
                $function$;

                CREATE FUNCTION mark_outbox_message_failed(
                    tenant_value uuid, message_id_value uuid, lock_value uuid,
                    failed_at_value timestamp with time zone, failure_code_value text)
                RETURNS boolean LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path = pg_catalog
                AS $function$
                DECLARE changed integer;
                BEGIN
                    IF public.current_tenant_id() IS DISTINCT FROM tenant_value THEN
                        RAISE EXCEPTION 'Outbox tenant context mismatch.' USING ERRCODE = '42501';
                    END IF;
                    IF failure_code_value !~ '^[a-z0-9][a-z0-9._-]*$' OR length(failure_code_value) > 100 THEN
                        RAISE EXCEPTION 'Outbox failure code is invalid.' USING ERRCODE = '22023';
                    END IF;
                    UPDATE public.outbox_messages SET status = 'Failed', failed_at_utc = failed_at_value,
                        lock_id = NULL, locked_until_utc = NULL, failure_code = failure_code_value
                    WHERE id = message_id_value AND tenant_id = tenant_value
                      AND status = 'Processing' AND lock_id = lock_value;
                    GET DIAGNOSTICS changed = ROW_COUNT;
                    RETURN changed = 1;
                END
                $function$;

                CREATE FUNCTION enforce_outbox_message_immutability()
                RETURNS trigger LANGUAGE plpgsql VOLATILE SECURITY INVOKER SET search_path = pg_catalog
                AS $function$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Outbox messages cannot be deleted.' USING ERRCODE = '42501';
                    END IF;
                    IF NEW.id IS DISTINCT FROM OLD.id OR NEW.tenant_id IS DISTINCT FROM OLD.tenant_id
                       OR NEW.message_type IS DISTINCT FROM OLD.message_type
                       OR NEW.schema_version IS DISTINCT FROM OLD.schema_version
                       OR NEW.aggregate_type IS DISTINCT FROM OLD.aggregate_type
                       OR NEW.aggregate_id IS DISTINCT FROM OLD.aggregate_id
                       OR NEW.correlation_id IS DISTINCT FROM OLD.correlation_id
                       OR NEW.causation_id IS DISTINCT FROM OLD.causation_id
                       OR NEW.idempotency_key IS DISTINCT FROM OLD.idempotency_key
                       OR NEW.payload_json IS DISTINCT FROM OLD.payload_json
                       OR NEW.headers_json IS DISTINCT FROM OLD.headers_json
                       OR NEW.occurred_at_utc IS DISTINCT FROM OLD.occurred_at_utc
                       OR NEW.recorded_at_utc IS DISTINCT FROM OLD.recorded_at_utc
                       OR NEW.available_at_utc IS DISTINCT FROM OLD.available_at_utc THEN
                        RAISE EXCEPTION 'Outbox envelope fields are immutable.' USING ERRCODE = '42501';
                    END IF;
                    RETURN NEW;
                END
                $function$;

                CREATE TRIGGER outbox_messages_enforce_immutability
                    BEFORE UPDATE OR DELETE ON outbox_messages
                    FOR EACH ROW EXECUTE FUNCTION enforce_outbox_message_immutability();

                REVOKE ALL ON TABLE outbox_messages FROM crevafactos_runtime;
                GRANT SELECT ON TABLE outbox_messages TO crevafactos_runtime;
                REVOKE ALL ON FUNCTION append_outbox_message(uuid, uuid, uuid, uuid, text, integer, text, text, uuid, uuid, text, jsonb, jsonb, timestamp with time zone, timestamp with time zone) FROM PUBLIC;
                REVOKE ALL ON FUNCTION claim_outbox_messages(uuid, uuid, timestamp with time zone, timestamp with time zone, integer) FROM PUBLIC;
                REVOKE ALL ON FUNCTION mark_outbox_message_processed(uuid, uuid, uuid, timestamp with time zone) FROM PUBLIC;
                REVOKE ALL ON FUNCTION mark_outbox_message_failed(uuid, uuid, uuid, timestamp with time zone, text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION append_outbox_message(uuid, uuid, uuid, uuid, text, integer, text, text, uuid, uuid, text, jsonb, jsonb, timestamp with time zone, timestamp with time zone) TO crevafactos_runtime;
                GRANT EXECUTE ON FUNCTION claim_outbox_messages(uuid, uuid, timestamp with time zone, timestamp with time zone, integer) TO crevafactos_runtime;
                GRANT EXECUTE ON FUNCTION mark_outbox_message_processed(uuid, uuid, uuid, timestamp with time zone) TO crevafactos_runtime;
                GRANT EXECUTE ON FUNCTION mark_outbox_message_failed(uuid, uuid, uuid, timestamp with time zone, text) TO crevafactos_runtime;

                ALTER TABLE outbox_messages ENABLE ROW LEVEL SECURITY;
                ALTER TABLE outbox_messages FORCE ROW LEVEL SECURITY;
                CREATE POLICY outbox_messages_select_current ON outbox_messages FOR SELECT TO crevafactos_runtime USING (tenant_id = current_tenant_id());
                CREATE POLICY outbox_messages_insert_current ON outbox_messages FOR INSERT TO crevafactos_runtime WITH CHECK (tenant_id = current_tenant_id());
                CREATE POLICY outbox_messages_update_current ON outbox_messages FOR UPDATE TO crevafactos_runtime USING (tenant_id = current_tenant_id()) WITH CHECK (tenant_id = current_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER outbox_messages_enforce_immutability ON outbox_messages;
                DROP FUNCTION enforce_outbox_message_immutability();
                DROP FUNCTION mark_outbox_message_failed(uuid, uuid, uuid, timestamp with time zone, text);
                DROP FUNCTION mark_outbox_message_processed(uuid, uuid, uuid, timestamp with time zone);
                DROP FUNCTION claim_outbox_messages(uuid, uuid, timestamp with time zone, timestamp with time zone, integer);
                DROP FUNCTION append_outbox_message(uuid, uuid, uuid, uuid, text, integer, text, text, uuid, uuid, text, jsonb, jsonb, timestamp with time zone, timestamp with time zone);
                """);

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.Sql(
                "DELETE FROM permission_definitions WHERE id IN ('10000000-0000-4000-8000-000000000007', '10000000-0000-4000-8000-000000000008');");
        }
    }
}
