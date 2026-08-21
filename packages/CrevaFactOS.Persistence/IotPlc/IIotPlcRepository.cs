namespace CrevaFactOS.Persistence.IotPlc;

public interface IIotPlcRepository
{
    Task<IotPlcWorkspaceReadModel> GetWorkspaceAsync(
        IotPlcOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IotPlcEndpointListReadModel> ListEndpointsAsync(
        IotPlcOperationContext context,
        IotPlcEndpointListFilters filters,
        CancellationToken cancellationToken = default);

    Task<IotPlcEndpointDetailReadModel?> GetEndpointByReferenceAsync(
        IotPlcOperationContext context,
        string reference,
        CancellationToken cancellationToken = default);

    Task<IotPlcSignalListReadModel> ListSignalsAsync(
        IotPlcOperationContext context,
        IotPlcSignalListFilters filters,
        CancellationToken cancellationToken = default);

    Task<IotPlcSignalObservationListReadModel> ListSignalObservationsAsync(
        IotPlcOperationContext context,
        string signalReference,
        IotPlcSignalObservationListFilters filters,
        CancellationToken cancellationToken = default);

    Task<IotPlcConnectionHealthListReadModel> ListConnectionHealthAsync(
        IotPlcOperationContext context,
        string endpointReference,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IotPlcWriteResult> CreateEndpointAsync(
        IotPlcOperationContext context,
        CreateIotPlcEndpointCommand command,
        CancellationToken cancellationToken = default);

    Task<IotPlcWriteResult> UpdateEndpointAsync(
        IotPlcOperationContext context,
        string reference,
        UpdateIotPlcEndpointCommand command,
        CancellationToken cancellationToken = default);

    Task<IotPlcWriteResult> CreateSignalAsync(
        IotPlcOperationContext context,
        CreateIotPlcSignalCommand command,
        CancellationToken cancellationToken = default);

    Task<IotPlcWriteResult> UpdateSignalAsync(
        IotPlcOperationContext context,
        string reference,
        UpdateIotPlcSignalCommand command,
        CancellationToken cancellationToken = default);

    Task<IotPlcWriteResult> IngestObservationAsync(
        IotPlcOperationContext context,
        IngestIotPlcObservationCommand command,
        CancellationToken cancellationToken = default);

    Task<IotPlcWriteResult> IngestConnectionHealthAsync(
        IotPlcOperationContext context,
        IngestIotPlcConnectionHealthCommand command,
        CancellationToken cancellationToken = default);
}
