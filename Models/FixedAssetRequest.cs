using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FixedAssetSystem.Models;

public partial class FixedAssetRequest : IValidatableObject
{
    public int Id { get; set; }

    public string? ControlNo { get; set; }

    public DateOnly DateRequested { get; set; }

    public string Department { get; set; } = null!;

    public DateOnly TargetDateNeeded { get; set; }

    public string Section { get; set; } = null!;

    public int Quantity { get; set; }

    public int? AssetTypeId { get; set; }

    public string? AssetType { get; set; }

    public string DetailedDescription { get; set; } = null!;

    public string ReasonPurpose { get; set; } = null!;

    public string? ProposedLocation { get; set; }

    public string? EstimatedLifeSpan { get; set; }

    public string RequestType { get; set; } = null!;

    public int? ExistingUnitCount { get; set; }

    public string? ExistingUser { get; set; }

    public string? DamagedReportNo { get; set; }

    public int RequestedByEmployeeId { get; set; }

    public string? RequestedByName { get; set; }

    public DateTime? RequestedAt { get; set; }

    public int? EvaluatedByEmployeeId { get; set; }

    public string? EvaluatedByName { get; set; }

    public DateTime? EvaluatedAt { get; set; }

    public string? RequestStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual AssetType? AssetTypeNavigation { get; set; }

    public virtual Employee? EvaluatedByEmployee { get; set; }

    public virtual ICollection<ExistingUnitDetail> ExistingUnitDetails { get; set; } = new List<ExistingUnitDetail>();

    public virtual ICollection<FixedAssetPrintLog> FixedAssetPrintLogs { get; set; } = new List<FixedAssetPrintLog>();

    public virtual ICollection<FixedAssetRequestApproval> FixedAssetRequestApprovals { get; set; } = new List<FixedAssetRequestApproval>();

    public virtual ICollection<MemorandumReceipt> MemorandumReceipts { get; set; } = new List<MemorandumReceipt>();

    public virtual ICollection<RequestStatusHistory> RequestStatusHistories { get; set; } = new List<RequestStatusHistory>();

    public virtual Employee? RequestedByEmployee { get; set; }

    // ========== ADD VALIDATION METHOD ==========
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validation 1: Quantity must be at least 1
        if (Quantity < 1)
        {
            yield return new ValidationResult(
                "Quantity must be at least 1.",
                new[] { nameof(Quantity) });
        }

        // Validation 2: For Replacement requests, DamagedReportNo is required
        if (RequestType == "Replacement" && string.IsNullOrWhiteSpace(DamagedReportNo))
        {
            yield return new ValidationResult(
                "Damaged Report No. is required when Request Type is 'Replacement'.",
                new[] { nameof(DamagedReportNo) });
        }
    }
}
