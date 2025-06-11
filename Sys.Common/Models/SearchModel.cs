using System;
using System.Collections.Generic;

namespace Sys.Common.Models
{
    public class SearchModel
    {
        public double? CompanyId { get; set; }
        public string SearchACondition { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }
        public string OrderBy { get; set; }
        public bool? IsDesc { get; set; }
        public bool IsPaging { get; set; }
    }
    

    public class SearchModelv2
    {
        public string ExternalOrdNbr { get; set; }
        public string RouteZone { get; set; }
        public string CustomerId { get; set; }
        public string ShipToId { get; set; }
        public string Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }
    }
}