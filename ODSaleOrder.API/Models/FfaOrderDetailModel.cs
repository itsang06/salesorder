using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using Sys.Common.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace ODSaleOrder.API.Models
{
    public class FfaOrderDetailModel
    {
        public FfasoOrderInformation OrderInfo { get; set; }
        public FfasoOrderItem Item { get; set; }
        public FfasoImportItem ItemImport { get; set; }
    }

    public class FfaOrderGroupModel
    {
        public FfasoOrderInformation OrderInfo { get; set; }
        public List<FfasoOrderItem> Items { get; set; } = new List<FfasoOrderItem>();
        public List<FfasoImportItem> ItemImports { get; set; } = new List<FfasoImportItem>();
    }

    public class ListFfaModel
    {
        public List<FfaOrderGroupModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }

    public class RequestDistributorShipto
    {
        public string DistributorCode { get; set; }
        public string ShiptoCode { get; set; }
    }
    public class SearchFfaOrderModel : EcoparamsWithGenericFilter
    {
        public List<RequestDistributorShipto> ListDistributorAndShipto { get; set; } = new List<RequestDistributorShipto>();
        public string SalesRepID { get; set; }
        public string ImportStatus { get; set; }
        public string FFAStatus  { get; set; }
        public string Customer { get; set; }
    }

    public class ImportListFfaOrder
    {
        public List<string> OrderRefNumbers { get; set; } = new List<string>();
    }

    public class CancelListFfaOrder
    {
        public string OrderRefNumbers { get; set; } = string.Empty;
        public string ReasonCancel { get; set; } = string.Empty;
    }


    public class FFAOrderInfoExisted
    {
        public Guid Id { get; set; }
        public string OrderRefNumber { get; set; }
        public string Status { get; set; }
        public string VisitID { get; set; }
       
    }

    public class FFAOrderItemExisted
    {
        public Guid Id { get; set; }

        public string OrderRefNumber { get; set; }

        public string VisitId { get; set; }

    }

    public class IssueImportOrderModel
    {
        public string OrderRefNumber { get; set; }
        public string Message { get; set; }
    }

    public class IssueImportResultModel
    {
        public List<IssueImportOrderModel> ListSuccess { get; set; } = new List<IssueImportOrderModel>();
        public List<IssueImportOrderModel> ListError { get; set; } = new List<IssueImportOrderModel>();
    }
}
