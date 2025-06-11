using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysAdmin.API.Constants
{
    public static class Constant
    {
        public static class MenuTypeConst
        {
            public const string SO_Menu01 = "SO01";
            public const string SO_Menu02 = "SO02";
        }

        public static class ParameterCode
        {
            public const string DefaultParamCode = "PR00";
            public const string PrefixParamCode = "PR";
            public const string DefaultSpecificParamCode = "SPR00";
            public const string PrefixSpecificParamCode = "SPR";
        }

        public static class BeatPlanType
        {
            public const string FREQUENCY = "Frequency";
            public const string DAILY = "Daily";
        }

        public static class BeatPlanStatus
        {
            public const string NEW = "New";
            public const string RELEASE = "Released";
        }

        public static class ValidStatus
        {
            public const string PASSED = "Passed";
            public const string FAILED = "Failed";
        }

        public class StatusStr
        {
            public const string Active = "Active";
            public const string InActive = "InActive";
        }

        public static class BeatPlanInquiryType
        {
            public const string TOTALBYDAYS = "Total shipto by days";
            public const string TOTALBYDATE = "Total shipto by date";
            public const string SCHEDULEBYDATE = "Schedule by date";
            public const string SCHEDULEBYDAYS = "Schedule by days";
        }

        public static class DayOfWeekConst
        {
            public const string MON = "Monday";
            public const string TUE = "Tuesday";
            public const string WED = "Wednesday";
            public const string THU = "Thursday";
            public const string FRI = "Friday";
            public const string SAT = "Saturday";
            public const string SUN = "Sunday";
        }

        public static class CalendarConstants
        {
            public const string WEEK = "WEEK";
        }

        public static class SO_SaleOrderStatusConst //SO_STATUS
        {
            public const string DRAFT = "SO_ST_DRAFT";
            public const string OPEN = "SO_ST_OPEN";
            public const string SHIPPING = "SO_ST_SHIPPING";
            public const string WAITNGSHIPPING = "SO_ST_WAITINGSHIPPING";
            public const string DELIVERED = "SO_ST_DELIVERED";//
            public const string PARTIALDELIVERED = "SO_ST_PARTIALDELIVERED";//
            public const string FAILED = "SO_ST_FAILED";
            public const string CANCEL = "SO_ST_CANCEL";
            public const string CONFIRM = "SO_ST_CONFIRM";
            public const string COMPLETE_DRAFT = "SO_ST_COMPLETE_DRAFT";//
            public const string WAITINGIMPORT = "OS_SO_00";
            public const string IMPORTSUCCESSFULLY = "OS_SO_01";
            public const string OUTOFBUDGET = "OS_SO_03";
            public const string OUTOFSTOCK = "OS_SO_02";
            public const string OUTOFSTOCKBUDGET = "OS_SO_04";

            /// <summary>
            /// Trạng thái cho phép cancel SO
            /// </summary>
            public static List<string> AllowCancelStatuses = new List<string>()
            {
                WAITINGIMPORT,
                OUTOFSTOCK,
                OUTOFBUDGET,
                OUTOFSTOCKBUDGET,
                IMPORTSUCCESSFULLY,
                WAITNGSHIPPING
            };
            /// <summary>
            /// Trạng thái chưa có tạo SO_OrderInformation
            /// </summary>
            public static List<string> HaveNoSOStatuses = new List<string>()
            {
                WAITINGIMPORT,
                OUTOFSTOCK,
                OUTOFBUDGET,
                OUTOFSTOCKBUDGET
            };
            /// <summary>
            /// Trạng thái đã tạo SO_OrderInformation
            /// </summary>
            public static List<string> AlreadyHaveSOStatuses = new List<string>()
            {
                IMPORTSUCCESSFULLY,
                WAITNGSHIPPING
            };
            /// <summary>
            /// Trạng thái không cần update status cho OSOrderInformation
            /// </summary>
            public static List<string> NotUpdateStatuses = new List<string>()
            {
                WAITINGIMPORT,
                OUTOFSTOCK,
                OUTOFBUDGET,
                OUTOFSTOCKBUDGET,
                IMPORTSUCCESSFULLY
            };
        }

        public class INV_TransactionType
        {
            public const string INV_INC = "INV01";
            public const string INV_DEC = "INV02";
            public const string PO_IN = "INV03";
            public const string PO_OUT = "INV04";
            public const string SO_CONFIRM = "INV07";
            public const string SO_PICKING = "INV08";
            public const string SO_SHIPPED = "INV09"; // SO_ST_SHIPPING  => SO_ST_DELIVERED || SO_ST_PARTIALDELIVERED
            public const string SO_CL = "INV10";  //SO_ST_DELIVERED || SO_ST_PARTIALDELIVERED => SO_ST_CANCEL
            public const string SO_RE = "INV11";
            public const string SO_SHIPPED_NOPICKING = "INV12"; // SO_ST_WAITINGSHIPPING => SO_ST_DELIVERED || SO_ST_PARTIALDELIVERED
            public const string SO_SHIPPED_DIRECT = "INV13"; // SO_ST_OPEN => SO_ST_DELIVERED
            public const string SO_BOOKED_CANCEL = "INV14"; // SO_ST_OPEN => SO_ST_CANCEL
            public const string SO_WAITING_FAILED = "INV15";  // SO_ST_WAITINGSHIPPING => SO_ST_FAILED
            public const string SO_PICKING_FAILED = "INV16"; // SO_ST_SHIPPING  => SO_ST_FAILED
            public const string SO_GIVEBACK = "INV21";
            public const string SO_RECALL = "INV20";
        }


        #region Promotion
        public class PROMO_PROMOTIONTYPECONST
        {
            public const string Promotion = "Promotion";
            public const string Display = "Display";
            public const string Accumulate = "Accumulate";
        }
        public class PROMO_ITEMSCOPECONST
        {
            public const string LINE = "LINE";
            public const string GROUP = "GROUP";
            public const string BUNDLE = "BUNDLE";
        }
        public class PROMO_BYBREAKDOWNCONST
        {
            public const string QUANTITY = "QUANTITY";
            public const string AMOUNT = "AMOUNT";
        }
        public class PROMOTIONCHECKBY
        {
            /// <summary>
            /// Khuyến mãi số lượng
            /// </summary>
            public const bool QUANTITY = true;
            /// <summary>
            /// Khuyến mãi theo tiền
            /// </summary>
            public const bool AMOUNT = false;
        }

        public class PROMO_GIVINGTYPECONST
        {
            public const string FREEITEM = "FREEITEM";
            public const string AMOUNT = "AMOUNT";
            public const string PERCENTED = "PERCENTED";
        }

        public class PROMO_RULEOFGIVING
        {
            public const string BOX = "According Box Carton";
            public const string PASSLEVEL = "According Pass Level";
        }
        #endregion

        public class REVENUE_REPORT_CONST
        {
            public const string AMOUNT = "Amount";
            public const string REVENUE = "Revenue";
        }

        public class SO_SaleOrderTypeConst
        {
            public const string SalesOrder = "SalesOrder";
            public const string ReturnOrder = "ReturnOrder";
        }

        public class FFA_ORDER_TYPE
        {
            public const string SalesOrder = "SalesOrder";
            public const string SplitOrder = "SplitOrder";
            public const string DirectOrder = "DirectOrder";
        }


        public class CustomerSettingConst
        {
            public const string CUS01 = "CUS01";
            public const string CUS02 = "CUS02";
            public const string CUS03 = "CUS03";
            public const string CUS04 = "CUS04";
            public const string CUS05 = "CUS05";
            public const string CUS06 = "CUS06";
            public const string CUS07 = "CUS07";
            public const string CUS08 = "CUS08";
            public const string CUS09 = "CUS09";
            public const string CUS10 = "CUS10";
        }


        public class ItemSettingConst
        {
            public const string Industry = "IT01";
            public const string Category = "IT02";
            public const string SubCategory = "IT03";
            public const string Brand = "IT04";
            public const string SubBrand = "IT05";
            public const string PackSize = "IT06";
            public const string PackType = "IT07";
            public const string Packaging = "IT08";
            public const string Weight = "IT09";
            public const string Volume = "IT10";
        }

        public class HierarchyLevelConst
        {
            public const string IT01 = "Industry";
            public const string IT02 = "Category";
            public const string IT03 = "SubCategory";
            public const string IT04 = "Brand";
            public const string IT05 = "SubBrand";
            public const string IT06 = "PackSize";
            public const string IT07 = "PackType";
            public const string IT08 = "Packaging";
            public const string IT09 = "Weight";
            public const string IT10 = "Volume";
        }


        public class PBPReportViewByConst
        {
            public const string DSA = "DSA";
            public const string RouteZone = "Route Zone";

        }

        public class TerritorySettingConst
        {
            public const string Branch = "TL01";
            public const string Region = "TL02";
            public const string SubRegion = "TL03";
            public const string Area = "TL04";
            public const string SubArea = "TL05";
            // public const string SubArea = "TL05";

        }
        public const string ReasonCodePrefix = "RS";


        public class TP_GIFT_TYPE
        {
            public const string SKU = "01";
            public const string ITEMGROUP = "02";
            public const string ITEMHIERARCHY = "03";
        }

        public class TP_SALE_TYPE
        {
            public const string SKU = "01";
            public const string ITEMGROUP = "02";
            public const string ITEMHIERARCHY = "03";

        }

        public class UOM_CONV_RETURN
        {
            public const string BASEUOM = "01";
            public const string SALESUOM = "02";

        }
        public class SO_SOURCE_CONST
        {
            public const string MOBILE = "FFA";
            public const string NOTMOBILE = "RDOS";
            public const string ONESHOP = "ONESHOP";
        }

        public const string BL_CANCEL_REASON_CODE = "STBL";

        public class IMPORT_STATUS
        {
            public const string SUCCESS = "S";
            public const string FAILED = "F";
            public const string NULL = "null";
        }

        public class BUDGET_BOOK_OPTION
        {
            public const string F = "F";
            public const string FP = "FP";
            public const string P = "P";
        }

        public static class AllocateType
        {
            public const string KIT = "KIT";
            public const string GROUP = "Group";
            public const string SKU = "SKU";
            public const string ATTRIBUTE = "ATTRIBUTE";
        }

        public static class FFASOSTATUS
        {
            public const string WatingImport = "FFA_SO_00";
            public const string ImportSuccessfully = "FFA_SO_01";
            public const string NeedConfirm = "FFA_SO_02";
            public const string WatingStock = "FFA_SO_03";
            public const string WatingBudget = "FFA_SO_04";
            public const string WatingBudgetStock = "FFA_SO_05";
            public const string ReConfirmed = "FFA_SO_06";
            public const string CanCelImport = "FFA_SO_07";
        }

        public static class OSSOSTATUS
        {
            public const string OSCancel = "OS_ST_07";
            public const string SOCancel = "OS_ST_06";
        }

        public static class OD_Constant
        {
            public const string DEFAULT_SCHEMA = "public";
            public static string DistributorCode { get; set; } = null;
            public static string SchemaName { get; set; } = "public";
            public static string UserLogin { get; set; } = null;
            public static string UserToken { get; set; } = null;

            public const string KeyHeader = "DistributorCode";
        }

        public static bool IsODSiteConstant = false;
        public static string LinkODSystem = null;
        public const string PrincipleCodeServiceUrlConstant = "principal";
        public static bool IsDevelopEnv = Environment.GetEnvironmentVariable("DIGI_ENVIRONMENT")?.ToLower() == "development";

        public static class OwnerTypeConstant
        {
            public const string SYSTEM = "SYSTEM";
            public const string PRINCIPAL = "PRINCIPAL";
            public const string DISTRIBUTOR = "DISTRIBUTOR";
        }

        public static class SORECALLFROMTYPE
        {
            public const string PRINCIPAL = "PRINCIPAL";
            public const string DISTRIBUTOR = "DISTRIBUTOR";
        }

        public static class SORECALLTYPE
        {
            public const string SKU = "SKU";
            public const string ITEMGROUP = "ITEMGROUP";
            public const string ITEMATTRIBUTE = "ITEMATTRIBUTE";
        }
        public static class SORECALLSTATUS
        {
            public const string NEW = "NEW";
            public const string RELEASED = "RELEASED";
        }
        public static class SORECALLSCOPETYPE
        {
            public const string SALEAREA = "SALEAREA";
            public const string DISTRIBUTOR = "DISTRIBUTOR";
        }

        public static class DataType
        {
            public const string SORECALLREQ_SYNC = "SORECALLREQ_SYNC";
            public const string SORECALL_SYNC = "SORECALL_SYNC";
        }

        public static class RequestType
        {
            public const string INSERT = "Insert";
            public const string UPDATE = "Update";
            public const string DELETE = "Delete";
        }

        public static class HistoryStatus
        {
            public const string SUCCESS = "SUCCESS";
            public const string FAILED = "FAILED";
        }

        public class PriorityStandard
        {
            public const int Priority = 1;
            public const int PriorityByTime = 2;
            public const int Ratio = 3;
        }

        public class OSNotificationType
        {
            public const string NORMAL = "NORMAL";
        }
        public class OSNotificationNavigateType
        {
            public const string OS_ORDER = "OS_ORDER";
        }
        public class OSNotificationPriority
        {
            public const string WARNING = "WARNING";
        }

        public class OSNotificationPurpose
        {
            public const string OS_UPDATEORDER = "OS_UPDATEORDER";
            /// <summary>
            /// SO_ST_DELIVERED | 
            /// SO_ST_PARTIALDELIVERED
            /// </summary>
            public const string OS_COMPLETEDORDER = "OS_COMPLETEDORDER";
            /// <summary>
            /// SO_ST_CANCEL
            /// </summary>
            public const string OS_CANCELORDER = "OS_CANCELORDER";
            /// <summary>
            /// SO_ST_SHIPPING
            /// </summary>
            public const string OS_SHIPPINGORDER = "OS_SHIPPINGORDER";
            /// <summary>
            /// OS_SO_01
            /// </summary>
            public const string OS_PROCESSINGORDER = "OS_PROCESSINGORDER";
            /// <summary>
            /// OS_SO_02
            /// OS_SO_03
            /// OS_SO_04
            /// </summary>
            public const string OS_CONFRIMORDER = "OS_CONFRIMORDER";

            /// <summary>
            /// Lấy mapping purpose từ Status của SO_OrderInformations
            /// </summary>
            /// <param name="OrderStatusCode">Là Status của SO_OrderInformations</param>
            /// <returns></returns>
            public static string GetPurpose(string OrderStatusCode)
            {
                switch (OrderStatusCode)
                {
                    case SO_SaleOrderStatusConst.CANCEL:
                        return OS_CANCELORDER;
                    case SO_SaleOrderStatusConst.OUTOFBUDGET:
                    case SO_SaleOrderStatusConst.OUTOFSTOCK:
                    case SO_SaleOrderStatusConst.OUTOFSTOCKBUDGET:
                        return OS_CONFRIMORDER;
                    case SO_SaleOrderStatusConst.IMPORTSUCCESSFULLY:
                        return OS_PROCESSINGORDER;
                    case SO_SaleOrderStatusConst.SHIPPING:
                        return OS_SHIPPINGORDER;
                    case SO_SaleOrderStatusConst.DELIVERED:
                    case SO_SaleOrderStatusConst.PARTIALDELIVERED:
                        return OS_COMPLETEDORDER;
                    default: return OS_UPDATEORDER;
                }
            }
        }

        public static class PromotionSetting
        {
            // Promotion Type
            public const string PromotionByProduct = "01";
            public const string ProductGroups = "02";
            public const string ProductSets = "03";
            public const string AccordingToOrderValue = "04";

            //ProgramType
            public const string PromotionProgram = "01";
            public const string DiscountProgram = "02";

            // Status Budget for Promotion
            public const string StatusDefining = "01";
            public const string StatusCanLinkPromotion = "02";
            public const string StatusLinkedPromotion = "03";

            // Scope
            public const string ScopeNationwide = "01";
            public const string ScopeSalesTerritoryLevel = "02";
            public const string ScopeDSA = "03";

            // Applicable Object
            public const string ObjectAllCustomer = "01";
            public const string ObjectCustomerAttributes = "02";
            public const string ObjectCustomerShipto = "03";

            //PromotionProductType
            public const string SKU = "01";
            public const string ItemGroup = "02";
            public const string ItemHierarchyValue = "03";

            // status Promotion
            public const string Inprogress = "01";
            public const string WaitConfirm = "02";
            public const string Confirmed = "03";
            public const string Refuse = "04";

            public const int AccordingToTheProgram = 1;
            public const int SaleCalendar = 2;

            // Discount Type
            public const int DiscountAmount = 1;
            public const int DiscountPercent = 2;
        }

        public static long LocationCodeAllowBoook = 1;
    }
}