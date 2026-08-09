using CrevaFactOS.Persistence.Erp;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class ErpConnectionProfileEntityConfiguration : IEntityTypeConfiguration<ErpConnectionProfile>
{
    public void Configure(EntityTypeBuilder<ErpConnectionProfile> entity)
    {
        entity.ToTable("erp_connection_profiles");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(30);
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(e => e.ExternalCompanyId).HasColumnName("external_company_id").HasMaxLength(100);
        entity.Property(e => e.ExternalCompanyCode).HasColumnName("external_company_code").HasMaxLength(100);
        entity.Property(e => e.BaseEndpoint).HasColumnName("base_endpoint").HasMaxLength(500);
        entity.Property(e => e.ConnectionMode).HasColumnName("connection_mode").HasMaxLength(30);
        entity.Property(e => e.CredentialReference).HasColumnName("credential_reference").HasMaxLength(200);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.LastSuccessfulConnectionAtUtc).HasColumnName("last_successful_connection_at_utc");
        entity.Property(e => e.LastFailedConnectionAtUtc).HasColumnName("last_failed_connection_at_utc");
        entity.Property(e => e.LastHealthCheckAtUtc).HasColumnName("last_health_check_at_utc");
        entity.Property(e => e.LastHealthLatencyMs).HasColumnName("last_health_latency_ms");
        entity.Property(e => e.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(100);
        entity.Property(e => e.LastErrorMessage).HasColumnName("last_error_message").HasMaxLength(500);
        entity.Property(e => e.LastSuccessfulSyncAtUtc).HasColumnName("last_successful_sync_at_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.Name }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ErpSyncRunEntityConfiguration : IEntityTypeConfiguration<ErpSyncRun>
{
    public void Configure(EntityTypeBuilder<ErpSyncRun> entity)
    {
        entity.ToTable("erp_sync_runs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(30);
        entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.RecordsRead).HasColumnName("records_read");
        entity.Property(e => e.RecordsCreated).HasColumnName("records_created");
        entity.Property(e => e.RecordsUpdated).HasColumnName("records_updated");
        entity.Property(e => e.RecordsSkipped).HasColumnName("records_skipped");
        entity.Property(e => e.RecordsFailed).HasColumnName("records_failed");
        entity.Property(e => e.Cursor).HasColumnName("cursor").HasMaxLength(200);
        entity.Property(e => e.ErrorCode).HasColumnName("error_code").HasMaxLength(100);
        entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(500);
        entity.Property(e => e.TriggeredBySubjectId).HasColumnName("triggered_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.RetriedFromSyncRunId).HasColumnName("retried_from_sync_run_id");

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.StartedAtUtc });
        entity.HasOne(e => e.ConnectionProfile).WithMany()
            .HasForeignKey(e => e.ConnectionProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ErpIntegrationErrorEntityConfiguration : IEntityTypeConfiguration<ErpIntegrationError>
{
    public void Configure(EntityTypeBuilder<ErpIntegrationError> entity)
    {
        entity.ToTable("erp_integration_errors");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.SyncRunId).HasColumnName("sync_run_id");
        entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(100);
        entity.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(30);
        entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(100);
        entity.Property(e => e.Message).HasColumnName("message").HasMaxLength(500);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");

        entity.HasOne(e => e.SyncRun).WithMany(s => s.Errors)
            .HasForeignKey(e => e.SyncRunId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CanonicalErpCustomerEntityConfiguration : IEntityTypeConfiguration<CanonicalErpCustomer>
{
    public void Configure(EntityTypeBuilder<CanonicalErpCustomer> entity)
    {
        entity.ToTable("erp_customers");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(30);
        entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(100);
        entity.Property(e => e.ExternalCode).HasColumnName("external_code").HasMaxLength(100);
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(300);
        entity.Property(e => e.TaxNumber).HasColumnName("tax_number").HasMaxLength(50);
        entity.Property(e => e.TaxOffice).HasColumnName("tax_office").HasMaxLength(100);
        entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200);
        entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(50);
        entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(10);
        entity.Property(e => e.IsActive).HasColumnName("is_active");
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.SourceUpdatedAtUtc).HasColumnName("source_updated_at_utc");
        entity.Property(e => e.ImportedAtUtc).HasColumnName("imported_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ConnectionProfileId, e.ExternalId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ErpConnectionProfile>().WithMany()
            .HasForeignKey(e => e.ConnectionProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CanonicalErpProductEntityConfiguration : IEntityTypeConfiguration<CanonicalErpProduct>
{
    public void Configure(EntityTypeBuilder<CanonicalErpProduct> entity)
    {
        entity.ToTable("erp_products");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(30);
        entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(100);
        entity.Property(e => e.ExternalCode).HasColumnName("external_code").HasMaxLength(100);
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(300);
        entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(30);
        entity.Property(e => e.Barcode).HasColumnName("barcode").HasMaxLength(100);
        entity.Property(e => e.IsActive).HasColumnName("is_active");
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.SourceUpdatedAtUtc).HasColumnName("source_updated_at_utc");
        entity.Property(e => e.ImportedAtUtc).HasColumnName("imported_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ConnectionProfileId, e.ExternalId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ErpConnectionProfile>().WithMany()
            .HasForeignKey(e => e.ConnectionProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ErpEntityMappingEntityConfiguration : IEntityTypeConfiguration<ErpEntityMapping>
{
    public void Configure(EntityTypeBuilder<ErpEntityMapping> entity)
    {
        entity.ToTable("erp_entity_mappings");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(30);
        entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(100);
        entity.Property(e => e.CanonicalEntityId).HasColumnName("canonical_entity_id");
        entity.Property(e => e.InternalEntityId).HasColumnName("internal_entity_id");
        entity.Property(e => e.SuggestedInternalEntityId).HasColumnName("suggested_internal_entity_id");
        entity.Property(e => e.SuggestedReason).HasColumnName("suggested_reason").HasMaxLength(100);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ConnectionProfileId, e.EntityType, e.ExternalId })
            .IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CanonicalErpOrderEntityConfiguration : IEntityTypeConfiguration<CanonicalErpOrder>
{
    public void Configure(EntityTypeBuilder<CanonicalErpOrder> entity)
    {
        entity.ToTable("erp_orders");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(30);
        entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(100);
        entity.Property(e => e.ExternalCode).HasColumnName("external_code").HasMaxLength(100);
        entity.Property(e => e.OrderNumber).HasColumnName("order_number").HasMaxLength(100);
        entity.Property(e => e.ExternalCustomerId).HasColumnName("external_customer_id").HasMaxLength(100);
        entity.Property(e => e.ExternalCustomerCode).HasColumnName("external_customer_code").HasMaxLength(100);
        entity.Property(e => e.OrderDateUtc).HasColumnName("order_date_utc");
        entity.Property(e => e.RequestedDeliveryDateUtc).HasColumnName("requested_delivery_date_utc");
        entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.Reference).HasColumnName("reference").HasMaxLength(200);
        entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(1000);
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.SourceUpdatedAtUtc).HasColumnName("source_updated_at_utc");
        entity.Property(e => e.ImportedAtUtc).HasColumnName("imported_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ConnectionProfileId, e.ExternalId }).IsUnique();
        entity.HasMany(e => e.Lines).WithOne(l => l.Order!).HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ErpConnectionProfile>().WithMany()
            .HasForeignKey(e => e.ConnectionProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CanonicalErpOrderLineEntityConfiguration : IEntityTypeConfiguration<CanonicalErpOrderLine>
{
    public void Configure(EntityTypeBuilder<CanonicalErpOrderLine> entity)
    {
        entity.ToTable("erp_order_lines");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.OrderId).HasColumnName("order_id");
        entity.Property(e => e.ExternalLineId).HasColumnName("external_line_id").HasMaxLength(100);
        entity.Property(e => e.LineNumber).HasColumnName("line_number");
        entity.Property(e => e.ExternalProductId).HasColumnName("external_product_id").HasMaxLength(100);
        entity.Property(e => e.ExternalProductCode).HasColumnName("external_product_code").HasMaxLength(100);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 4);
        entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(30);
        entity.Property(e => e.RequestedDeliveryDateUtc).HasColumnName("requested_delivery_date_utc");
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ConnectionProfileId, e.OrderId, e.ExternalLineId })
            .IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CanonicalErpWarehouseEntityConfiguration : IEntityTypeConfiguration<CanonicalErpWarehouse>
{
    public void Configure(EntityTypeBuilder<CanonicalErpWarehouse> entity)
    {
        entity.ToTable("erp_warehouses");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(30);
        entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(100);
        entity.Property(e => e.ExternalCode).HasColumnName("external_code").HasMaxLength(100);
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(300);
        entity.Property(e => e.IsActive).HasColumnName("is_active");
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.SourceUpdatedAtUtc).HasColumnName("source_updated_at_utc");
        entity.Property(e => e.ImportedAtUtc).HasColumnName("imported_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ConnectionProfileId, e.ExternalId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CanonicalErpInventoryBalanceEntityConfiguration
    : IEntityTypeConfiguration<CanonicalErpInventoryBalance>
{
    public void Configure(EntityTypeBuilder<CanonicalErpInventoryBalance> entity)
    {
        entity.ToTable("erp_inventory_balances");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(30);
        entity.Property(e => e.ExternalProductId).HasColumnName("external_product_id").HasMaxLength(100);
        entity.Property(e => e.ExternalProductCode).HasColumnName("external_product_code").HasMaxLength(100);
        entity.Property(e => e.ExternalWarehouseId).HasColumnName("external_warehouse_id").HasMaxLength(100);
        entity.Property(e => e.ExternalWarehouseCode).HasColumnName("external_warehouse_code").HasMaxLength(100);
        entity.Property(e => e.QuantityOnHand).HasColumnName("quantity_on_hand").HasPrecision(18, 4);
        entity.Property(e => e.QuantityAvailable).HasColumnName("quantity_available").HasPrecision(18, 4);
        entity.Property(e => e.QuantityReserved).HasColumnName("quantity_reserved").HasPrecision(18, 4);
        entity.Property(e => e.QuantityOnOrder).HasColumnName("quantity_on_order").HasPrecision(18, 4);
        entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(30);
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.SourceUpdatedAtUtc).HasColumnName("source_updated_at_utc");
        entity.Property(e => e.ImportedAtUtc).HasColumnName("imported_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new
        {
            e.TenantId,
            e.FacilityId,
            e.ConnectionProfileId,
            e.ExternalProductId,
            e.ExternalWarehouseId,
        }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CanonicalErpPurchaseOrderEntityConfiguration : IEntityTypeConfiguration<CanonicalErpPurchaseOrder>
{
    public void Configure(EntityTypeBuilder<CanonicalErpPurchaseOrder> entity)
    {
        entity.ToTable("erp_purchase_orders");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(30);
        entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(100);
        entity.Property(e => e.ExternalCode).HasColumnName("external_code").HasMaxLength(100);
        entity.Property(e => e.PurchaseOrderNumber).HasColumnName("purchase_order_number").HasMaxLength(100);
        entity.Property(e => e.ExternalSupplierId).HasColumnName("external_supplier_id").HasMaxLength(100);
        entity.Property(e => e.ExternalSupplierCode).HasColumnName("external_supplier_code").HasMaxLength(100);
        entity.Property(e => e.SupplierName).HasColumnName("supplier_name").HasMaxLength(300);
        entity.Property(e => e.OrderDateUtc).HasColumnName("order_date_utc");
        entity.Property(e => e.ExpectedDeliveryDateUtc).HasColumnName("expected_delivery_date_utc");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10);
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.SourceUpdatedAtUtc).HasColumnName("source_updated_at_utc");
        entity.Property(e => e.ImportedAtUtc).HasColumnName("imported_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ConnectionProfileId, e.ExternalId }).IsUnique();
        entity.HasMany(e => e.Lines).WithOne(l => l.PurchaseOrder!).HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CanonicalErpPurchaseOrderLineEntityConfiguration
    : IEntityTypeConfiguration<CanonicalErpPurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<CanonicalErpPurchaseOrderLine> entity)
    {
        entity.ToTable("erp_purchase_order_lines");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
        entity.Property(e => e.ExternalLineId).HasColumnName("external_line_id").HasMaxLength(100);
        entity.Property(e => e.LineNumber).HasColumnName("line_number");
        entity.Property(e => e.ExternalProductId).HasColumnName("external_product_id").HasMaxLength(100);
        entity.Property(e => e.ExternalProductCode).HasColumnName("external_product_code").HasMaxLength(100);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        entity.Property(e => e.OrderedQuantity).HasColumnName("ordered_quantity").HasPrecision(18, 4);
        entity.Property(e => e.ReceivedQuantity).HasColumnName("received_quantity").HasPrecision(18, 4);
        entity.Property(e => e.OpenQuantity).HasColumnName("open_quantity").HasPrecision(18, 4);
        entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(30);
        entity.Property(e => e.ExpectedDeliveryDateUtc).HasColumnName("expected_delivery_date_utc");
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CanonicalErpProductionOrderEntityConfiguration
    : IEntityTypeConfiguration<CanonicalErpProductionOrder>
{
    public void Configure(EntityTypeBuilder<CanonicalErpProductionOrder> entity)
    {
        entity.ToTable("erp_production_orders");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(30);
        entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(100);
        entity.Property(e => e.ExternalCode).HasColumnName("external_code").HasMaxLength(100);
        entity.Property(e => e.ProductionOrderNumber).HasColumnName("production_order_number").HasMaxLength(100);
        entity.Property(e => e.ExternalProductId).HasColumnName("external_product_id").HasMaxLength(100);
        entity.Property(e => e.ExternalProductCode).HasColumnName("external_product_code").HasMaxLength(100);
        entity.Property(e => e.ProductDescription).HasColumnName("product_description").HasMaxLength(500);
        entity.Property(e => e.PlannedQuantity).HasColumnName("planned_quantity").HasPrecision(18, 4);
        entity.Property(e => e.CompletedQuantity).HasColumnName("completed_quantity").HasPrecision(18, 4);
        entity.Property(e => e.ScrapQuantity).HasColumnName("scrap_quantity").HasPrecision(18, 4);
        entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(30);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.PlannedStartUtc).HasColumnName("planned_start_utc");
        entity.Property(e => e.PlannedEndUtc).HasColumnName("planned_end_utc");
        entity.Property(e => e.ActualStartUtc).HasColumnName("actual_start_utc");
        entity.Property(e => e.ActualEndUtc).HasColumnName("actual_end_utc");
        entity.Property(e => e.ExternalSalesOrderId).HasColumnName("external_sales_order_id").HasMaxLength(100);
        entity.Property(e => e.ExternalSalesOrderCode).HasColumnName("external_sales_order_code").HasMaxLength(100);
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.SourceUpdatedAtUtc).HasColumnName("source_updated_at_utc");
        entity.Property(e => e.ImportedAtUtc).HasColumnName("imported_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ConnectionProfileId, e.ExternalId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CanonicalErpDocumentEntityConfiguration : IEntityTypeConfiguration<CanonicalErpDocument>
{
    public void Configure(EntityTypeBuilder<CanonicalErpDocument> entity)
    {
        entity.ToTable("erp_documents");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(30);
        entity.Property(e => e.ExternalId).HasColumnName("external_id").HasMaxLength(100);
        entity.Property(e => e.ExternalCode).HasColumnName("external_code").HasMaxLength(100);
        entity.Property(e => e.DocumentType).HasColumnName("document_type").HasMaxLength(40);
        entity.Property(e => e.DocumentNumber).HasColumnName("document_number").HasMaxLength(100);
        entity.Property(e => e.DocumentDateUtc).HasColumnName("document_date_utc");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.RelatedEntityType).HasColumnName("related_entity_type").HasMaxLength(30);
        entity.Property(e => e.RelatedExternalEntityId).HasColumnName("related_external_entity_id").HasMaxLength(100);
        entity.Property(e => e.RelatedExternalEntityCode).HasColumnName("related_external_entity_code").HasMaxLength(100);
        entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(300);
        entity.Property(e => e.MimeType).HasColumnName("mime_type").HasMaxLength(100);
        entity.Property(e => e.ContentAvailable).HasColumnName("content_available");
        entity.Property(e => e.ExternalReference).HasColumnName("external_reference").HasMaxLength(200);
        entity.Property(e => e.SourceVersion).HasColumnName("source_version").HasMaxLength(100);
        entity.Property(e => e.SourceUpdatedAtUtc).HasColumnName("source_updated_at_utc");
        entity.Property(e => e.ImportedAtUtc).HasColumnName("imported_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ConnectionProfileId, e.ExternalId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ErpWritebackRequestEntityConfiguration : IEntityTypeConfiguration<ErpWritebackRequest>
{
    public void Configure(EntityTypeBuilder<ErpWritebackRequest> entity)
    {
        entity.ToTable("erp_writeback_requests");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ConnectionProfileId).HasColumnName("connection_profile_id");
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(30);
        entity.Property(e => e.CommandType).HasColumnName("command_type").HasMaxLength(100);
        entity.Property(e => e.TargetEntityType).HasColumnName("target_entity_type").HasMaxLength(30);
        entity.Property(e => e.TargetExternalId).HasColumnName("target_external_id").HasMaxLength(100);
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
        entity.Property(e => e.PayloadHash).HasColumnName("payload_hash").HasMaxLength(64);
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(500);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.RequestedBySubjectId).HasColumnName("requested_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.RequestedAtUtc).HasColumnName("requested_at_utc");
        entity.Property(e => e.SubmittedAtUtc).HasColumnName("submitted_at_utc");
        entity.Property(e => e.ApprovedPayloadHash).HasColumnName("approved_payload_hash").HasMaxLength(64);
        entity.Property(e => e.ApprovedVersion).HasColumnName("approved_version");
        entity.Property(e => e.ExecutedAtUtc).HasColumnName("executed_at_utc");
        entity.Property(e => e.ExternalExecutionReference).HasColumnName("external_execution_reference").HasMaxLength(200);
        entity.Property(e => e.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(100);
        entity.Property(e => e.LastErrorMessage).HasColumnName("last_error_message").HasMaxLength(500);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
        entity.HasIndex(e => new
        {
            e.TenantId,
            e.FacilityId,
            e.ConnectionProfileId,
            e.CommandType,
            e.IdempotencyKey,
        }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ErpConnectionProfile>().WithMany()
            .HasForeignKey(e => e.ConnectionProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ErpWritebackApprovalEntityConfiguration : IEntityTypeConfiguration<ErpWritebackApproval>
{
    public void Configure(EntityTypeBuilder<ErpWritebackApproval> entity)
    {
        entity.ToTable("erp_writeback_approvals");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.WritebackRequestId).HasColumnName("writeback_request_id");
        entity.Property(e => e.Decision).HasColumnName("decision").HasMaxLength(30);
        entity.Property(e => e.DecidedBySubjectId).HasColumnName("decided_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.DecidedAtUtc).HasColumnName("decided_at_utc");
        entity.Property(e => e.RequestVersion).HasColumnName("request_version");
        entity.Property(e => e.PayloadHash).HasColumnName("payload_hash").HasMaxLength(64);
        entity.Property(e => e.Comment).HasColumnName("comment").HasMaxLength(1000);
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.WritebackRequestId, e.DecidedAtUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.WritebackRequest).WithMany(r => r.Approvals)
            .HasForeignKey(e => e.WritebackRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
