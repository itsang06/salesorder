using Dapper;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DynamicSchema.Helper.Models;
using DynamicSchema.Helper.Models.Header;
using DynamicSchema.Helper.Services;
using DynamicSchema.Helper.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.PrincipalModel;
using ODSaleOrder.API.Services;
using ODSaleOrder.API.Services.Dapper;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using Sys.Common.JWT;
using Sys.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    public class SumpickingController : ControllerBase
    {
        private readonly ISumpickingService _service;

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;
        static AsyncLocker<string> AsyncLocker = new AsyncLocker<string>();
        private readonly IDapperRepositories _dapperRepositories;
        private readonly ISchemaNavigateService<ODDistributorSchema> _schemaNavigateService;
        private string? _schemaName = "public";

        public SumpickingController(RDOSContext dbContext, IDapperRepositories dapperRepositories, IHttpContextAccessor contextAccessor, ISumpickingService service)
        {
            _schemaNavigateService = new SchemaNavigateService<ODDistributorSchema>(dbContext);
            _dapperRepositories = dapperRepositories;
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
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
        [Route("Insert")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> Insert(SumpickingModel model)
        {
            return Ok(await _service.Insert(model, _token, User.GetName()));
        }

        [HttpPut]
        [Route("Update")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> Update(SumpickingModel model)
        {
            using (await AsyncLocker.LockAsync(model.SumPickingRefNumber))
            {
                return Ok(await _service.Update(model, _token, User.GetName()));
            }

        }

        [HttpPost]
        [Route("GetDetail")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> GetDetail(SumpickingDetailQueryModel query)
        {
            return Ok(await _service.GetDetailSumpicking(query, _token));
        }

        [HttpPost]
        [Route("GetSumpickingItems")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> GetSumpickingItems(SumpickingDetailItemQueryModel model)
        {
            return Ok(await _service.GetSumpickingItems(model.OrderRefNumbers, _token));
        }
        [HttpPost]
        [Route("GetSumpickingItemsV2")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult GetSumpickingItemsV2(SumpickingDetailItemQueryModel model)
        {

            ResultModelWithObject<SumPickingListReportModel> result = new ResultModelWithObject<SumPickingListReportModel>();
            try
            {
                SumPickingListReportModel sumPickingListReportModel = new SumPickingListReportModel();
                TrySetSchemaName(model.DistributorCode);

                string listOrderRefNumbers = string.Join(",", model.OrderRefNumbers.Select(x => $"'{x}'"));
                //Get ds san pham                
                var _queryInventory = $@"WITH raws AS(SELECT 
		                                t4.""InventoryID"" AS ""InventoryCode"", t5.""Description"" AS ""InventoryName""
		                                ,t9.""Description"" AS ""ItemAttributeDesc""
		                                ,t5.""BaseUnit"", t5.""SalesUnit"", t5.""PurchaseUnit"", t5.""Id"" AS ""InventoryId""
		                                ,SUM(t4.""OrderBaseQuantities"")::integer AS ""OrderBaseQuan""	
		                                ,SUM(t4.""ShippedBaseQuantities"")::integer AS ""ShippedBaseQuan""
		                                ,SUM(t4.""FailedBaseQuantities"")::integer AS ""FailedQuan""
		                                FROM {_schemaName}.""SO_OrderItems"" t4
		                                LEFT JOIN ""InventoryItems"" t5 on t5.""InventoryItemId""=t4.""InventoryID""
		                                LEFT JOIN public.""ItemAttributes"" t9 on t9.""Id"" = t5.""Attribute6""
		                                WHERE t4.""OrderRefNumber"" IN ({listOrderRefNumbers})
		                                GROUP BY t4.""InventoryID"", t5.""Description""
		                                ,t9.""Description"",t5.""BaseUnit"", t5.""SalesUnit"", t5.""PurchaseUnit"", t5.""Id""                      
	                                ),
                                  NumOfUOMConvert AS (
                                    SELECT ""ItemID"", COUNT(*) AS ""SLItemUOM"" FROM public.""ItemsUOMConversions""
                                    GROUP BY ""ItemID""
                                  ),  
                                  RankedOfUOMConvert  AS (
                                    SELECT *,
                                           ROW_NUMBER() OVER (PARTITION BY ""ItemID"" ORDER BY ""ConversionFactor"" DESC) AS rn
                                    FROM public.""ItemsUOMConversions""
                                  ),
  
	                                raws2 AS (
		                                SELECT t1.* 
		                                ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""OrderBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer ELSE t1.""OrderBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""OrderSLThung""
		                                ,(t1.""OrderBaseQuan""%t2.""ConversionFactor""::integer) ::integer AS ""OrderSLLocTmp""
    
		                                ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""ShippedBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer ELSE t1.""ShippedBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""ShippedSLThung""
		                                ,(t1.""ShippedBaseQuan""%t2.""ConversionFactor""::integer) ::integer AS ""ShippedSLLocTmp""

		                                ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""FailedQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer ELSE t1.""FailedQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""FailedSLThung""
		                                ,(t1.""FailedQuan""%t2.""ConversionFactor""::integer) ::integer AS ""FailedSLLocTmp""
                                    ,t3.""SLItemUOM""
		                                FROM raws t1
		                                LEFT JOIN public.""ItemsUOMConversions"" t2 on t2.""ItemID""=t1.""InventoryId"" AND t2.""ToUnit""=t1.""BaseUnit"" AND t2.""FromUnit""=t1.""PurchaseUnit""
                                    LEFT JOIN NumOfUOMConvert t3 ON t3.""ItemID""=t1.""InventoryId""
	                                )
	                                SELECT t1.*
	                                  ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN 0::integer ELSE t1.""OrderSLLocTmp""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""OrderSLLoc""
	                                  ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""OrderSLLocTmp"" ::integer  ELSE t1.""OrderSLLocTmp""%COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""OrderSLChai""

	                                  ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN 0 ::integer  ELSE t1.""ShippedSLLocTmp""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""ShippedSLLoc""
	                                  ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""ShippedBaseQuan""%COALESCE(t2.""ConversionFactor"", 1) ::integer  ELSE t1.""ShippedSLLocTmp""%COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""ShippedSLChai""

	                                  ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN 0 ::integer  ELSE t1.""FailedSLLocTmp""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""FailedSLLoc""
	                                  ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""FailedQuan""%COALESCE(t2.""ConversionFactor"", 1) ::integer  ELSE t1.""FailedSLLocTmp""%COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""FailedSLChai""
	                                FROM raws2 t1
                                  LEFT JOIN RankedOfUOMConvert t2 ON t2.""ItemID""=t1.""InventoryId"" AND t2.""FromUnit"" <> t1.""PurchaseUnit"" AND t2.rn = 2
                                  LEFT JOIN NumOfUOMConvert t3 ON t3.""ItemID""=t1.""InventoryId""
	                                ORDER BY t1.""ItemAttributeDesc"", t1.""InventoryName""
  
                                  ";
                var inventoryList = (List<SumPickingInventoryReportModel>)_dapperRepositories.Query<SumPickingInventoryReportModel>(_queryInventory);
                sumPickingListReportModel.sumPickingInventoryReports = inventoryList;

                //Get ds don hang
                var _queryOrder = $@"SELECT t1.""OrderRefNumber"", t1.""CustomerName""
	                                ,t1.""SalesRepName"" AS ""EmployeeName""
	                                ,t1.""OrderDate"", t1.""Ord_Extend_Amt"", t1.""Note""
	                                ,t2.""MainPhoneNumber"" AS ""EmployeePhone""
	                                ,t1.""IsPrintedDeliveryNote""
                                FROM {_schemaName}.""SO_OrderInformations"" t1
                                LEFT JOIN public.""PrincipleEmployees"" t2 on t2.""EmployeeCode""=t1.""SalesRepID""
                                WHERE t1.""OrderRefNumber"" IN({listOrderRefNumbers})
                                ORDER BY t1.""SalesRepName""";
                var orderList = (List<SumPickingSalesOrderReportModel>)_dapperRepositories.Query<SumPickingSalesOrderReportModel>(_queryOrder);
                sumPickingListReportModel.sumPickingSalesOrderReports = orderList;

                result.Data = sumPickingListReportModel ?? new();
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                result.IsSuccess = false;
            }
            return Ok(result);
        }

        [HttpPost]
        [Route("Search")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> Search(SumpickingSearchModel parameters)
        {
            return Ok(await _service.SearchSumpicking(parameters));
        }


        [HttpPost]
        [Route("Confirm")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> Confirm(SumpickingModel model)
        {
            using (await AsyncLocker.LockAsync(model.SumPickingRefNumber))
            {
                return Ok(await _service.Confirm(model, _token, User.GetName()));
            }
        }

        [HttpPost]
        [Route("SaveWithConfirm")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> SaveWithConfirm(SumpickingModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.SumPickingRefNumber))
            {
                using (await AsyncLocker.LockAsync(model.SumPickingRefNumber))
                {
                    return Ok(await _service.SaveWithConfirm(model, _token, User.GetName()));
                }
            }
            else
            {
                return Ok(await _service.SaveWithConfirm(model, _token, User.GetName()));
            }
        }        
        [HttpPost]
        [Route("GetSumpickingReport")]
        [MapToApiVersion("1.0")]
        public IActionResult GetSumpickingReport(SumpickingDetailQueryModelV2 model)
        {
            ResultModelWithObject<SumPickingListReportModel> result = new ResultModelWithObject<SumPickingListReportModel>();
            try
            {
                SumPickingListReportModel sumPickingListReportModel = new SumPickingListReportModel();
                TrySetSchemaName(model.DistributorCode);

                //Get header
                string _queryHeader = $@"SELECT t1.""SumPickingRefNumber"", t1.""PrintedCount"", t1.""DistributorCode"" 
	                            ,t2.""Name"" AS ""DistributorName"", t2.""AttentionPhoneValue"", t2.""BussinessFullAddress"", t2.""LogoFilePath""
	                            ,t1.""Vehicle"", t1.""VehicleLoad"", t1.""CreatedDate"", t1.""DriverCode""
	                            ,t3.""FullName"" AS ""DeliveryName"", t3.""MainPhoneNumber"" AS ""DeliveryPhone""
                            FROM {_schemaName}.""SO_SumPickingListHeaders"" t1
                            LEFT JOIN ""Distributors"" t2 on t2.""Code"" = t1.""DistributorCode""
                            LEFT JOIN {_schemaName}.""PrincipleEmployees"" t3 on t3.""EmployeeCode""=t1.""DriverCode""
                                WHERE t1.""SumPickingRefNumber""='{model.SumPickingRefNumber}'";

                var headers = (List<SumPickingHeaderReportModel>)_dapperRepositories.Query<SumPickingHeaderReportModel>(_queryHeader);
                sumPickingListReportModel.sumPickingHeaderReports = headers.FirstOrDefault();

                //Get ds nhan vien ban hang
                var _querySalesman = $@"SELECT t2.""RouteZoneID"" AS ""RouteZoneCode"", t2.""SalesRepName"" AS ""EmployeeName"",t3.""MainPhoneNumber"" AS ""EmployeePhone""
                                FROM {_schemaName}.""SO_SumPickingListDetails"" t1
                                LEFT JOIN {_schemaName}.""SO_OrderInformations"" t2 on t2.""OrderRefNumber"" = t1.""OrderRefNumber""
                                LEFT JOIN public.""PrincipleEmployees"" t3 on t3.""EmployeeCode""=t2.""SalesRepID""
                                WHERE t1.""SumPickingRefNumber""='{model.SumPickingRefNumber}'
                                GROUP BY t2.""SalesRepName"", t2.""RouteZoneID"", t3.""MainPhoneNumber""
                                ORDER BY t2.""SalesRepName""";
                var salesmanList = (List<SumPickingSalesmanReportModel>)_dapperRepositories.Query<SumPickingSalesmanReportModel>(_querySalesman);
                sumPickingListReportModel.sumPickingSalesmanReports = salesmanList;

                //Get ds don hang
                var _queryOrder = $@"SELECT t2.""OrderRefNumber"", t3.""CustomerName""
	                                ,t3.""SalesRepName"" AS ""EmployeeName""
	                                ,t3.""OrderDate"", t3.""Ord_Extend_Amt"", t3.""Note""
	                                ,t8.""MainPhoneNumber"" AS ""EmployeePhone""
                                    ,t3.""IsPrintedDeliveryNote""
                                FROM {_schemaName}.""SO_SumPickingListHeaders"" t1
                                LEFT JOIN {_schemaName}.""SO_SumPickingListDetails"" t2 on  t1.""SumPickingRefNumber""=t2.""SumPickingRefNumber""
                                LEFT JOIN {_schemaName}.""SO_OrderInformations"" t3 on t3.""OrderRefNumber"" = t2.""OrderRefNumber""
                                LEFT JOIN ""PrincipleEmployees"" t8 on t8.""EmployeeCode""=t3.""SalesRepID""
                                WHERE t1.""SumPickingRefNumber""='{model.SumPickingRefNumber}'
                                ORDER BY t3.""SalesRepName""";
                var orderList = (List<SumPickingSalesOrderReportModel>)_dapperRepositories.Query<SumPickingSalesOrderReportModel>(_queryOrder);
                sumPickingListReportModel.sumPickingSalesOrderReports = orderList;

                //Get ds san pham
                var _queryInventory = $@"WITH raws AS(SELECT t4.""InventoryID"" AS ""InventoryCode"", t5.""Description"" AS ""InventoryName"", t9.""Description"" AS ""ItemAttributeDesc""
	                                    , t5.""BaseUnit"", t5.""SalesUnit"", t5.""PurchaseUnit"", t5.""Id"" AS ""InventoryId""
										, SUM(t4.""OrderBaseQuantities"")::integer AS ""OrderBaseQuan""
										,SUM(t4.""ShippedBaseQuantities"")::integer AS ""ShippedBaseQuan""
	                                    ,SUM(t4.""FailedBaseQuantities"")::integer AS ""FailedQuan""
	                                    FROM {_schemaName}.""SO_SumPickingListHeaders"" t1
	                                    LEFT JOIN {_schemaName}.""SO_SumPickingListDetails"" t2 on  t1.""SumPickingRefNumber""=t2.""SumPickingRefNumber""
	                                    LEFT JOIN {_schemaName}.""SO_OrderInformations"" t3 on t3.""OrderRefNumber"" = t2.""OrderRefNumber""
	                                    LEFT JOIN {_schemaName}.""SO_OrderItems"" t4 on t4.""OrderRefNumber"" = t3.""OrderRefNumber""
	                                    LEFT JOIN ""InventoryItems"" t5 on t5.""InventoryItemId""=t4.""InventoryID""
	                                    LEFT JOIN public.""PrincipleEmployees"" t8 on t8.""EmployeeCode""=t3.""SalesRepID""
	                                    LEFT JOIN public.""ItemAttributes"" t9 on t9.""Id"" = t5.""Attribute6""
	                                    WHERE t1.""SumPickingRefNumber""='{model.SumPickingRefNumber}'
	                                    GROUP BY t4.""InventoryID"", t5.""Description""
	                                    ,t9.""Description"",t5.""BaseUnit"", t5.""SalesUnit"", t5.""PurchaseUnit"", t5.""Id""	
                                    ),
                                    NumOfUOMConvert AS (
                                        SELECT ""ItemID"", COUNT(*) AS ""SLItemUOM"" FROM public.""ItemsUOMConversions""
                                        GROUP BY ""ItemID""
                                    ),  
                                    RankedOfUOMConvert  AS (
                                        SELECT *,
                                            ROW_NUMBER() OVER (PARTITION BY ""ItemID"" ORDER BY ""ConversionFactor"" DESC) AS rn
                                        FROM public.""ItemsUOMConversions""
                                    ),
                                    raws2 AS (
		                                SELECT t1.* 
		                                ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""OrderBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer ELSE t1.""OrderBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""OrderSLThung""
		                                ,(t1.""OrderBaseQuan""%t2.""ConversionFactor""::integer) ::integer AS ""OrderSLLocTmp""
    
		                                ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""ShippedBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer ELSE t1.""ShippedBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""ShippedSLThung""
		                                ,(t1.""ShippedBaseQuan""%t2.""ConversionFactor""::integer) ::integer AS ""ShippedSLLocTmp""

		                                ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""FailedQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer ELSE t1.""FailedQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""FailedSLThung""
		                                ,(t1.""FailedQuan""%t2.""ConversionFactor""::integer) ::integer AS ""FailedSLLocTmp""
                                    ,t3.""SLItemUOM""
		                                FROM raws t1
		                                LEFT JOIN public.""ItemsUOMConversions"" t2 on t2.""ItemID""=t1.""InventoryId"" AND t2.""ToUnit""=t1.""BaseUnit"" AND t2.""FromUnit""=t1.""PurchaseUnit""
                                    LEFT JOIN NumOfUOMConvert t3 ON t3.""ItemID""=t1.""InventoryId""
	                                )
                                    SELECT t1.""InventoryCode"", t1.""InventoryName"", t1.""ItemAttributeDesc"", t1.""OrderSLThung"", t1.""ShippedSLThung"", t1.""FailedSLThung""
                                        ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN 0::integer ELSE t1.""OrderSLLocTmp""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""OrderSLLoc""
	                                    ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""OrderSLLocTmp"" ::integer  ELSE t1.""OrderSLLocTmp""%COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""OrderSLChai""

	                                    ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN 0 ::integer  ELSE t1.""ShippedSLLocTmp""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""ShippedSLLoc""
	                                    ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""ShippedBaseQuan""%COALESCE(t2.""ConversionFactor"", 1) ::integer  ELSE t1.""ShippedSLLocTmp""%COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""ShippedSLChai""

	                                    ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN 0 ::integer  ELSE t1.""FailedSLLocTmp""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""FailedSLLoc""
	                                    ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""FailedQuan""%COALESCE(t2.""ConversionFactor"", 1) ::integer  ELSE t1.""FailedSLLocTmp""%COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""FailedSLChai""
                                    FROM raws2 t1
                                    LEFT JOIN RankedOfUOMConvert t2 ON t2.""ItemID""=t1.""InventoryId"" AND t2.""FromUnit"" <> t1.""PurchaseUnit"" AND t2.rn = 2
                                    LEFT JOIN NumOfUOMConvert t3 ON t3.""ItemID""=t1.""InventoryId""
                                    ORDER BY t1.""ItemAttributeDesc"", t1.""InventoryName""";
                var inventoryList = (List<SumPickingInventoryReportModel>)_dapperRepositories.Query<SumPickingInventoryReportModel>(_queryInventory);
                sumPickingListReportModel.sumPickingInventoryReports = inventoryList;
                

                result.Data = sumPickingListReportModel ?? new();
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                result.IsSuccess = false;
            }
            return Ok(result);
        }

        [HttpPost]
        [Route("GetSumpickingReportV2")]
        [MapToApiVersion("1.0")]
        public IActionResult GetSumpickingReportV2(SumpickingDetailQueryModelV2 model)
        {
            ResultModelWithObject<List<SumPickingListReportModel>> result = new ResultModelWithObject<List<SumPickingListReportModel>>();
            try
            {
                List<SumPickingListReportModel> res = new List<SumPickingListReportModel>();
                SumPickingListReportModel sumPickingListReportModel = new SumPickingListReportModel();
                TrySetSchemaName(model.DistributorCode);
                string sumPickingRefNumberList = string.Empty;
                if (model.SumPickingRefNumberList != null && model.SumPickingRefNumberList.Count > 0)
                {
                    sumPickingRefNumberList = string.Join(", ", model.SumPickingRefNumberList.ConvertAll(s => $"'{s}'"));
                }
                else
                {
                    sumPickingRefNumberList = $@"'{model.SumPickingRefNumber}'";
                }

                //Get header
                string _queryHeader = $@"SELECT t1.""SumPickingRefNumber"", t1.""PrintedCount"", t1.""DistributorCode"" 
	                            ,t2.""Name"" AS ""DistributorName"", t2.""AttentionPhoneValue"", t2.""BussinessFullAddress"", t2.""LogoFilePath""
	                            ,t1.""Vehicle"", t1.""VehicleLoad"", t1.""CreatedDate"", t1.""DriverCode""
	                            ,t3.""FullName"" AS ""DeliveryName"", t3.""MainPhoneNumber"" AS ""DeliveryPhone""
                            FROM {_schemaName}.""SO_SumPickingListHeaders"" t1
                            LEFT JOIN ""Distributors"" t2 on t2.""Code"" = t1.""DistributorCode""
                            LEFT JOIN {_schemaName}.""PrincipleEmployees"" t3 on t3.""EmployeeCode""=t1.""DriverCode""
                            WHERE t1.""SumPickingRefNumber"" IN ({sumPickingRefNumberList})";

                var headers = (List<SumPickingHeaderReportModel>)_dapperRepositories.Query<SumPickingHeaderReportModel>(_queryHeader);
                foreach (var item in headers)
                {
                    SumPickingListReportModel reportModel = new SumPickingListReportModel();
                    reportModel.sumPickingHeaderReports = item;
                    res.Add(reportModel);
                }
                //sumPickingListReportModel.sumPickingHeaderReports = headers.FirstOrDefault();

                //Get ds nhan vien ban hang
                var _querySalesman = $@"SELECT t2.""RouteZoneID"" AS ""RouteZoneCode"", t2.""SalesRepName"" AS ""EmployeeName"",t3.""MainPhoneNumber"" AS ""EmployeePhone"", t1.""SumPickingRefNumber""
                                FROM {_schemaName}.""SO_SumPickingListDetails"" t1
                                LEFT JOIN {_schemaName}.""SO_OrderInformations"" t2 on t2.""OrderRefNumber"" = t1.""OrderRefNumber""
                                LEFT JOIN public.""PrincipleEmployees"" t3 on t3.""EmployeeCode""=t2.""SalesRepID""
                                WHERE t1.""SumPickingRefNumber"" IN ({sumPickingRefNumberList})
                                GROUP BY t2.""SalesRepName"", t2.""RouteZoneID"", t3.""MainPhoneNumber"", t1.""SumPickingRefNumber""
                                ORDER BY t2.""SalesRepName""";
                var salesmanList = (List<SumPickingSalesmanReportModel>)_dapperRepositories.Query<SumPickingSalesmanReportModel>(_querySalesman);
                foreach (var item in salesmanList)
                {
                    var sumPickingListReport = res.FirstOrDefault(x => x.sumPickingHeaderReports.SumPickingRefNumber == item.SumPickingRefNumber);
                    sumPickingListReport.sumPickingSalesmanReports.Add(item);
                }
                sumPickingListReportModel.sumPickingSalesmanReports = salesmanList;

                //Get ds don hang
                var _queryOrder = $@"SELECT t1.""SumPickingRefNumber"",t2.""OrderRefNumber"", t3.""CustomerName""
	                                ,t3.""SalesRepName"" AS ""EmployeeName""
	                                ,t3.""OrderDate"", t3.""Ord_Extend_Amt"", t3.""Note""
	                                ,t8.""MainPhoneNumber"" AS ""EmployeePhone""
                                    ,t3.""IsPrintedDeliveryNote""
                                FROM {_schemaName}.""SO_SumPickingListHeaders"" t1
                                LEFT JOIN {_schemaName}.""SO_SumPickingListDetails"" t2 on  t1.""SumPickingRefNumber""=t2.""SumPickingRefNumber""
                                LEFT JOIN {_schemaName}.""SO_OrderInformations"" t3 on t3.""OrderRefNumber"" = t2.""OrderRefNumber""
                                LEFT JOIN ""PrincipleEmployees"" t8 on t8.""EmployeeCode""=t3.""SalesRepID""
                                WHERE t1.""SumPickingRefNumber"" IN ({sumPickingRefNumberList})
                                ORDER BY t3.""SalesRepName""";
                var orderList = (List<SumPickingSalesOrderReportModel>)_dapperRepositories.Query<SumPickingSalesOrderReportModel>(_queryOrder);
                foreach (var item in orderList)
                {
                    var sumPickingListReport = res.FirstOrDefault(x => x.sumPickingHeaderReports.SumPickingRefNumber == item.SumPickingRefNumber);
                    sumPickingListReport.sumPickingSalesOrderReports.Add(item);
                }
                //sumPickingListReportModel.sumPickingSalesOrderReports = orderList;

                //Get ds san pham
                var _queryInventory = $@"WITH raws AS(SELECT t4.""InventoryID"" AS ""InventoryCode"", t5.""Description"" AS ""InventoryName"", t9.""Description"" AS ""ItemAttributeDesc"", t1.""SumPickingRefNumber""
	                                    , t5.""BaseUnit"", t5.""SalesUnit"", t5.""PurchaseUnit"", t5.""Id"" AS ""InventoryId""
										, SUM(t4.""OrderBaseQuantities"")::integer AS ""OrderBaseQuan""
										,SUM(t4.""ShippedBaseQuantities"")::integer AS ""ShippedBaseQuan""
	                                    ,SUM(t4.""FailedBaseQuantities"")::integer AS ""FailedQuan""
	                                    FROM {_schemaName}.""SO_SumPickingListHeaders"" t1
	                                    LEFT JOIN {_schemaName}.""SO_SumPickingListDetails"" t2 on  t1.""SumPickingRefNumber""=t2.""SumPickingRefNumber""
	                                    LEFT JOIN {_schemaName}.""SO_OrderInformations"" t3 on t3.""OrderRefNumber"" = t2.""OrderRefNumber""
	                                    LEFT JOIN {_schemaName}.""SO_OrderItems"" t4 on t4.""OrderRefNumber"" = t3.""OrderRefNumber""
	                                    LEFT JOIN ""InventoryItems"" t5 on t5.""InventoryItemId""=t4.""InventoryID""
	                                    LEFT JOIN public.""PrincipleEmployees"" t8 on t8.""EmployeeCode""=t3.""SalesRepID""
	                                    LEFT JOIN public.""ItemAttributes"" t9 on t9.""Id"" = t5.""Attribute6""
	                                    WHERE t1.""SumPickingRefNumber"" IN ({sumPickingRefNumberList})
	                                    GROUP BY t4.""InventoryID"", t5.""Description""
	                                    ,t9.""Description"",t5.""BaseUnit"", t5.""SalesUnit"", t5.""PurchaseUnit"", t5.""Id"",t1.""SumPickingRefNumber""	
                                    ),
                                    NumOfUOMConvert AS (
                                        SELECT ""ItemID"", COUNT(*) AS ""SLItemUOM"" FROM public.""ItemsUOMConversions""
                                        GROUP BY ""ItemID""
                                    ),  
                                    RankedOfUOMConvert  AS (
                                        SELECT *,
                                            ROW_NUMBER() OVER (PARTITION BY ""ItemID"" ORDER BY ""ConversionFactor"" DESC) AS rn
                                        FROM public.""ItemsUOMConversions""
                                    ),
                                    raws2 AS (
		                                SELECT t1.* 
		                                ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""OrderBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer ELSE t1.""OrderBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""OrderSLThung""
		                                ,(t1.""OrderBaseQuan""%t2.""ConversionFactor""::integer) ::integer AS ""OrderSLLocTmp""
    
		                                ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""ShippedBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer ELSE t1.""ShippedBaseQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""ShippedSLThung""
		                                ,(t1.""ShippedBaseQuan""%t2.""ConversionFactor""::integer) ::integer AS ""ShippedSLLocTmp""

		                                ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""FailedQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer ELSE t1.""FailedQuan""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""FailedSLThung""
		                                ,(t1.""FailedQuan""%t2.""ConversionFactor""::integer) ::integer AS ""FailedSLLocTmp""
                                    ,t3.""SLItemUOM""
		                                FROM raws t1
		                                LEFT JOIN public.""ItemsUOMConversions"" t2 on t2.""ItemID""=t1.""InventoryId"" AND t2.""ToUnit""=t1.""BaseUnit"" AND t2.""FromUnit""=t1.""PurchaseUnit""
                                    LEFT JOIN NumOfUOMConvert t3 ON t3.""ItemID""=t1.""InventoryId""
	                                )
                                    SELECT t1.""InventoryCode"", t1.""InventoryName"", t1.""ItemAttributeDesc"", t1.""OrderSLThung"", t1.""ShippedSLThung"", t1.""FailedSLThung"", t1.""SumPickingRefNumber""
                                        ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN 0::integer ELSE t1.""OrderSLLocTmp""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""OrderSLLoc""
	                                    ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""OrderSLLocTmp"" ::integer  ELSE t1.""OrderSLLocTmp""%COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""OrderSLChai""

	                                    ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN 0 ::integer  ELSE t1.""ShippedSLLocTmp""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""ShippedSLLoc""
	                                    ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""ShippedBaseQuan""%COALESCE(t2.""ConversionFactor"", 1) ::integer  ELSE t1.""ShippedSLLocTmp""%COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""ShippedSLChai""

	                                    ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN 0 ::integer  ELSE t1.""FailedSLLocTmp""/COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""FailedSLLoc""
	                                    ,CASE WHEN t3.""SLItemUOM"" <= 1 THEN t1.""FailedQuan""%COALESCE(t2.""ConversionFactor"", 1) ::integer  ELSE t1.""FailedSLLocTmp""%COALESCE(t2.""ConversionFactor"", 1) ::integer END AS ""FailedSLChai""
                                    FROM raws2 t1
                                    LEFT JOIN RankedOfUOMConvert t2 ON t2.""ItemID""=t1.""InventoryId"" AND t2.""FromUnit"" <> t1.""PurchaseUnit"" AND t2.rn = 2
                                    LEFT JOIN NumOfUOMConvert t3 ON t3.""ItemID""=t1.""InventoryId""
                                    ORDER BY t1.""ItemAttributeDesc"", t1.""InventoryName""";
                var inventoryList = (List<SumPickingInventoryReportModel>)_dapperRepositories.Query<SumPickingInventoryReportModel>(_queryInventory);
                foreach (var item in inventoryList)
                {
                    var sumPickingListReport = res.FirstOrDefault(x => x.sumPickingHeaderReports.SumPickingRefNumber == item.SumPickingRefNumber);
                    sumPickingListReport.sumPickingInventoryReports.Add(item);
                }
                sumPickingListReportModel.sumPickingInventoryReports = inventoryList;


                result.Data = res ?? new();
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                result.IsSuccess = false;
            }
            return Ok(result);
        }

        [HttpPost]
        [Route("UpdateSumpickingHeader")]
        public async Task<IActionResult> UpdateSumpickingHeader(SumpickingDetailQueryModelV2 model)
        {
            BaseResultModel result = new();

            try
            {
                TrySetSchemaName(model.DistributorCode);
                if(model.SumPickingRefNumberList.Count > 0 && string.IsNullOrEmpty(model.SumPickingRefNumber))
                {
                    string sumPickingRefNumber = string.Join(", ", model.SumPickingRefNumberList.ConvertAll(s => $"'{s}'"));
                    string query = $@"UPDATE {_schemaName}.""SO_SumPickingListHeaders"" SET ""PrintedCount""=""PrintedCount""+1, ""LastedPrintDate""=Now() WHERE ""SumPickingRefNumber"" IN ({sumPickingRefNumber})";

                    //var parameters = new DynamicParameters();
                    //parameters.Add("@SumPickingRefNumber", sumPickingRefNumber);
                    //int affectedRows = await _dapperRepositories.ExecuteAsync(query, parameters);
                    int affectedRows = await _dapperRepositories.ExecuteAsync(query, new DynamicParameters());
                    result.IsSuccess = affectedRows > 0;
                }
                else
                {
                    string query = $@"UPDATE {_schemaName}.""SO_SumPickingListHeaders"" SET ""PrintedCount""=""PrintedCount""+1, ""LastedPrintDate""=Now() WHERE ""SumPickingRefNumber""=@SumPickingRefNumber";

                    var parameters = new DynamicParameters();
                    parameters.Add("@SumPickingRefNumber", model.SumPickingRefNumber);
                    int affectedRows = await _dapperRepositories.ExecuteAsync(query, parameters);
                    result.IsSuccess = affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                result.IsSuccess = false;
            }
            return Ok(result);
        }
    }
}
