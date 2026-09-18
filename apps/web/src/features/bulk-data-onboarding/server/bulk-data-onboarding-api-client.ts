import "./bulk-data-onboarding-server-boundary";

export type BulkImportJobSummaryApi = {
  id: string;
  name: string;
  status: string;
  sourceSystem: string;
  sourceProviderKind: string;
  totalSourceRows: number;
  validRows: number;
  quarantinedRows: number;
  importedRows: number;
  safetyMode: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type BulkImportJobWorkspaceApi = {
  job: BulkImportJobSummaryApi;
  catalog: Array<{
    sourceDatabase: string;
    sourceSchema: string;
    sourceTable: string;
    columns: Array<{ name: string; dataType: string }>;
    estimatedRowCount: number;
  }>;
  impactPreview?: {
    areasToCreate: number;
    linesToCreate: number;
    stationsToCreate: number;
    resourcesToCreate: number;
    materialsToCreate: number;
    ordersToCreate: number;
    eventsToCreate: number;
    quarantinedRows: number;
    conflicts: number;
  };
  reconciliation?: {
    sourceRows: number;
    stagedRows: number;
    validRows: number;
    importedRows: number;
    quarantinedRows: number;
    balanced: boolean;
  };
  checkpoint?: {
    currentTable: string;
    currentBatch: number;
    processedRows: number;
  };
};
