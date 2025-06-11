using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;
using Microsoft.AspNetCore.Mvc;
using nProx.Helpers.Dapper;
using nProx.Helpers.Services.Paging;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models.PrincipalModel;
using ODSaleOrder.API.Models;
using nProx.Helpers.Models;
using System.Collections.Generic;
using System.Reflection;
using System;
using DynamicSchema.Helper.Models;
using System.Linq;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    //[Authorize]
    public class DeliveryNoteController : ControllerBase
    {
        private readonly IDapperRepositories _dapperRepositories;
        private readonly ISchemaNavigateService<ODDistributorSchema> _schemaNavigateService;
        private string? _schemaName = "public";
        private readonly IPagingService _pagingService;
        private readonly IDynamicBaseRepository<InvoiceOrder> _service;

        public DeliveryNoteController(RDOSContext dbContext, IDapperRepositories dapperRepositories, IPagingService pagingService)
        {
            _schemaNavigateService = new SchemaNavigateService<ODDistributorSchema>(dbContext);
            _dapperRepositories = dapperRepositories;
            _pagingService = pagingService;
            _service = new DynamicBaseRepository<InvoiceOrder>(dbContext); ;
        }

        private async void TrySetSchemaName(string DistributorCode)
        {
            string text = DistributorCode;
            if (text == "public")
            {
                _schemaName = "public";
                return;
            }

            ResultModelWithObject<ODDistributorSchema> resultModelWithObject = await _schemaNavigateService.NavigateSchemaByDistributorCode(text);
            if (resultModelWithObject.IsSuccess)
            {
                PropertyInfo property = resultModelWithObject.Data.GetType().GetProperty("SchemaName");
                if (property != null)
                {
                    _schemaName = property.GetValue(resultModelWithObject.Data) as string;
                }
            }
            else
            {
                _schemaName = null;
            }
        }

        [HttpPost]
        [Route("GetReport")]
        public IActionResult GetReport(DeliveryNoteRequest inp)
        {
            Result<List<Delivery>> result = new Result<List<Delivery>>();
            try
            {
                List<Delivery> deliveryList = new();
                TrySetSchemaName(inp.DistributorCode);
                foreach (var OrderRefNumber in inp.OrderRefNumbers)
                {
                    Delivery delivery = new Delivery();
                    delivery.OrderRefNumber = OrderRefNumber;
                    string query = $@"SELECT  soi.""PrintedDeliveryNoteCount"",soi.""DistributorCode"", db.""Name"" as ""DisName"", db.""BussinessFullAddress"" ""DisAddress"", db.""AttentionPhoneValue"" as ""DisPhone"", 
                                db.""LogoFilePath"" as ""DisLogo"", soi.""LastedDeliveryNotePrintDate"" as ""NgayIn"", soi.""SalesRepID"" as ""MaSM"", emp.""FullName"" as ""TenSM"", emp.""MainPhoneNumber"" as ""SDTSM"", plh.""DriverCode"", 
                                driveremp.""FullName"" ""DriverName"", driveremp.""MainPhoneNumber"" ""DriverPhone"", soi.""CustomerId"" ""MaKhachHang"", soi.""CustomerName"" ""TenKhachHang"", soi.""CustomerPhone"" ""SDTKhachHang"",
                                soi.""OrderDate"" ""NgayDonHang"", soi.""LastedDeliveryNotePrintDate"" ""NgayGiaoHang"", soi.""CustomerAddress"" ""DiaChiKhachHang"", soi.""Note"" ""GhiChu"", db.""BankName"" ""TenNganHang"", 
                                db.""BankAccount"" ""TenTaiKhoan"", db.""BankNumber"" ""STK""
                                from ""{_schemaName}"".""SO_OrderInformations"" soi
                                  left join ""Distributors"" db on soi.""DistributorCode"" = db.""Code""
                                  left join ""PrincipleEmployees"" emp on emp.""EmployeeCode"" = soi.""SalesRepID""
                                  left join ""{_schemaName}"".""SO_SumPickingListDetails"" detail on soi.""OrderRefNumber"" = detail.""OrderRefNumber""
                                  left join ""{_schemaName}"".""SO_SumPickingListHeaders"" plh on detail.""SumPickingRefNumber"" = plh.""SumPickingRefNumber""
                                  left join ""{_schemaName}"".""PrincipleEmployees"" driveremp on driveremp.""EmployeeCode"" = plh.""DriverCode"" 
                                WHERE soi.""OrderRefNumber"" = '{OrderRefNumber}' ";

                    var headers = (List<DeliveryNote>)_dapperRepositories.Query<DeliveryNote>(query);
                    var header = headers.FirstOrDefault();
                    query = @$"WITH raws1 AS (
  SELECT
    oi.""InventoryID"" || ' - ' || t1.""Description"" AS ""SanPham"",
    oi.""IsFree"",
    oi.""UOMDesc"",
    t1.""BaseUnit"",
    t1.""SalesUnit"",
    t1.""PurchaseUnit"",
    t1.""Id"" AS ""InventoryId"",
    SUM(oi.""OrderBaseQuantities"") AS ""OrderBaseQuan"",
    SUM(
      CASE
        WHEN oi.""Ord_Line_TotalBeforeTax_Amt"" IS NOT NULL AND oi.""Ord_Line_TotalBeforeTax_Amt"" > 0 
        THEN oi.""Ord_Line_TotalBeforeTax_Amt""
        ELSE 0
      END
    ) AS ""TongThanhTienTruocThue"",
    SUM(
      CASE
        WHEN oi.""Ord_Line_TotalAfterTax_Amt"" IS NOT NULL AND oi.""Ord_Line_TotalAfterTax_Amt"" > 0 
        THEN oi.""Ord_Line_TotalAfterTax_Amt""
        ELSE 0
      END
    ) AS ""TongThanhTienSauThue"",   
    MAX(
      CASE
        WHEN oi.""UnitPriceBeforeTax"" IS NOT NULL AND oi.""UnitPriceBeforeTax"" > 0 
        THEN oi.""UnitPriceBeforeTax""
        ELSE 0
      END
    ) AS ""DonGiaTruocThue"",
    MAX(
      CASE
        WHEN oi.""UnitPriceAfterTax"" IS NOT NULL AND oi.""UnitPriceAfterTax"" > 0 
        THEN oi.""UnitPriceAfterTax""
        ELSE 0
      END
    ) AS ""DonGiaSauThue""
FROM
    ""{_schemaName}"".""SO_OrderItems"" oi
LEFT JOIN ""public"".""InventoryItems"" t1 ON t1.""InventoryItemId"" = oi.""InventoryID""
WHERE
    oi.""OrderRefNumber"" = '{OrderRefNumber}'
GROUP BY
    oi.""IsFree"",oi.""InventoryID"", t1.""Description"", oi.""UOMDesc"",
    t1.""BaseUnit"",
    t1.""SalesUnit"",
    t1.""PurchaseUnit"",
    t1.""Id""
)
,raws2 AS (
  SELECT t1.*
  ,CASE WHEN t1.""PurchaseUnit"" = t1.""SalesUnit"" THEN t1.""OrderBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer ELSE t1.""OrderBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""SLThung""
  ,(t1.""OrderBaseQuan""%t2.""ConversionFactor""::integer) ::integer AS ""SLLocTmp""
  FROM raws1 t1
  LEFT JOIN public.""ItemsUOMConversions"" t2 on t2.""ItemID""=t1.""InventoryId"" AND t2.""ToUnit""=t1.""BaseUnit"" AND t2.""FromUnit""=t1.""PurchaseUnit""
)
,FINAL AS(
SELECT
  t1.""SanPham"",
  t1.""TongThanhTienTruocThue"" AS ""ThanhTienTruocThue"",
  t1.""TongThanhTienSauThue"" AS ""ThanhTienSauThue"",
  t1.""DonGiaTruocThue"",
  t1.""DonGiaSauThue"",
  t1.""UOMDesc""
  ,t1.""OrderBaseQuan"", t1.""SLThung""
  ,CASE WHEN t1.""PurchaseUnit"" = t1.""SalesUnit"" THEN 0 ::integer  ELSE t1.""SLLocTmp""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""SLLoc""
  ,CASE WHEN t1.""PurchaseUnit"" = t1.""SalesUnit"" THEN t1.""OrderBaseQuan""%COALESCE(t2.""ConversionFactor"", 1) ::integer  ELSE t1.""SLLocTmp""%COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""SLChai""
  ,(t1.""SLThung""::text || ' | ' ||
   (CASE 
      WHEN t1.""PurchaseUnit"" = t1.""SalesUnit"" 
      THEN '0'
      ELSE (t1.""SLLocTmp"" / COALESCE(t2.""ConversionFactor"", 1))::integer::text
    END) || ' | ' ||
   (CASE 
      WHEN t1.""PurchaseUnit"" = t1.""SalesUnit"" 
      THEN (t1.""OrderBaseQuan"" % COALESCE(t2.""ConversionFactor"", 1))::integer::text
      ELSE (t1.""SLLocTmp"" % COALESCE(t2.""ConversionFactor"", 1))::integer::text
    END)
  ) AS ""SoLuong""
FROM raws2 t1
LEFT JOIN public.""ItemsUOMConversions"" t2 on t2.""ItemID""=t1.""InventoryId"" AND t2.""ToUnit""=t1.""BaseUnit"" AND t2.""FromUnit""=t1.""SalesUnit""
ORDER BY t1.""SanPham""
)
 SELECT
  ""SanPham"",
  ""SoLuong"",
  ""DonGiaTruocThue"",
  ""ThanhTienTruocThue"",
  ""DonGiaSauThue"",
  ""ThanhTienSauThue"",
  ""UOMDesc""
FROM
  FINAL UNION ALL
SELECT
  'TỔNG CỘNG',
  SUM(""SLThung"") || ' | ' || SUM(""SLLoc"") || ' | ' || SUM(""SLChai"") AS ""SoLuong"",
  SUM(""DonGiaTruocThue""),
  SUM(""ThanhTienTruocThue""),
  SUM(""DonGiaSauThue""),
  SUM(""ThanhTienSauThue""),
  ''
FROM
  FINAL;";

                    var Items = (List<DeliveryItem>)_dapperRepositories.Query<DeliveryItem>(query);
                    delivery.Header = header;
                    delivery.Items = Items;

                    query = @$"SELECT 
                                  CASE WHEN ""Orig_Ord_Extend_Amt"" IS NOT NULL AND ""Orig_Ord_Extend_Amt"" > 0 THEN ""Orig_Ord_Extend_Amt""
		                                ELSE 0 END ""TienPhaiThanhToan"",
                                  CASE WHEN (""Ordline_Disc_Amt"" IS NOT NULL AND ""Ordline_Disc_Amt"" > 0) OR (""Ord_Disc_Amt"" IS NOT NULL AND ""Ord_Disc_Amt"" > 0) THEN  ""Ordline_Disc_Amt"" +  ""Ord_Disc_Amt""
		                                ELSE 0 END ""TongKhuyenMaiTien""
                                FROM 
                                    ""{_schemaName}"".""SO_OrderInformations""
                                WHERE 
                                    ""OrderRefNumber"" = '{OrderRefNumber}'";
                    var TienThanhToans = (List<TienPhaiThanhToanModel>)_dapperRepositories.Query<TienPhaiThanhToanModel>(query);
                    delivery.TienPhaiThanhToan = TienThanhToans.FirstOrDefault();
                    query = @$"SELECT
(SELECT BOOL_OR(""IsFree"")
                            FROM ""{_schemaName}"".""SO_OrderItems"" oi
                            WHERE oi.""OrderRefNumber"" = '{OrderRefNumber}') AS ""HasTangHang"",

(SELECT CASE
	WHEN (
		SELECT
                                        SUM(CASE
                                            WHEN oi.""Ord_line_Disc_Amt"" > 0
                                            AND (oi.""Shipped_line_Disc_Amt"" = 0 OR oi.""Shipped_line_Disc_Amt"" IS NULL) THEN oi.""Ord_line_Disc_Amt""
				ELSE 0
                                        END)
		FROM
                                        ""{_schemaName}"".""SO_OrderItems"" oi
		WHERE
                                        oi.""OrderRefNumber"" = '{OrderRefNumber}'
	) > 0 THEN TRUE
	ELSE FALSE
END) AS ""HasTangTienChietKhau"",

(SELECT CASE
	WHEN (
		SELECT
                                        SUM(CASE
                                            WHEN oi.""Shipped_line_Disc_Amt"" > 0 THEN oi.""Shipped_line_Disc_Amt""
				ELSE 0
                                        END)
		FROM
                                        ""{_schemaName}"".""SO_OrderItems"" oi
		WHERE
                                        oi.""OrderRefNumber"" = '{OrderRefNumber}'
	) > 0 THEN TRUE
	ELSE FALSE
END) AS ""HasTangTienKhuyenMai"";";
                    var Status = (List<DeliveryStatus>)_dapperRepositories.Query<DeliveryStatus>(query);
                    var stat = Status.FirstOrDefault();

                    if (stat.HasTangHang)
                    {
                        query = @$"SELECT 
    ""PromotionCode"" || ' - ' || ""PromotionDescription"" AS ""Name"",
    ""ItemCode"" || ' - ' || ""ItemDescription"" AS ""SanPhamTang"",
    CASE 
        WHEN ""BaseUnitCode"" = ""UOM"" THEN 'CHAI'
        WHEN ""UOM"" = ""PurchaseUnitCode"" THEN 'THUNG'
        WHEN ""UOM"" = ""SalesUnitCode"" THEN 'LOC'
        ELSE 'CHAI' 
    END AS ""UOM"",
    ""OrderQuantities"",
    CASE 
        WHEN ""BaseUnitCode"" = ""UOM"" THEN '0 | 0 | ' || ""OrderQuantities""
        WHEN ""UOM"" = ""PurchaseUnitCode"" THEN ""OrderQuantities"" || ' | 0 | 0'
        WHEN ""UOM"" = ""SalesUnitCode"" THEN '0 | ' || ""OrderQuantities"" || ' | 0'
        ELSE '0 | 0 | ' || ""OrderQuantities""
    END AS ""Quantity""
FROM 
    ""{{_schemaName}}"".""SO_OrderItems""
WHERE 
    ""OrderRefNumber"" = '{{OrderRefNumber}}' AND
    ""IsFree"" IS TRUE

UNION ALL

-- Dòng tổng
SELECT 
    'TOTAL' AS ""Name"",
    '' AS ""SanPhamTang"",
    '' AS ""UOM"",  
    SUM(""OrderQuantities"") AS ""OrderQuantities"",
    SUM(CASE 
        WHEN ""BaseUnitCode"" = ""UOM"" THEN 0 
        WHEN ""UOM"" = ""PurchaseUnitCode"" THEN ""OrderQuantities"" 
        ELSE 0 
    END) || ' | ' ||
    SUM(CASE 
        WHEN ""UOM"" = ""SalesUnitCode"" THEN ""OrderQuantities"" 
        ELSE 0 
    END) || ' | ' ||
    SUM(CASE 
        WHEN ""BaseUnitCode"" = ""UOM"" THEN ""OrderQuantities"" 
        ELSE 0 
    END) AS ""Quantity""
FROM 
    ""{{_schemaName}}"".""SO_OrderItems""
WHERE 
    ""OrderRefNumber"" = '{{OrderRefNumber}}' AND
    ""IsFree"" IS TRUE
;";
                        query = query.Replace("{_schemaName}", _schemaName).Replace("{OrderRefNumber}", OrderRefNumber);
                        var TangHangs = (List<OrderItemModel>)_dapperRepositories.Query<OrderItemModel>(query);
                        delivery.TangHang = new List<OrderItemModel>();
                        delivery.TangHang = TangHangs;
                    }

                    if (stat.HasTangTienKhuyenMai || stat.HasTangTienChietKhau)
                    {
                        query = @$"SELECT * FROM (
                                    SELECT ""PromotionCode"" ||' - ' || ""PromotionDescription"" as ""Name"", 
                                    CASE WHEN ""Ord_line_Disc_Amt"" is not null and ""Ord_line_Disc_Amt""  > 0 and (""Shipped_line_Disc_Amt"" is null or ""Shipped_line_Disc_Amt"" = 0) then ""Ord_line_Disc_Amt"" + ""DisCountAmount""
                                         ELSE ""DisCountAmount"" + ""Shipped_line_Disc_Amt"" END ""SoTienTang""
                                     from ""{_schemaName}"".""SO_OrderItems""
                                      where ""OrderRefNumber"" = '{OrderRefNumber}' ) WHERE ""SoTienTang"" > 0";

                        delivery.TangTien = new List<TangTien>();
                        delivery.TangTien = (List<TangTien>)_dapperRepositories.Query<TangTien>(query);
                    }


                    delivery.Status = stat;
                    deliveryList.Add(delivery);
                }
                result.Data = deliveryList;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Messages.Add(ex.Message);
            }
            return Ok(result);
        }


    }
}
