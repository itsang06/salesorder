using System;
public  class ProductivityReportModel 
{
  public Guid Id { get; set; }
  public string RouteZoneID { get; set; }
  public Guid OrderItemId { get; set; } = Guid.Empty;
  public string OrderItemInventoryID { get; set; }
  public string OrderItemItemDescription { get; set; }
  public Guid? OrderItemBaseUnit { get; set; } = Guid.Empty;
  public int OrderItemOrderBaseQuantities { get; set; } = 0;
  public int OrderItemShippedBaseQuantities { get; set; } = 0; // quy đổi số lượng base cho ShippedQuantities
}