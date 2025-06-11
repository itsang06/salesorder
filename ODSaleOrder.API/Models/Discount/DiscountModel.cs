using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;
using static SysAdmin.API.Constants.Constant;

namespace ODSaleOrder.API.Models
{
    public class DiscountModel
    {
        public Guid id { get; set; } // "082a447f-6346-403c-8b62-2d174e935d14",
        public string code { get; set; } // "1011101",
        public string shortName { get; set; } // "Chương trình chiết khấu toàn quốc",
        public string fullName { get; set; } // "Chương trình chiết khấu toàn quốc",
        public string scheme { get; set; } // "1",
        public string status { get; set; } // "03",
        public string statusDescription { get; set; } // null,
        public DateTime effectiveDateFrom { get; set; } // "2022-11-09T21:18:55",
        public DateTime? validUntil { get; set; } // null,
        public string saleOrg { get; set; } // "THPGTRDOS",
        public string scopeType { get; set; } // "01",
        public string sicCode { get; set; } // "THPBEV",
        public string applicableObjectType { get; set; } // "01",
        public int discountType { get; set; } // 2,
        public List<DiscountDetailModel> listDiscountStructureDetails { get; set; }
    }


    public class DiscountDetailModel
    {
        public Guid id { get; set; }  //"16073905-6608-478e-b62b-6279dd54ad9e",
        public string discountCode { get; set; }  //"1011101",
        public string sicCode { get; set; }  //null,
        public int discountType { get; set; }  //2,
        public string nameDiscountLevel { get; set; }  //"Mua 10.000.000 chiết khấu 10%",
        public string discountCheckValue { get; set; }  //10000000,
        public int discountAmount { get; set; }  //0,
        public decimal discountPercent { get; set; }  //10,
        public string imagePath { get; set; }  //"",
        public string imageName { get; set; }  //"",
        public string fileExt { get; set; }  //"",
        public string folderType { get; set; }  //"",
        public int deleteFlag { get; set; }  //0
    }


    public class DiscountResultModel
    {
        public string code { get; set; } //"1011101",
        public string shortName { get; set; } //"Chương trình chiết khấu toàn quốc",
        public string fullName { get; set; } //"Chương trình chiết khấu toàn quốc",
        public string levelName { get; set; } //"Mua 10.000.000 chiết khấu 10%",
        public int checkBy { get; set; } //2,
        public string levelCheckValue { get; set; } //10000000,
        public decimal levelAmount { get; set; } //0,
        public decimal levelPercent { get; set; } //10,
        public decimal discountAmount { get; set; } //1023312311.3
    }

}
