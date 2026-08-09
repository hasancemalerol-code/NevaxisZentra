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
