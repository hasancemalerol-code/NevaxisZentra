namespace NevaxisFactOS.Persistence.Production;

/// <summary>
/// Boundary contract marking the future seam between Production Execution and the Machine/Edge bounded context.
///
/// Architectural constraint (4L):
///   - Production Execution owns: production state, lifecycle, human execution records (downtime, setup, cycles),
///     exceptions, and operational analytics.
///   - Machine/Edge bounded context will own: device connectivity, PLC/OPC-UA/MQTT/Modbus adapters,
///     machine telemetry acquisition, automatic cycle detection, and machine command safety.
///
/// This interface is intentionally minimal in PRODUCTION-4L.
/// Concrete implementation belongs to a future Machine/Edge package.
/// Production must NEVER directly command machines — all machine commands must flow through the
/// Machine/Edge bounded context with its own authorization and safety checks.
///
/// Extension seam: when Machine/Edge package is implemented, methods such as the following may be added:
///   - ReportMachineStateAsync(...)  — machine signals machine state to Production
///   - ReportCycleSignalAsync(...)   — machine reports cycle completion
///   - ReportDowntimeSignalAsync(...) — machine reports unplanned stop
/// </summary>
public interface IProductionMachineExecutionGateway
{
    // Intentionally empty in PRODUCTION-4L.
    // Serves as a compile-time architectural boundary marker.
    // Prevents accidental direct machine-command coupling from Production Execution code.
}
