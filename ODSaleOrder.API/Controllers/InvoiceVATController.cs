using DynamicSchema.Helper.Models;
using DynamicSchema.Helper.Services;
using DynamicSchema.Helper.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using nProx.Helpers.Dapper;
using nProx.Helpers.Models;
using nProx.Helpers.Services.Paging;
using ODSaleOrder.API.Infrastructure;
using System.Collections.Generic;
using System.Globalization;
using System;
using System.Reflection;
using ODSaleOrder.API.Models;
using System.Linq;
using ODSaleOrder.API.Models.PrincipalModel;
using Microsoft.EntityFrameworkCore;
using Sys.Common.Utils;
using Sys.Common.JWT;
using Microsoft.AspNetCore.Authorization;
using AuthorizeAttribute = Sys.Common.JWT.AuthorizeAttribute;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoiceVATController : ControllerBase
    {
        private readonly IDapperRepositories _dapperRepositories;
        private readonly ISchemaNavigateService<ODDistributorSchema> _schemaNavigateService;
        private string? _schemaName = "public";
        private readonly IPagingService _pagingService;
        private readonly IDynamicBaseRepository<InvoiceOrder> _service;

        public InvoiceVATController(RDOSContext dbContext, IDapperRepositories dapperRepositories, IPagingService pagingService)
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
        [Route("searchwithpaging")]
        public IActionResult searchwithpaging(GeneralSearch inp)
        {
            ItemResult<List<OrderInvoiceModel>> result = new ItemResult<List<OrderInvoiceModel>>();
            try
            {
                string queryAdd = "";
                string DisCode = null;
                if (inp.SearchDynamic.ContainsKey("DistributorCode"))
                    DisCode = inp.SearchDynamic["DistributorCode"]?.ToString();
                string FromDate = null;
                if (inp.SearchDynamic.ContainsKey("FromDate"))
                    FromDate = DateTime.ParseExact(inp.SearchDynamic["FromDate"]?.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");

                string ToDate = null;
                if (inp.SearchDynamic.ContainsKey("ToDate"))
                    ToDate = DateTime.ParseExact(inp.SearchDynamic["ToDate"]?.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture).AddDays(1).ToString("yyyy-MM-dd");

                string Code = null;
                if (inp.SearchDynamic.ContainsKey("Code"))
                    Code = inp.SearchDynamic["Code"]?.ToString();

                bool? Status = null;
                if (inp.SearchDynamic.ContainsKey("Status"))
                {
                    string val = inp.SearchDynamic["Status"]?.ToString();
                    Status = val.Equals("Exported") ? true : false;
                }

                TrySetSchemaName(DisCode);

                string query = $@"SELECT 
                info.""OrderRefNumber"",
                info.""OrderDate"" as ""InvoiceDate"", 
                info.""OrderDate"",
                info.""CustomerId"", 
                info.""CustomerName"", 
                info.""DistributorCode"",       
                (SELECT sys.""SettingValue"" FROM ""public"".""SystemSettings"" sys WHERE sys.""SettingKey"" = 'INVOICEFORMCODE' LIMIT 1) as ""InvoiceFormCode"",
                dis.""Name"" as ""DistributorName"",
                info.""Owner_ID"" as ""OwnerCode"", 
                (CASE
                  WHEN info.""Owner_ID"" = info.""DistributorCode"" THEN dis.""Name""
                  ELSE
                  emp.""FullName""
                END) as ""OwnerName"",
                inv.""InvoiceNumber"",
                inv.""InvoiceDate"",
                inv.""InvoiceSignCode"",
                inv.""InvoiceReferenceNumber""
                FROM ""{_schemaName}"".""SO_OrderInformations"" info 
                LEFT JOIN ""{_schemaName}"".""SoInvoiceOrders"" inv ON inv.""OrderRefNumber"" = info.""OrderRefNumber""
                LEFT JOIN app_union_public('{_schemaName}', null::""PrincipleEmployees"") emp ON emp.""EmployeeCode"" = info.""Owner_ID"" 
                LEFT JOIN ""public"".""Distributors"" dis ON dis.""Code"" = info.""DistributorCode""
                WHERE info.""OrderRefNumber"" IS NOT NULL
                AND info.""OrderType"" IN ('SalesOrder', 'SplitOrder', 'DirectOrder')
                AND info.""Status""  IN ('SO_ST_DELIVERED', 'SO_ST_PARTIALDELIVERED')
";

                query += $"AND info.\"OrderDate\" >= '{FromDate}'\n";
                if (ToDate is not null)
                    query += $"AND info.\"OrderDate\" < '{ToDate}'\n";
                if (Code is not null)
                    query += @$"AND (LOWER(info.""CustomerId"") like '%{Code.ToLower()}%' OR LOWER(info.""CustomerName"") like '%{Code.ToLower()}%')";
                if (Status is not null)
                    query += Status ?? false ? @$"AND inv.""IsPrinted"" = true" : @$"AND (inv.""IsPrinted"" is null or  inv.""IsPrinted"" = false)";
                var trans = (List<OrderInvoiceModel>)_dapperRepositories.Query<OrderInvoiceModel>(query);
                return Ok(_pagingService.SearchSimpleHasPaging<OrderInvoiceModel>(inp, trans));
            }
            catch (Exception ex)
            {
                result.Messages.Add(ex.Message);
            }
            return Ok(result);
        }

        [HttpPut]
        [Route("Update")]
        public IActionResult Update(OrderInvoiceModel inp)
        {
            ItemResult<OrderInvoiceModel> result = new ItemResult<OrderInvoiceModel>();
            try
            {
                TrySetSchemaName(inp.DistributorCode);
                string query = @$"SELECT * FROM ""{_schemaName}"".""SoInvoiceOrders"" WHERE  ""OrderRefNumber"" = '{inp.OrderRefNumber}'";
                var raws = (List<InvoiceOrder>)_dapperRepositories.Query<InvoiceOrder>(query);
                if (raws is null || !raws.Any())
                {
                    query = @$"SELECT so.""OrderRefNumber"", 
                            so.""DistributorCode"", 
                            dis.""Name"" as ""DistributorName"",
                            dis.""BussinessFullAddress"" as ""DistributorAddress"",
                            dis.""TaxCode"" as ""DistributorTaxCode"",
                            dis.""Phone"" as ""DistributorPhone"",
                            NULL::varchar as ""DistributorFax"",
                            dis.""BankNumber"" as ""DistributorBankAccount"",
                            dis.""BankName"" as ""DistributorBankName"",
                            'S01'::varchar as ""DistributorShiptoCode"",
                            dis.""Name"" as ""DistributorShiptoName"",
                            dis.""BussinessFullAddress"" as ""DistributorShiptoAddress"",
                            so.""CustomerId"", 
                            cus.""CustomerCode"", 
                            cus.""CustomerName"", 
                            cus.""CustomerAddress"",
                            cus.""CustomerBankAccount"", 
                            cus.""CustomerBankName"", 
                            cus.""CustomerTaxCode"",
                            so.""SalesRepID"" as ""SalemanCode"",
                            so.""SalesRepName"" as ""SalemanName""
                            FROM ""{_schemaName}"".""SO_OrderInformations"" so
                            LEFT JOIN ""public"".""Distributors"" dis ON dis.""Code"" = so.""DistributorCode""
                            LEFT JOIN ""public"".""f_getcustomerbydistributorv2""('{inp.DistributorCode}') cus On cus.""CustomerCode"" = so.""CustomerId""
                            WHERE so.""OrderRefNumber"" = '{inp.OrderRefNumber}'";
                    InvoiceHeaderModel elements = ((List<InvoiceHeaderModel>)_dapperRepositories.Query<InvoiceHeaderModel>(query))?[0];
                    if (elements is null || elements.OrderRefNumber is null)
                    {
                        result.Messages.Add("Not found header");
                        return Ok(result);
                    }
                    InvoiceOrder invoice = new InvoiceOrder()
                    {
                        Id = Guid.NewGuid(),
                        OrderRefNumber = elements.OrderRefNumber,
                        CustomerAddress = elements.CustomerAddress,
                        CustomerBankAccount = elements.CustomerBankAccount,
                        CustomerBankName = elements.CustomerBankName,
                        CustomerCode = elements.CustomerCode,
                        CustomerName = elements.CustomerName,
                        CustomerTaxCode = elements.CustomerTaxCode,
                        DistributorAddress = elements.DistributorAddress,
                        DistributorBankAccount = elements.DistributorBankAccount,
                        DistributorBankName = elements.DistributorBankName,
                        DistributorCode = elements.DistributorCode,
                        DistributorFax = elements.DistributorFax,
                        DistributorName = elements.DistributorName,
                        DistributorPhone = elements.DistributorPhone,
                        DistributorShiptoAddress = elements.DistributorShiptoAddress,
                        DistributorShiptoCode = elements.DistributorShiptoCode,
                        DistributorShiptoName = elements.DistributorShiptoName,
                        DistributorTaxCode = elements.DistributorTaxCode,
                        SalemanCode = elements.SalemanCode,
                        SalemanName = elements.SalemanName,
                        InvoiceDate = inp.InvoiceDate ?? inp.OrderDate,
                        CreatedDate = DateTime.Now,
                        CreatedBy = User.GetName(),
                        InvoiceFormCode = inp.InvoiceFormCode,
                        InvoiceNumber = inp.InvoiceNumber,
                        InvoiceReferenceNumber = inp.InvoiceReferenceNumber,
                        InvoiceSignCode = inp.InvoiceSignCode,
                        IsPrinted = false,
                    };
                    _service.Insert(invoice, _schemaName);
                    result.Success = true;
                }
                else
                {
                    var raw = raws[0];
                    raw.InvoiceNumber = inp.InvoiceNumber;
                    raw.InvoiceFormCode = inp.InvoiceFormCode;
                    raw.InvoiceReferenceNumber = inp.InvoiceReferenceNumber;
                    raw.InvoiceSignCode = inp.InvoiceSignCode;
                    raw.UpdatedDate = DateTime.Now;
                    raw.UpdatedBy = User.GetName();
                    _service.Update(raw, _schemaName);
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.Messages.Add(ex.Message);
            }
            return Ok(result);
        }

        [HttpPost]
        [Route("GetInvoiceDetail")]
        [AllowAnonymous]
        public IActionResult GetInvoiceDetail(RequestInvoiceDetail inp)
        {
            Result<DetailInvoice> result = new Result<DetailInvoice>();
            try
            {
                TrySetSchemaName(inp.DistributorCode);
                DetailInvoice detail = new DetailInvoice();
                string query = @$"SELECT inv.*, ffa.""PaymentType"", ffa.""PaymentTypeDesc"", ci.""ShortName""  
                                FROM ""{_schemaName}"".""SoInvoiceOrders"" inv
                                LEFT JOIN ""CustomerInformations"" ci ON ci.""CustomerCode"" = inv.""CustomerCode""
                                LEFT JOIN  ""{_schemaName}"".""FFASoOrderInformations"" ffa ON ffa.""OrderRefNumber"" = inv.""OrderRefNumber""
                                WHERE inv.""OrderRefNumber"" = '{inp.OrderRefNumber}';";
                var header = ((List<HeaderInvoice>)_dapperRepositories.Query<HeaderInvoice>(query))?[0];
                if (header is null)
                {
                    result.Messages.Add("NotFoundYourOrderRefNum");
                    return Ok(result);
                }
                detail.Header = header;
                query = @$"SELECT 
                        so.""OrderRefNumber"",
                        so.""ItemCode"", so.""ItemDescription"",
                        so.""UOM"" as ""Uom"", so.""UOMDesc"" as ""UomDesc"",
                        so.""ShippedQuantities"" as ""OrderQuantities"",
                        so.""UnitPrice"",
                        so.""Shipped_Line_Extend_Amt"" as ""Amount"",
                        so.""VAT"" as ""VatAmount"",
                        so.""VatValue"" as ""VatPercent"",
                        so.""PromotionDescription"",
                        so.""DisCountAmount"" as ""DiscountAmount"",
                        so.""IsFree""
                        FROM ""{_schemaName}"".""SO_OrderItems"" so
                        WHERE so.""OrderRefNumber"" = '{inp.OrderRefNumber}';";
                var body = (List<BodyInvoice>)_dapperRepositories.Query<BodyInvoice>(query);
                if (header is null)
                {
                    result.Messages.Add("NotFoundYourOrderRefNum");
                    return Ok(result);
                }
                detail.Body = body;
                result.Data = detail;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Messages.Add(ex.Message);
            }
            return Ok(result);
        }

        [HttpPut]
        [Route("ChangeStatus")]
        public IActionResult ChangeStatus(OrderInvoiceModel inp)
        {
            ItemResult<OrderInvoiceModel> result = new ItemResult<OrderInvoiceModel>();
            try
            {
                TrySetSchemaName(inp.DistributorCode);
                string query = @$"SELECT * FROM ""{_schemaName}"".""SoInvoiceOrders"" WHERE  ""OrderRefNumber"" = '{inp.OrderRefNumber}'";
                var raws = ((List<InvoiceOrder>)_dapperRepositories.Query<InvoiceOrder>(query))[0];
                if (raws is not null)
                {
                    raws.IsPrinted = true;
                    _service.Update(raws, _schemaName);
                }
                result.Success = true;
            }
            catch (Exception ex)
            {

            }
            return Ok(result);
        }
    }
}
