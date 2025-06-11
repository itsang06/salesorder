using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Reflection.Emit;

namespace ODSaleOrder.API.Infrastructure
{
    public class FFASoSuggestOrder
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string VisitId { get; set; }
        public string CustomerId { get; set; }
        public string CusShiptoId { get; set; }
        public string EmployeeCode { get; set; }
        public DateTime? SuggestOrdTime { get; set; }
        public string SuggestType { get; set; }
        public string ItemType { get; set; }
        public string ItemCode { get; set; }
        public double? SuggestQty { get; set; }
        public string UomId { get; set; }
        public bool IsPicked { get; set; }
        public string ReasonId { get; set; }
        public string ReasonDesc { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }


    }
}
