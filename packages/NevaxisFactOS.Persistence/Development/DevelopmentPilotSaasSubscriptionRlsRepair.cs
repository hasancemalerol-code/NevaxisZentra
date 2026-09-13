using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Development;

internal static class DevelopmentPilotSaasSubscriptionRlsRepair
{
    private static readonly (string Table, string Prefix)[] TenantScopedTables =
    [
        ("saas_tenant_subscriptions", "saas_tenant_subscriptions"),
        ("production_manager_policies", "pmp"),
        ("factory_state_snapshots", "fss"),
        ("factory_predictions", "fp"),
        ("factory_decisions", "fd"),
        ("factory_decision_options", "fdo"),
        ("factory_simulations", "fsim"),
        ("factory_decision_outcomes", "fout"),
        ("factory_decision_memories", "fmem"),
        ("digital_workers", "dw"),
        ("digital_worker_policies", "dwp"),
        ("digital_worker_budgets", "dwb"),
        ("digital_work_cases", "dwc"),
        ("digital_work_case_participants", "dwcp"),
        ("digital_work_activities", "dwa"),
        ("digital_work_verifications", "dwv"),
        ("digital_work_approvals", "dwap"),
        ("digital_work_handoffs", "dwh"),
        ("digital_worker_usages", "dwu"),
        ("digital_workforce_runtime_states", "dwrs"),
        ("factory_case_dependencies", "fcd"),
        ("factory_event_correlations", "fec"),
    ];

    internal static async Task EnsureRuntimeReadAccessAsync(
        NevaxisFactOSDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var repairSql = """
            CREATE OR REPLACE FUNCTION current_tenant_id()
            RETURNS uuid
            LANGUAGE sql
            STABLE
            PARALLEL SAFE
            AS $function$
                SELECT NULLIF(
                    COALESCE(
                        NULLIF(current_setting('NevaxisFactOS.tenant_id', true), ''),
                        NULLIF(current_setting('CrevaFactOS.tenant_id', true), '')),
                    '')::uuid
            $function$;

            REVOKE ALL ON FUNCTION current_tenant_id() FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION current_tenant_id() TO crevafactos_runtime;

            GRANT SELECT, INSERT, UPDATE ON TABLE saas_tenant_subscriptions TO crevafactos_runtime;
            GRANT SELECT, INSERT ON TABLE saas_tenant_subscription_transition_history TO crevafactos_runtime;
            GRANT SELECT, INSERT ON TABLE saas_subscription_audit_events TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE production_manager_policies TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE factory_state_snapshots TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE factory_predictions TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE factory_decisions TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE factory_decision_options TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE factory_simulations TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE factory_decision_outcomes TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE factory_decision_memories TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE digital_workers TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE digital_worker_policies TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE digital_worker_budgets TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE digital_work_cases TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE digital_work_case_participants TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE digital_work_activities TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE digital_work_verifications TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE digital_work_approvals TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE digital_work_handoffs TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE digital_worker_usages TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE digital_workforce_runtime_states TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE factory_case_dependencies TO crevafactos_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE factory_event_correlations TO crevafactos_runtime;
            """;

        foreach (var (table, prefix) in TenantScopedTables)
        {
            repairSql += BuildTenantPolicyRepairSql(table, prefix);
        }

        repairSql += """
            DROP POLICY IF EXISTS saas_tenant_subscriptions_select_current ON saas_tenant_subscriptions;
            CREATE POLICY saas_tenant_subscriptions_select_current
                ON saas_tenant_subscriptions FOR SELECT TO crevafactos_runtime
                USING (tenant_id = current_tenant_id());

            DROP POLICY IF EXISTS saas_tenant_subscriptions_insert_current ON saas_tenant_subscriptions;
            CREATE POLICY saas_tenant_subscriptions_insert_current
                ON saas_tenant_subscriptions FOR INSERT TO crevafactos_runtime
                WITH CHECK (tenant_id = current_tenant_id());

            DROP POLICY IF EXISTS saas_tenant_subscriptions_update_current ON saas_tenant_subscriptions;
            CREATE POLICY saas_tenant_subscriptions_update_current
                ON saas_tenant_subscriptions FOR UPDATE TO crevafactos_runtime
                USING (tenant_id = current_tenant_id())
                WITH CHECK (tenant_id = current_tenant_id());
            """;

        await dbContext.Database.ExecuteSqlRawAsync(repairSql, cancellationToken);
    }

    private static string BuildTenantPolicyRepairSql(string tableName, string prefix) =>
        $"""

            DROP POLICY IF EXISTS rls_{prefix}_tenant_select ON {tableName};
            CREATE POLICY rls_{prefix}_tenant_select
                ON {tableName} FOR SELECT TO crevafactos_runtime
                USING (tenant_id = current_tenant_id());

            DROP POLICY IF EXISTS rls_{prefix}_tenant_insert ON {tableName};
            CREATE POLICY rls_{prefix}_tenant_insert
                ON {tableName} FOR INSERT TO crevafactos_runtime
                WITH CHECK (tenant_id = current_tenant_id());

            DROP POLICY IF EXISTS rls_{prefix}_tenant_update ON {tableName};
            CREATE POLICY rls_{prefix}_tenant_update
                ON {tableName} FOR UPDATE TO crevafactos_runtime
                USING (tenant_id = current_tenant_id())
                WITH CHECK (tenant_id = current_tenant_id());
            """;
}
