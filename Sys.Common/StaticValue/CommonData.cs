using System.Collections.Generic;
using SysAdmin.Models.SystemUrl;

namespace SysAdmin.Models.StaticValue
{
    public static class CommonData
    {
        public class StockType
        {
            public const int Stock = 1;
            public const int NonStock = 2;
        }
        public class SystemSetting
        {
            public const string StockItemType = "MDMST";
            public const string NonStockItemType = "MDMNST";
            public const string Status = "MDSS";
            public const string Country = "COUNTRY";

            public const string S3BucketType = "S3bucket";
            public const string AccessKey = "AccessKey";
            public const string SecretKey = "SecretKey";
            public const string S3BucketNameKey = "-awsbucket";
            public const string S3CdnKey = "-cdn";
            public const string S3CccesspointKey = "-accesspoint";
            public const string S3DefaultDataScript = "script";
        }
        public class ItemSetting
        {
            public const string IT01 = "IT01";
            public const string IT02 = "IT02";
            public const string IT03 = "IT03";
            public const string IT04 = "IT04";
            public const string IT05 = "IT05";
            public const string IT06 = "IT06";
            public const string IT07 = "IT07";
            public const string IT08 = "IT08";
            public const string IT09 = "IT09";
            public const string IT10 = "IT10";
        }

        public class GeoMasterCode
        {
            public const string CITY = "CITY";
            public const string PROVINCE = "PROV";
            public const string STATE = "STAT";
            public const string DISTRICT = "DIST";
            public const string WARDS = "WARD";
            public const string REGION = "REGI";
        }

        public class MultiplyDivide
        {
            public const string Multiply = "M";
            public const string Divide = "D";
        }
        public class StatusStr
        {
            public const string Active = "Active";
            public const string InActive = "InActive";
        }
        public class Status
        {
            public const string Active = "1";
            public const string InActive = "0";
        }

        public static List<SystemUrlModel> SystemUrl { get; set; }
        public static class SystemUrlCode
        {
            public const string KpiCode = "KpiAPI";
            public const string SystemAdminAPI = "SystemAdminAPI";
            public const string RDOSSystem = "SystemAdminAPI";
            public const string SalesOrgAPI = "SalesOrgAPI";
            public const string ODTpAPI = "ODTpAPI";
            public const string ODInventoryAPI = "ODInventoryAPI";
            public const string PurchaseOrderCode = "PurchaseOrderAPI";
            public const string RouteMngAPI = "RouteMngAPI";
            public const string SaleOrderAPI = "SaleOrderAPI";
            public const string PriceMngCode = "PriceMngAPI";
            public const string BaselineAPI = "BaselineAPI";
            public const string NotiMobileRdosAPI = "NotiMobileRdosAPI";
            public const string ODItemAPI = "ODItemAPI";
            public const string ODCustomerAPI = "ODCustomerAPI";
            public const string ODPriceAPI = "ODPriceAPI";
            public const string ODSaleOrderAPI = "ODSaleOrderAPI";
            public const string ODBaseLineAPI = "ODBaseLineAPI";
            public const string ODCommonAPI = "ODCommonAPI";
            public const string ODDistributorAPI = "ODDistributorAPI";
            public const string ODNotificationAPI = "ODNotificationAPI";
        }
    }
}
