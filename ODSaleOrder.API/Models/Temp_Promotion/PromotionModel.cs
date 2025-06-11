using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    #region Program
    public class Temp_PromotionModel : Temp_Programs
    {
        public List<Temp_PromotionDetailsModel> Temp_ProgramsDetails { get; set; }
    }

    public class Temp_PromotionDetailsModel : Temp_ProgramsDetails
    {
        public List<Temp_ProgramDetailsItemsGroup> Temp_ProgramDetailsItemsGroup { get; set; }
        public List<Temp_ProgramDetailReward> Temp_ProgramDetailReward { get; set; }
    }
    #endregion

    #region CustomerPrograms
    public class PromotionCustomerModel : ProgramCustomers
    {
        public List<PromotionCustomerDetailsModel> ProgramsCustomerDetails { get; set; }
    }

    public class PromotionCustomerDetailsModel : ProgramCustomersDetail
    {
        public List<ProgramCustomerItemsGroup> ProgramCustomerItemsGroup { get; set; }
        public List<ProgramCustomerDetailsItems> ProgramCustomerDetailsItems { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<SO_OrderItems> ItemsConvert { get; set; }
    }
    #endregion

    public class CalDisReqModel
    {
        public decimal TotalAmt { get; set; }
    }

    public class CustomerPromotionRequestModel
    {
        public string SaleOrgCode { get; set; }
        public string SicCode { get; set; }
        public string CustomerCode { get; set; }
        public string ShiptoCode { get; set; }
        public string RouteZoneCode { get; set; }
        public string DsaCode { get; set; }
        public string Branch { get; set; }
        public string Region { get; set; }
        public string SubRegion { get; set; }
        public string Area { get; set; }
        public string SubArea { get; set; }
        public string Shipto_Attribute1 { get; set; }
        public string Shipto_Attribute2 { get; set; }
        public string Shipto_Attribute3 { get; set; }
        public string Shipto_Attribute4 { get; set; }
        public string Shipto_Attribute5 { get; set; }
        public string Shipto_Attribute6 { get; set; }
        public string Shipto_Attribute7 { get; set; }
        public string Shipto_Attribute8 { get; set; }
        public string Shipto_Attribute9 { get; set; }
        public string Shipto_Attribute10 { get; set; }
        public string DistributorCode { get; set; }
    }

    #region TP Promotion
    public class TpCustomerPromotionModel
    {
        public Guid id { get; set; }  //"2b126dba-0f26-4b3c-aad9-b6ec5f1ae374",
        public string code { get; set; }  //"LINE16",
        public string shortName { get; set; }  //"Mua 1 thùng tặng 1 chai mua từ 10 thùng mới tặng",
        public string fullName { get; set; }  //"Mua 1 thùng tặng 1 chai mua từ 10 thùng mới tặng",
        public string scheme { get; set; }  //"22LINE16",
        public string status { get; set; }  //"03",
        public string statusDescription { get; set; }  //null,
        public DateTime effectiveDateFrom { get; set; }  //"2022-12-01T20:02:26",
        public DateTime? validUntil { get; set; }  //"2022-12-30T10:00:00",
        public string saleOrg { get; set; }  //"SOGT3LV",
        public string scopeType { get; set; }  //"01",
        public string applicableObjectType { get; set; }  //"01",
        public string sicCode { get; set; }  //"SICIT0201"
    }

    public class TpPromotionDetailModel
    {
        public Guid id { get; set; }  // "2b126dba-0f26-4b3c-aad9-b6ec5f1ae374",
        public string promotionType { get; set; }  // "01",
        public string code { get; set; }  // "LINE16",
        public string shortName { get; set; }  // "Mua 1 thùng tặng 1 chai mua từ 10 thùng mới tặng",
        public string fullName { get; set; }  // "Mua 1 thùng tặng 1 chai mua từ 10 thùng mới tặng",
        public string status { get; set; }  // "03",
        public string scheme { get; set; }  // "22LINE16",
        public DateTime effectiveDateFrom { get; set; }  // "2022-12-01T20:02:26",
        public DateTime validUntil { get; set; }  // "2022-12-30T10:00:00",
        public string saleOrg { get; set; }  // "SOGT3LV",
        public string sicCode { get; set; }  // "SICIT0201",
        public int settlementFrequency { get; set; }  // 1,
        public string frequencyPromotion { get; set; }  // null,
        public string imageName1 { get; set; }  // "",
        public string imagePath1 { get; set; }  // "",
        public string imageFileExt1 { get; set; }  // "",
        public string imageFolderType1 { get; set; }  // "",
        public string imageName2 { get; set; }  // "",
        public string imagePath2 { get; set; }  // "",
        public string imageFileExt2 { get; set; }  // "",
        public string imageFolderType2 { get; set; }  // "",
        public string imageName3 { get; set; }  // "",
        public string imagePath3 { get; set; }  // "",
        public string imageFileExt3 { get; set; }  // "",
        public string imageFolderType3 { get; set; }  // "",
        public string imageName4 { get; set; }  // "",
        public string imagePath4 { get; set; }  // "",
        public string imageFileExt4 { get; set; }  // "",
        public string imageFolderType4 { get; set; }  // "",
        public string scopeType { get; set; }  // "01",
        public string scopeSaleTerritoryLevel { get; set; }  // "",
        public bool isProgram { get; set; }  // false,
        public string fileName { get; set; }  // "",
        public string filePath { get; set; }  // "",
        public string fileExt { get; set; }  // "",
        public string folderType { get; set; }  // "",
        public string applicableObjectType { get; set; }  // "01",
        public bool promotionCheckBy { get; set; }  // true,
        public bool ruleOfGiving { get; set; }  // false,
        public bool ruleOfGivingByValue { get; set; }  // true,
        public bool isApplyBudget { get; set; }  // false,
        public string userName { get; set; }  // "SAdmin",
        public string reasonStep1 { get; set; }  // null,
        public string reasonStep2 { get; set; }  // null,
        public string reasonStep3 { get; set; }  // null,
        public string reasonStep4 { get; set; }  // null,
        public string reasonStep5 { get; set; }  // null,
        public bool isCheck { get; set; }  // false,
        // public string listScopeSalesTerritory { get; set; }  // [],
        // public string listScopeDSA { get; set; }  // [],
        // public string listCustomerSetting { get; set; }  // [],
        // public string listCustomerAttribute { get; set; }  // [],
        // public string listCustomerShipto { get; set; }  // [],
        public List<DefinitionStructureModel> listDefinitionStructure { get; set; }
    }



    public class DefinitionStructureModel
    {
        public Guid id { get; set; }  //"43c7ce36-56a3-4b18-909a-86ed413fd1fa",
        public string promotionCode { get; set; }  //"LINE16",
        public bool promotionCheckBy { get; set; }  //true,
        public string levelCode { get; set; }  //"LINE16L1",
        public string levelName { get; set; }  //"Mức 1",
        public int quantityPurchased { get; set; }  //10,
        public decimal valuePurchased { get; set; }  //0,
        public decimal onEach { get; set; }  //0,
        public string imageName1 { get; set; }  //"",
        public string imagePath1 { get; set; }  //"",
        public string imageFileExt1 { get; set; }  //"",
        public string imageFolderType1 { get; set; }  //"",
        public string imageName2 { get; set; }  //"",
        public string imagePath2 { get; set; }  //"",
        public string imageFileExt2 { get; set; }  //"",
        public string imageFolderType2 { get; set; }  //"",
        public string productTypeForSale { get; set; }  //"02",
        public string itemHierarchyLevelForSale { get; set; }  //null,
        public bool isGiftProduct { get; set; }  //true,
        public bool isDonate { get; set; }  //false,
        public bool isFixMoney { get; set; }  //true,
        public bool ruleOfGiving { get; set; }  //true,
        public bool ruleOfGivingByProduct { get; set; }  //true,
        public int ruleOfGivingByProductQuantity { get; set; }  //0,
        public string ruleOfGivingByProductPacking { get; set; }  //null,
        public bool isGiveSameProductSale { get; set; }  //true,
        public string productTypeForGift { get; set; }  //"02",
        public string itemHierarchyLevelForGift { get; set; }  //null,
        public bool isApplyBudget { get; set; }  //false,
        public decimal amountOfDonation { get; set; }  //0,
        public decimal percentageOfAmount { get; set; }  //0,
        public string budgetForDonation { get; set; }  //null,
        public int onEachQuantity { get; set; }  //: 10,
        public decimal onEachValue { get; set; }  //: 0,
        // public string BudgetCode { get; set; }
        // public string BudgetType { get; set; }
        // public string budgetAllocationLevel { get; set; }
        public string budgetCodeForGift { get; set; } // null,
        public string budgetTypeOfGift { get; set; } // null,
        public string budgetAllocationLevelOfGift { get; set; } // null,
        public string budgetCodeForDonate { get; set; } // "062002",
        public string budgetTypeOfDonate { get; set; } // "02",
        public string budgetAllocationLevelOfDonate { get; set; } // "TL04",
        public bool Allowance { get; set; }

        public List<ProductForSalesModel> listProductForSales { get; set; }
        public List<ProductForGiftModel> listProductForGifts { get; set; }
    }


    public class ProductForSalesModel
    {
        public Guid id { get; set; }   //"4d36dc5d-d3af-4ed1-99ec-22836d58dd71",
        public string promotionCode { get; set; }   //"LINE16",
        public string levelCode { get; set; }   //"LINE16L1",
        public string productType { get; set; }   //"02",
        public string productCode { get; set; }   //"0173q1l1t1",
        public string productDescription { get; set; }   //"Cola 24 chai 1 thùng RGB 240 ml",
        public string packing { get; set; }   //"THUNG",
        public string packingDescription { get; set; }   //"Thùng",
        public int sellNumber { get; set; }   //0,
        public string itemHierarchyLevel { get; set; }   //null,
                                                         // public string listUom { get; set; }   //[]
    }


    public class ProductForGiftModel
    {
        public Guid id { get; set; }  //"78f7b7a4-1ffc-4c67-b334-318638091710",
        public string promotionCode { get; set; }  //"LINE16",
        public string levelCode { get; set; }  //"LINE16L1",
        public string productCode { get; set; }  //"0173q1l1t1",
        public string productDescription { get; set; }  //"Cola 24 chai 1 thùng RGB 240 ml",
        public string packing { get; set; }  //"CHAI",
        public string packingDescription { get; set; }  //"Chai",
        public int numberOfGift { get; set; }  //1,
        public string budgetCode { get; set; }  //"",
        public string budgetName { get; set; }  //"",
        public bool isDefaultProduct { get; set; }  //false,
        public int exchange { get; set; }  //0,
        public string itemHierarchyLevel { get; set; }  //null,
        public string productType { get; set; }  //"02",
                                                 // public string listUom { get; set; }  //[]
    }

    public class RewardItemChangeRequestModel
    {
        public string PromotionCode { get; set; }
        public string DetailCode { get; set; }
        public List<string> ExcludedProduct { get; set; }
    }


    #endregion

    public class PromoRefRequestModel
    {
        public string Username { get; set; }
    }

    public class BudgetReqModel
    {
        public string budgetCode { get; set; }                             //: "string",
        public string budgetType { get; set; }                             //: "string",
        public string customerCode { get; set; }                             //: "string",
        public string customerShipTo { get; set; }                             //: "string",
        public string saleOrg { get; set; }                             //: "string",
        public string budgetAllocationLevel { get; set; }                             //: "string",
        public float budgetBook { get; set; }                             //: 0,
        public string salesTerritoryValueCode { get; set; }                             //: "string",
        public string promotionCode { get; set; }                             //: "string",
        public string promotionLevel { get; set; }                             //: "string",
        public string routeZoneCode { get; set; }                             //: "string",
        public string dsaCode { get; set; }                             //: "string",
        public string subAreaCode { get; set; }                             //: "string",
        public string areaCode { get; set; }                             //: "string",
        public string subRegionCode { get; set; }                             //: "string",
        public string regionCode { get; set; }                             //: "string",
        public string branchCode { get; set; }                             //: "string",
        public string nationwideCode { get; set; }                             //: "string",
        public string salesOrgCode { get; set; }                             //: "string",
        public string referalCode { get; set; }                             //: "string",
        public string distributorCode { get; set; }
    }


    public class BudgetResModel
    {
        public string budgetCode { get; set; } //STRING
        public string referalCode { get; set; } //STRING
        public string budgetType { get; set; } //STRING
        public string customerCode { get; set; } //STRING
        public string customerShipTo { get; set; } //STRING
        public string promotionCode { get; set; } //STRING
        public string promotionLevel { get; set; } //STRING
        public float budgetBook { get; set; } //FLOAT
        public float budgetBooked { get; set; } //FLOAT
        public bool budgetBookOver { get; set; } //BOOLEAN
        public bool status { get; set; } //BOOLEAN
        public string message { get; set; } //BOOLEAN
    }

    public class BudgetDetail
    {
        public string budgetCode { get; set; } //: "string",
        public string budgetType { get; set; } //: "string",
        public string budgetAllocationLevel { get; set; } //: "string",
        public string promotionCode { get; set; } //: "string",
        public string promotionLevel { get; set; } //: "string",
        public string customerCode { get; set; } //: "string",
        public string customerShiptoCode { get; set; } //: "string",
        public string salesOrgCode { get; set; } //: "string",
        public string routeZoneCode { get; set; } //: "string",
        public string dsaCode { get; set; } //: "string",
        public string subAreaCode { get; set; } //: "string",
        public string areaCode { get; set; } //: "string",
        public string subRegionCode { get; set; } //: "string",
        public string regionCode { get; set; } //: "string",
        public string branchCode { get; set; } //: "string",
        public string nationwideCode { get; set; } //: "string",
        public string referalCode { get; set; } //: "string",
        public float budgetBook { get; set; } //: 0
    }
}
