namespace CrevaFactOS.Persistence.IotPlc;

public interface IIotPlcIntelligenceRepository
{
    Task<IotPlcIntelligenceWorkspaceReadModel> GetWorkspaceAsync(
        IotPlcOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IotPlcQualityProfileListReadModel> ListProfilesAsync(
        IotPlcOperationContext context,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IotPlcQualityProfileReadModel?> GetProfileByReferenceAsync(
        IotPlcOperationContext context,
        string reference,
        CancellationToken cancellationToken = default);

    Task<IotPlcIntelligenceWriteResult> CreateProfileAsync(
        IotPlcOperationContext context,
        CreateIotPlcQualityProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<IotPlcIntelligenceWriteResult> UpdateProfileAsync(
        IotPlcOperationContext context,
        string reference,
        UpdateIotPlcQualityProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<IotPlcAssessmentDetailReadModel<IotPlcConnectivityAssessmentReadModel>?> GetConnectivityAssessmentAsync(
        IotPlcOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IotPlcConnectivityAssessmentReadModel>> ListConnectivityAssessmentsAsync(
        IotPlcOperationContext context,
        Guid? endpointId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IotPlcAssessmentDetailReadModel<IotPlcSignalQualityAssessmentReadModel>?> GetSignalAssessmentAsync(
        IotPlcOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IotPlcSignalQualityAssessmentReadModel>> ListSignalAssessmentsAsync(
        IotPlcOperationContext context,
        Guid? signalId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IotPlcIntelligenceWriteResult> EvaluateConnectivityAsync(
        IotPlcOperationContext context,
        EvaluateIotPlcConnectivityCommand command,
        CancellationToken cancellationToken = default);

    Task<IotPlcIntelligenceWriteResult> EvaluateSignalQualityAsync(
        IotPlcOperationContext context,
        EvaluateIotPlcSignalQualityCommand command,
        CancellationToken cancellationToken = default);
}
