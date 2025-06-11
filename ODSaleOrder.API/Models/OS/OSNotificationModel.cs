using System.Collections.Generic;

namespace ODSaleOrder.API.Models.OS
{
    public class OSNotificationModel
    {
        public string External_OrdNBR { get; set; }
        public string OrderRefNumber { get; set; }
        public string OSStatus { get; set; }
        public string SOStatus { get; set; }
        public string DistributorCode { get; set; }
        public string DistributorName { get; set; }
        public string OutletCode { get; set; }
        public string Purpose { get; set; }
    }

    public class OSNotificationReqModel
    {
        public string title { get; set; }
        public string body { get; set; }
        public string notiImageFileName { get; set; }
        public string notiImagePathFile { get; set; }
        public List<string> outletCodeList { get; set; } = new List<string>();
        public string notiType { get; set; }
        public string priority { get; set; }
        public string navigateType { get; set; }
        public string navigatePath { get; set; }
        public string purpose { get; set; }
        public string templateData { get; set; }
        public string ownerType { get; set; }
        public string ownerCode { get; set; }
    }

    public class OsNotiResponse
    {
        public List<string> messages { get; set; }
        public object data { get; set; }
        public bool success { get; set; }
        public string strackTrace { get; set; }
        public int totalCount { get; set; }
    }

    public class SaleOrderTestNotiModel
    {
        public string External_OrdNBR { get; set; }
        public string OrderRefNumber { get; set; }
        public string OSStatus { get; set; }
        public string DistributorCode { get; set; }
        public string DistributorName { get; set; }
        public string OSOutletCode { get; set; }
        public string Status { get; set; }
    }
}
