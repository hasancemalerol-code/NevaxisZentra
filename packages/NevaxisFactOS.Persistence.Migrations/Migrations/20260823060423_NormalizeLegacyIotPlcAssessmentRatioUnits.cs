using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeLegacyIotPlcAssessmentRatioUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $repair$
                DECLARE
                    ambiguous_signal_count integer;
                    ambiguous_connectivity_count integer;
                    violating_signal_count integer;
                    violating_connectivity_count integer;
                BEGIN
                    SELECT COUNT(*) INTO ambiguous_signal_count
                    FROM iot_plc_signal_quality_assessments s
                    WHERE GREATEST(
                            COALESCE(s.good_ratio, -1),
                            COALESCE(s.uncertain_ratio, -1),
                            COALESCE(s.bad_ratio, -1),
                            COALESCE(s.stale_ratio, -1),
                            COALESCE(s.source_conflict_ratio, -1)) > 1
                      AND NOT (
                            GREATEST(
                                COALESCE(s.good_ratio, -1),
                                COALESCE(s.uncertain_ratio, -1),
                                COALESCE(s.bad_ratio, -1),
                                COALESCE(s.stale_ratio, -1),
                                COALESCE(s.source_conflict_ratio, -1)) <= 100
                            AND LEAST(
                                CASE WHEN s.good_ratio IS NULL THEN 999 ELSE s.good_ratio END,
                                CASE WHEN s.uncertain_ratio IS NULL THEN 999 ELSE s.uncertain_ratio END,
                                CASE WHEN s.bad_ratio IS NULL THEN 999 ELSE s.bad_ratio END,
                                CASE WHEN s.stale_ratio IS NULL THEN 999 ELSE s.stale_ratio END,
                                CASE WHEN s.source_conflict_ratio IS NULL THEN 999 ELSE s.source_conflict_ratio END) >= 0
                            AND ABS(
                                COALESCE(s.good_ratio, 0)
                                + COALESCE(s.uncertain_ratio, 0)
                                + COALESCE(s.bad_ratio, 0)
                                + COALESCE(s.stale_ratio, 0)
                                + COALESCE(s.source_conflict_ratio, 0) - 100) <= 0.01
                            AND NOT (
                                (COALESCE(s.good_ratio, 0) > 0 AND COALESCE(s.good_ratio, 0) <= 1)
                                OR (COALESCE(s.uncertain_ratio, 0) > 0 AND COALESCE(s.uncertain_ratio, 0) <= 1)
                                OR (COALESCE(s.bad_ratio, 0) > 0 AND COALESCE(s.bad_ratio, 0) <= 1)
                                OR (COALESCE(s.stale_ratio, 0) > 0 AND COALESCE(s.stale_ratio, 0) <= 1)
                                OR (COALESCE(s.source_conflict_ratio, 0) > 0 AND COALESCE(s.source_conflict_ratio, 0) <= 1)));

                    IF ambiguous_signal_count > 0 THEN
                        RAISE EXCEPTION 'IOT-PLC-2 legacy ratio repair blocked: % ambiguous signal-quality assessment rows remain.', ambiguous_signal_count;
                    END IF;

                    SELECT COUNT(*) INTO ambiguous_connectivity_count
                    FROM iot_plc_connectivity_assessments c
                    WHERE c.health_observation_availability_ratio IS NOT NULL
                      AND (
                            c.health_observation_availability_ratio < 0
                            OR c.health_observation_availability_ratio > 100);

                    IF ambiguous_connectivity_count > 0 THEN
                        RAISE EXCEPTION 'IOT-PLC-2 legacy ratio repair blocked: % ambiguous connectivity assessment rows remain.', ambiguous_connectivity_count;
                    END IF;

                    UPDATE iot_plc_signal_quality_assessments s
                    SET
                        good_ratio = CASE WHEN s.good_ratio IS NULL THEN NULL ELSE s.good_ratio / 100 END,
                        uncertain_ratio = CASE WHEN s.uncertain_ratio IS NULL THEN NULL ELSE s.uncertain_ratio / 100 END,
                        bad_ratio = CASE WHEN s.bad_ratio IS NULL THEN NULL ELSE s.bad_ratio / 100 END,
                        stale_ratio = CASE WHEN s.stale_ratio IS NULL THEN NULL ELSE s.stale_ratio / 100 END,
                        source_conflict_ratio = CASE WHEN s.source_conflict_ratio IS NULL THEN NULL ELSE s.source_conflict_ratio / 100 END
                    WHERE GREATEST(
                            COALESCE(s.good_ratio, -1),
                            COALESCE(s.uncertain_ratio, -1),
                            COALESCE(s.bad_ratio, -1),
                            COALESCE(s.stale_ratio, -1),
                            COALESCE(s.source_conflict_ratio, -1)) > 1
                      AND GREATEST(
                            COALESCE(s.good_ratio, -1),
                            COALESCE(s.uncertain_ratio, -1),
                            COALESCE(s.bad_ratio, -1),
                            COALESCE(s.stale_ratio, -1),
                            COALESCE(s.source_conflict_ratio, -1)) <= 100
                      AND LEAST(
                            CASE WHEN s.good_ratio IS NULL THEN 999 ELSE s.good_ratio END,
                            CASE WHEN s.uncertain_ratio IS NULL THEN 999 ELSE s.uncertain_ratio END,
                            CASE WHEN s.bad_ratio IS NULL THEN 999 ELSE s.bad_ratio END,
                            CASE WHEN s.stale_ratio IS NULL THEN 999 ELSE s.stale_ratio END,
                            CASE WHEN s.source_conflict_ratio IS NULL THEN 999 ELSE s.source_conflict_ratio END) >= 0
                      AND ABS(
                            COALESCE(s.good_ratio, 0)
                            + COALESCE(s.uncertain_ratio, 0)
                            + COALESCE(s.bad_ratio, 0)
                            + COALESCE(s.stale_ratio, 0)
                            + COALESCE(s.source_conflict_ratio, 0) - 100) <= 0.01
                      AND NOT (
                            (COALESCE(s.good_ratio, 0) > 0 AND COALESCE(s.good_ratio, 0) <= 1)
                            OR (COALESCE(s.uncertain_ratio, 0) > 0 AND COALESCE(s.uncertain_ratio, 0) <= 1)
                            OR (COALESCE(s.bad_ratio, 0) > 0 AND COALESCE(s.bad_ratio, 0) <= 1)
                            OR (COALESCE(s.stale_ratio, 0) > 0 AND COALESCE(s.stale_ratio, 0) <= 1)
                            OR (COALESCE(s.source_conflict_ratio, 0) > 0 AND COALESCE(s.source_conflict_ratio, 0) <= 1));

                    UPDATE iot_plc_connectivity_assessments c
                    SET health_observation_availability_ratio = c.health_observation_availability_ratio / 100
                    WHERE c.health_observation_availability_ratio > 1
                      AND c.health_observation_availability_ratio <= 100;

                    SELECT COUNT(*) INTO violating_signal_count
                    FROM iot_plc_signal_quality_assessments s
                    WHERE GREATEST(
                            COALESCE(s.good_ratio, -1),
                            COALESCE(s.uncertain_ratio, -1),
                            COALESCE(s.bad_ratio, -1),
                            COALESCE(s.stale_ratio, -1),
                            COALESCE(s.source_conflict_ratio, -1)) > 1;

                    SELECT COUNT(*) INTO violating_connectivity_count
                    FROM iot_plc_connectivity_assessments c
                    WHERE c.health_observation_availability_ratio > 1;

                    IF violating_signal_count > 0 OR violating_connectivity_count > 0 THEN
                        RAISE EXCEPTION 'IOT-PLC-2 legacy ratio repair blocked: % signal and % connectivity rows still exceed 0-1 contract.',
                            violating_signal_count, violating_connectivity_count;
                    END IF;
                END
                $repair$;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_iot_plc_signal_quality_assessments_bad_ratio",
                table: "iot_plc_signal_quality_assessments",
                sql: "bad_ratio IS NULL OR (bad_ratio >= 0 AND bad_ratio <= 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_iot_plc_signal_quality_assessments_good_ratio",
                table: "iot_plc_signal_quality_assessments",
                sql: "good_ratio IS NULL OR (good_ratio >= 0 AND good_ratio <= 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_iot_plc_signal_quality_assessments_source_conflict_ratio",
                table: "iot_plc_signal_quality_assessments",
                sql: "source_conflict_ratio IS NULL OR (source_conflict_ratio >= 0 AND source_conflict_ratio <= 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_iot_plc_signal_quality_assessments_stale_ratio",
                table: "iot_plc_signal_quality_assessments",
                sql: "stale_ratio IS NULL OR (stale_ratio >= 0 AND stale_ratio <= 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_iot_plc_signal_quality_assessments_uncertain_ratio",
                table: "iot_plc_signal_quality_assessments",
                sql: "uncertain_ratio IS NULL OR (uncertain_ratio >= 0 AND uncertain_ratio <= 1)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_iot_plc_connectivity_assessments_health_observation_availab~",
                table: "iot_plc_connectivity_assessments",
                sql: "health_observation_availability_ratio IS NULL OR (health_observation_availability_ratio >= 0 AND health_observation_availability_ratio <= 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_iot_plc_signal_quality_assessments_bad_ratio",
                table: "iot_plc_signal_quality_assessments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_iot_plc_signal_quality_assessments_good_ratio",
                table: "iot_plc_signal_quality_assessments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_iot_plc_signal_quality_assessments_source_conflict_ratio",
                table: "iot_plc_signal_quality_assessments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_iot_plc_signal_quality_assessments_stale_ratio",
                table: "iot_plc_signal_quality_assessments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_iot_plc_signal_quality_assessments_uncertain_ratio",
                table: "iot_plc_signal_quality_assessments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_iot_plc_connectivity_assessments_health_observation_availab~",
                table: "iot_plc_connectivity_assessments");
        }
    }
}
