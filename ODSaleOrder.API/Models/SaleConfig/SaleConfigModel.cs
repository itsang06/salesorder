using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    public class DSAqueryModel
    {
        public List<Temp_PromotionDetailsModel> Temp_ProgramsDetails { get; set; }
    }

    public class DistributorInfoModel
    {
        public string DistributorCode { get; set; }
        public string DistributorName { get; set; }
        public Guid? DSAId { get; set; }
        public string DSACode { get; set; }
        public string TerritoryStructureCode { get; set; }
        public string TerritoryValueKey { get; set; }
        public string SalesOrgId { get; set; }
        public Guid DistributorId { get; set; }

    }

    public class MdmModel
    {
        public string TerritoryValue {get;set;} //TL04-AC2
        public string TerritoryLevel { get; set; }   // "TL04",
        public string Source { get; set; }   // "Area",
        public string EmployeeCode { get; set; }   // "SCS000014",
        public bool IsDsa { get; set; }   // true
    }

    public class EmployeeHoInforResultModel
    {
        public string EmployeeCode { get; set; }
        public string SalesOrgId { get; set; }
        public string TerriotryStructureCode { get; set; }
        public string TerritoryValueKey { get; set; }
        public List<DistributorInfoModel> ListDistributor { get; set; } = new List<DistributorInfoModel>();
    }
}
