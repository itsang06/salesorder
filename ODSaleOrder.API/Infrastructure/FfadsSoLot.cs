using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Infrastructure;

public partial class FfadsSoLot
{
    public Guid Id { get; set; }

    public string OrderRefNumber { get; set; }

    public string VisitId { get; set; }

    public string External_OrdNBR { get; set; }

    public int? OrderLine { get; set; }

    public string ItemCode { get; set; }

    public string ItemDescription { get; set; }

    public string Uom { get; set; }

    public string UomDesc { get; set; }

    public string ItemGroupId { get; set; }

    public string ItemGroupDescription { get; set; }

    public int? OriginalOrderQtyBooked { get; set; }

    public int? OriginalOrderBaseQtyBooked { get; set; }

    public string IssueUom { get; set; }

    public string IssueUomDesc { get; set; }

    public int? IssueQty { get; set; }

    public int? IssueBaseQty { get; set; }

    public string AllocateType { get; set; }

    public string LotNum { get; set; }

    public string SerialNum { get; set; }

    public DateTime? ExpiredDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string CreatedBy { get; set; }

    public string UpdatedBy { get; set; }
}
