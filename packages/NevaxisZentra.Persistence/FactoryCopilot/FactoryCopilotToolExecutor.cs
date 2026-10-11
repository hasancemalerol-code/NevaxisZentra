using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.ContextKnowledge;
using NevaxisZentra.Persistence.ContinuousOptimization;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.MixedProduction;
using NevaxisZentra.Persistence.ScenarioRecovery;
using NevaxisZentra.Persistence.ThroughputIntelligence;

namespace NevaxisZentra.Persistence.FactoryCopilot;

internal sealed class FactoryCopilotToolExecutor(
    NevaxisZentraDbContext dbContext,
    IContextKnowledgeService contextKnowledge,
    IThroughputIntelligenceService throughputIntelligence,
    IScenarioRecoveryService scenarioRecovery,
    IDigitalWorkforceWorksService worksService,
    IContinuousOptimizationService continuousOptimization)
{
    public async Task<(FactoryCopilotToolBundle Bundle, IReadOnlyList<FactoryCopilotToolInvocation> Invocations)> ExecuteAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        string intent,
        IReadOnlyList<string> toolIds,
        FactoryCopilotContextInput context,
        FactoryCopilotResolvedContext resolved,
        string queryText,
        CancellationToken cancellationToken)
    {
        var bundle = new FactoryCopilotToolBundle();
        var invocations = new List<FactoryCopilotToolInvocation>();
        var knownTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var toolId in toolIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            FactoryCopilotToolRegistry.Require(toolId, intent);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                switch (toolId)
                {
                    case FactoryCopilotToolIds.GetLineState:
                        bundle = bundle with
                        {
                            LineState = await GetLineStateAsync(facilityId, tenantId, resolved, cancellationToken),
                        };
                        TrackKnownTokens(knownTokens, resolved.LineCode, resolved.LineName);
                        invocations.Add(Success(toolId, stopwatch, "Canonical line state resolved"));
                        break;
                    case FactoryCopilotToolIds.GetThroughputOverview:
                        bundle = bundle with
                        {
                            ThroughputOverview = await throughputIntelligence.GetLineOverviewAsync(
                                facilityId, resolved.LineId, "current-shift", null, null, cancellationToken),
                        };
                        TrackThroughputTokens(knownTokens, bundle.ThroughputOverview);
                        invocations.Add(Success(toolId, stopwatch, "MP2 throughput overview loaded"));
                        break;
                    case FactoryCopilotToolIds.GetCurrentConstraint:
                        if (bundle.ThroughputOverview is null)
                        {
                            bundle = bundle with
                            {
                                ThroughputOverview = await throughputIntelligence.GetLineOverviewAsync(
                                    facilityId, resolved.LineId, "current-shift", null, null, cancellationToken),
                            };
                        }
                        TrackThroughputTokens(knownTokens, bundle.ThroughputOverview);
                        invocations.Add(Success(toolId, stopwatch, "Current constraint extracted"));
                        break;
                    case FactoryCopilotToolIds.GetLossTree:
                        if (bundle.ThroughputOverview is null)
                        {
                            bundle = bundle with
                            {
                                ThroughputOverview = await throughputIntelligence.GetLineOverviewAsync(
                                    facilityId, resolved.LineId, "current-shift", null, null, cancellationToken),
                            };
                        }
                        TrackThroughputTokens(knownTokens, bundle.ThroughputOverview);
                        invocations.Add(Success(toolId, stopwatch, "Loss tree extracted"));
                        break;
                    case FactoryCopilotToolIds.GetScenarioComparison:
                        bundle = bundle with
                        {
                            ScenarioComparison = await throughputIntelligence.CompareScenariosAsync(
                                facilityId, resolved.LineId, cancellationToken),
                        };
                        if (context.ScenarioRunId.HasValue)
                        {
                            bundle = bundle with
                            {
                                ScenarioWorkspace = await scenarioRecovery.GetWorkspaceAsync(
                                    facilityId, context.ScenarioRunId.Value, cancellationToken),
                            };
                            TrackScenarioTokens(knownTokens, bundle.ScenarioWorkspace);
                        }
                        invocations.Add(Success(toolId, stopwatch, "Scenario comparison facts loaded"));
                        break;
                    case FactoryCopilotToolIds.GetWorkDetail:
                        if (context.DigitalWorkCaseId.HasValue)
                        {
                            bundle = bundle with
                            {
                                WorkDetail = await worksService.GetCaseDetailAsync(
                                    context.DigitalWorkCaseId.Value, subjectId, false, cancellationToken),
                            };
                            TrackWorkTokens(knownTokens, bundle.WorkDetail);
                        }
                        invocations.Add(Success(toolId, stopwatch, "Work projection loaded"));
                        break;
                    case FactoryCopilotToolIds.GetVerifiedKnowledge:
                        bundle = bundle with
                        {
                            VerifiedKnowledge = await contextKnowledge.ListVerifiedKnowledgeAsync(
                                facilityId, 10, cancellationToken),
                        };
                        invocations.Add(Success(toolId, stopwatch, "Verified knowledge retrieved"));
                        break;
                    case FactoryCopilotToolIds.GetRecoveryOptions:
                        var comparison = await throughputIntelligence.CompareScenariosAsync(
                            facilityId, resolved.LineId, cancellationToken);
                        bundle = bundle with
                        {
                            ScenarioComparison = comparison,
                            RecoveryOptions = BuildRecoveryOptions(comparison),
                        };
                        invocations.Add(Success(toolId, stopwatch, "Registered recovery options listed"));
                        break;
                    case FactoryCopilotToolIds.RunScenarioSimulation:
                        bundle = bundle with
                        {
                            SimulationSnapshot = await RunSimulationAsync(
                                facilityId, tenantId, subjectId, resolved.LineId, queryText, cancellationToken),
                        };
                        TrackSimulationTokens(knownTokens, bundle.SimulationSnapshot);
                        invocations.Add(Success(toolId, stopwatch, "Scenario simulation completed"));
                        break;
                    case FactoryCopilotToolIds.GetVerificationResult:
                        if (context.ScenarioRunId.HasValue)
                        {
                            bundle = bundle with
                            {
                                ScenarioWorkspace = await scenarioRecovery.GetWorkspaceAsync(
                                    facilityId, context.ScenarioRunId.Value, cancellationToken),
                            };
                            TrackScenarioTokens(knownTokens, bundle.ScenarioWorkspace);
                        }
                        else if (context.DigitalWorkCaseId.HasValue)
                        {
                            bundle = bundle with
                            {
                                WorkDetail = await worksService.GetCaseDetailAsync(
                                    context.DigitalWorkCaseId.Value, subjectId, false, cancellationToken),
                            };
                            TrackWorkTokens(knownTokens, bundle.WorkDetail);
                        }
                        invocations.Add(Success(toolId, stopwatch, "Verification outcome loaded"));
                        break;
                    case FactoryCopilotToolIds.GetShiftContext:
                        bundle = bundle with
                        {
                            ThroughputOverview = await throughputIntelligence.GetLineOverviewAsync(
                                facilityId, resolved.LineId, "current-shift", null, null, cancellationToken),
                            PriorShiftOverview = await throughputIntelligence.GetLineOverviewAsync(
                                facilityId, resolved.LineId, "prior-shift", null, null, cancellationToken),
                            ShiftSignals = BuildShiftSignals(
                                await throughputIntelligence.GetLineOverviewAsync(
                                    facilityId, resolved.LineId, "current-shift", null, null, cancellationToken),
                                await throughputIntelligence.GetLineOverviewAsync(
                                    facilityId, resolved.LineId, "prior-shift", null, null, cancellationToken)),
                        };
                        invocations.Add(Success(toolId, stopwatch, "Shift comparison loaded"));
                        break;
                    case FactoryCopilotToolIds.GetExecutiveSignals:
                        var overview = await throughputIntelligence.GetLineOverviewAsync(
                            facilityId, resolved.LineId, "current-shift", null, null, cancellationToken);
                        var workspace = await worksService.GetWorkspaceAsync(null, null, 20, subjectId, cancellationToken);
                        bundle = bundle with
                        {
                            ThroughputOverview = overview,
                            ExecutiveSignals = BuildExecutiveSignals(overview, workspace),
                        };
                        TrackThroughputTokens(knownTokens, overview);
                        invocations.Add(Success(toolId, stopwatch, "Executive signals compiled"));
                        break;
                    case FactoryCopilotToolIds.GetMachinePerformance:
                    {
                        var performance = await GetMachinePerformanceAsync(facilityId, resolved, cancellationToken);
                        bundle = bundle with { MachinePerformance = performance };
                        if (performance is not null)
                        {
                            TrackKnownTokens(knownTokens, performance.Machines.SelectMany(m => new[] { m.ResourceCode, m.ResourceName }).ToArray());
                        }

                        invocations.Add(Success(
                            toolId,
                            stopwatch,
                            performance is null ? "No machine events in the available period" : $"{performance.Machines.Count} machines aggregated"));
                        break;
                    }
                    case FactoryCopilotToolIds.GetOptimizationRun:
                    {
                        ContinuousOptimizationRunSummary? optimizationRun = null;
                        ContinuousOptimizationExplainability? optimizationExplainability = null;
                        var coRunQuery = dbContext.ContinuousOptimizationRunRecords.AsNoTracking()
                            .Where(r => r.FacilityId == facilityId);
                        if (context.DigitalWorkCaseId.HasValue)
                        {
                            coRunQuery = coRunQuery.Where(r => r.DigitalWorkCaseId == context.DigitalWorkCaseId);
                        }
                        else if (resolved.LineId != Guid.Empty)
                        {
                            coRunQuery = coRunQuery.Where(r => r.LineId == resolved.LineId);
                        }

                        var coRun = await coRunQuery
                            .OrderByDescending(r => r.UpdatedAtUtc)
                            .FirstOrDefaultAsync(cancellationToken);
                        if (coRun is not null)
                        {
                            optimizationRun = await continuousOptimization.GetRunAsync(
                                facilityId, coRun.Id, cancellationToken);
                            optimizationExplainability = await continuousOptimization.GetExplainabilityAsync(
                                facilityId, coRun.Id, cancellationToken);
                        }

                        bundle = bundle with
                        {
                            OptimizationRun = optimizationRun,
                            OptimizationExplainability = optimizationExplainability,
                        };
                        invocations.Add(Success(toolId, stopwatch, "Continuous optimization explainability loaded"));
                        break;
                    }
                    default:
                        throw new FactoryCopilotException(
                            FactoryCopilotFailureCodes.ToolUnauthorized,
                            $"Tool {toolId} is not implemented.");
                }
            }
            catch (FactoryCopilotException)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                invocations.Add(new FactoryCopilotToolInvocation(
                    toolId,
                    FactoryCopilotToolRegistry.Require(toolId, intent).AccessClass,
                    false,
                    ex.Message,
                    stopwatch.ElapsedMilliseconds));
            }
        }

        return (bundle with { KnownEntityTokens = knownTokens.ToList() }, invocations);
    }

    private async Task<CanonicalOperationalStateResult> GetLineStateAsync(
        Guid facilityId,
        Guid tenantId,
        FactoryCopilotResolvedContext resolved,
        CancellationToken cancellationToken)
    {
        var claims = await dbContext.CanonicalProductionEvents.AsNoTracking()
            .Where(e => e.FacilityId == facilityId && e.LineId == resolved.LineId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(5)
            .Select(e => new ContextSourceClaim(
                "production-event",
                e.Id.ToString(),
                e.EventType,
                e.OccurredAtUtc,
                e.ReceivedAtUtc,
                0.9m))
            .ToListAsync(cancellationToken);

        return await contextKnowledge.ResolveCanonicalStateAsync(
            facilityId,
            tenantId,
            ContextKnowledgeScopeEntityTypes.Line,
            resolved.LineId.ToString(),
            claims,
            cancellationToken);
    }

    private async Task<ScenarioMetricSnapshot> RunSimulationAsync(
        Guid facilityId,
        Guid tenantId,
        string subjectId,
        Guid lineId,
        string queryText,
        CancellationToken cancellationToken)
    {
        var normalized = queryText.ToLowerInvariant();
        var actionType = normalized.Contains("+1 operatör", StringComparison.Ordinal) ||
                         normalized.Contains("operatör koy", StringComparison.Ordinal)
            ? ScenarioRecoveryActionTypes.AddOperator
            : ScenarioRecoveryActionTypes.AddOperator;

        return await scenarioRecovery.SimulateActionAsync(
            facilityId,
            tenantId,
            lineId,
            new SimulateScenarioRecoveryActionRequest(actionType, "ST-30", null),
            subjectId,
            cancellationToken);
    }

    private static List<string> BuildRecoveryOptions(IReadOnlyList<ScenarioComparisonRow> rows) =>
        rows
            .Where(row => !string.Equals(row.Eligibility, "INELIGIBLE", StringComparison.OrdinalIgnoreCase))
            .Select(row =>
                $"{row.ScenarioCode}: {row.Title} (+{row.DeltaGoodThroughputPerHour:0.#}/saat, risk {row.OperationalRiskScore:0.##})")
            .ToList();

    private static IReadOnlyList<string> BuildShiftSignals(
        ThroughputIntelligenceOverview? current,
        ThroughputIntelligenceOverview? prior)
    {
        if (current is null || prior is null)
        {
            return ["Shift comparison requires both current and prior windows."];
        }

        var delta = current.Measurement.GoodUnitsPerHour - prior.Measurement.GoodUnitsPerHour;
        return
        [
            $"Current shift throughput: {current.Measurement.GoodUnitsPerHour:0.#}/saat",
            $"Prior shift throughput: {prior.Measurement.GoodUnitsPerHour:0.#}/saat",
            $"Delta: {delta:+#.#;-#.#;0}/saat",
            current.Constraints.FirstOrDefault(c => c.Role == "primary")?.StationCode is { } constraint
                ? $"Current constraint: {constraint}"
                : "Current constraint: unavailable",
        ];
    }

    private static List<string> BuildExecutiveSignals(
        ThroughputIntelligenceOverview overview,
        WorksWorkspaceSnapshot workspace)
    {
        var signals = new List<string>();
        if (overview.ProductionGap.GapPerHour > 0)
        {
            signals.Add(
                $"Throughput gap {overview.ProductionGap.GapPerHour:0.#}/saat on {overview.LineCode}");
        }

        var primaryConstraint = overview.Constraints.FirstOrDefault(c => c.Role == "primary");
        if (primaryConstraint?.StationCode is not null)
        {
            signals.Add($"Primary constraint: {primaryConstraint.StationCode} ({primaryConstraint.ConstraintType})");
        }

        var criticalWork = workspace.Items.Count(item =>
            item.Severity is "critical" or "high" || item.HasPendingApproval || item.IsBlocked);
        if (criticalWork > 0)
        {
            signals.Add($"{criticalWork} critical/blocked/pending-approval work items");
        }

        if (signals.Count == 0)
        {
            signals.Add("No elevated operational signals in bounded scan.");
        }

        return signals;
    }

    /// <summary>
    /// Aggregates the line's canonical events per machine: produced units (GOOD quantity) and stops (STOP minutes by
    /// reason). Aggregation happens in SQL and the result is bounded to the 10 most relevant machines.
    /// </summary>
    private async Task<FactoryCopilotMachinePerformance?> GetMachinePerformanceAsync(
        Guid facilityId,
        FactoryCopilotResolvedContext resolved,
        CancellationToken cancellationToken)
    {
        var events = dbContext.CanonicalProductionEvents.AsNoTracking()
            .Where(e => e.FacilityId == facilityId && e.LineId == resolved.LineId && e.ResourceId != null);

        var bounds = await events
            .GroupBy(_ => 1)
            .Select(g => new { From = g.Min(e => e.OccurredAtUtc), To = g.Max(e => e.OccurredAtUtc), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);
        if (bounds is null || bounds.Count == 0)
        {
            return null;
        }

        var produced = await events
            .Where(e => e.EventType == MixedProductionEventTypes.Good)
            .GroupBy(e => new { ResourceId = e.ResourceId!.Value, e.OperationCode })
            .Select(g => new { g.Key.ResourceId, g.Key.OperationCode, Units = g.Sum(e => e.Quantity ?? 0m) })
            .ToListAsync(cancellationToken);

        var stops = await events
            .Where(e => e.EventType == MixedProductionEventTypes.Stop)
            .GroupBy(e => new { ResourceId = e.ResourceId!.Value, e.OperationCode, Reason = e.ReasonCode ?? "unspecified" })
            .Select(g => new
            {
                g.Key.ResourceId,
                g.Key.OperationCode,
                g.Key.Reason,
                Count = g.Count(),
                Minutes = g.Sum(e => e.Quantity ?? 0m),
            })
            .ToListAsync(cancellationToken);

        // Working days: distinct calendar days with production inside a scheduled window (weekends and days without
        // scheduled production are not scheduled time).
        var scheduledStamps = await events
            .Where(e => e.EventType == MixedProductionEventTypes.Good && e.OperationCode == FactoryCalendarWindows.Scheduled)
            .Select(e => e.OccurredAtUtc)
            .Distinct()
            .Take(50_000)
            .ToListAsync(cancellationToken);
        var workingDays = scheduledStamps.Select(t => DateOnly.FromDateTime(t.UtcDateTime)).Distinct().Count();

        var resourceIds = produced.Select(p => p.ResourceId)
            .Concat(stops.Select(s => s.ResourceId))
            .Distinct()
            .ToList();
        var resources = await dbContext.ManufacturingResources.AsNoTracking()
            .Where(r => resourceIds.Contains(r.Id))
            .Select(r => new FactoryCopilotMachineIdentity(r.Id, r.Code, r.Name))
            .ToListAsync(cancellationToken);

        return FactoryCopilotMachineMetrics.Build(
            bounds.From,
            bounds.To,
            bounds.Count,
            workingDays,
            resources,
            produced.Select(p => new FactoryCopilotProducedRow(p.ResourceId, p.OperationCode, p.Units)).ToList(),
            stops.Select(s => new FactoryCopilotStopRow(s.ResourceId, s.OperationCode, s.Reason, s.Count, s.Minutes)).ToList());
    }

    private static FactoryCopilotToolInvocation Success(string toolId, Stopwatch stopwatch, string summary)
    {
        stopwatch.Stop();
        var definition = FactoryCopilotToolRegistry.List().First(t => t.ToolId == toolId);
        return new FactoryCopilotToolInvocation(
            toolId,
            definition.AccessClass,
            true,
            summary,
            stopwatch.ElapsedMilliseconds);
    }

    private static void TrackKnownTokens(ISet<string> tokens, params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                tokens.Add(value.Trim());
            }
        }
    }

    private static void TrackThroughputTokens(ISet<string> tokens, ThroughputIntelligenceOverview? overview)
    {
        if (overview is null)
        {
            return;
        }

        TrackKnownTokens(tokens, overview.LineCode, overview.LineName);
        foreach (var station in overview.Topology)
        {
            TrackKnownTokens(tokens, station.Code, station.Name);
        }
    }

    private static void TrackScenarioTokens(ISet<string> tokens, ScenarioRecoveryWorkspace? workspace)
    {
        if (workspace is null)
        {
            return;
        }

        TrackKnownTokens(tokens, workspace.LineCode, workspace.SelectedOptionLabel);
    }

    private static void TrackWorkTokens(ISet<string> tokens, WorksCaseDetailSnapshot? detail)
    {
        if (detail is null)
        {
            return;
        }

        TrackKnownTokens(tokens, detail.Summary.Id.ToString(), detail.Summary.Title);
    }

    private static void TrackSimulationTokens(ISet<string> tokens, ScenarioMetricSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        TrackKnownTokens(
            tokens,
            snapshot.ConstraintAfter?.StationCode,
            snapshot.ConstraintBefore?.StationCode);
    }
}

internal sealed record FactoryCopilotResolvedContext(
    Guid LineId,
    string LineCode,
    string LineName,
    string? ResourceCode,
    string? OrderReference);
