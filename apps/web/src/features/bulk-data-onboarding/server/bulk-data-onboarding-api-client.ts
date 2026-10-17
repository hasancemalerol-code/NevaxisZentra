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
  updatedRows: number;
  failedRows: number;
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
    updatedRows: number;
    quarantinedRows: number;
    failedRows: number;
    balanced: boolean;
  };
  checkpoint?: {
    currentTable: string;
    currentBatch: number;
    processedRows: number;
  };
  quarantine?: Array<{
    id: string;
    sourceTable: string;
    sourceKey: string;
    reasonCode: string;
    safeReason: string;
    severity: string;
    status: string;
  }>;
  mappingRules?: Array<{
    sourceTable: string;
    sourceColumn: string;
    canonicalEntityType: string;
    canonicalField: string;
    mappingType: string;
  }>;
  uploadedFiles?: {
    files: Array<{ fileName: string; storedName: string; sizeBytes: number }>;
  };
};
