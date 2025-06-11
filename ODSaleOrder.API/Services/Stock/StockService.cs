using AutoMapper;
using Microsoft.Extensions.Logging;
using Sys.Common.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using RestSharp;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;
using static SysAdmin.API.Constants.Constant;
using ODSaleOrder.API.Services.Base;
using System.Linq;
using SysAdmin.Models.StaticValue;
using nProx.Helpers.Dapper;

namespace ODSaleOrder.API.Services.Stock
{
    public class StockService : IStockService
    {
        private readonly ILogger<StockService> _logger;
        private readonly IMapper _mapper;
        public IRestClient _client;
        public readonly IClientService _clientService;

        //private readonly IBaseRepository<UomConventionFactorModel> _uomConventionFactorRepo;

        private readonly IDapperRepositories _dapperRepositories;

        private string UserToken;

        public StockService(ILogger<StockService> logger,
            IMapper mapper,
            IClientService clientService,
            IDapperRepositories dapperRepositories
        )
        {
            _logger = logger;
            _mapper = mapper;
            _clientService = clientService;
            _dapperRepositories = dapperRepositories;
        }

        public BaseResultModel GetProductWithStock(ProductWithStockModel model, string token)
        {
            try
            {
                UserToken = token;
                var query = $@"SELECT * FROM ""public"".""f_getproductswithstock""('{model.DistributorCode}')";

                var res = (List<ItemInventoryModel>)_dapperRepositories.Query<ItemInventoryModel>(query);

                if (res.ToList().Count > 0)
                {
                    var lstGroupCode = new List<string>();
                    foreach (var item in res)
                    {
                        lstGroupCode.Add(item.ItemGroupCode);
                    }
                    _ = lstGroupCode.Distinct().ToList();

                    var salePrices = _clientService.CommonRequest<ResultModelWithObject<List<StockSalePriceModel>>>(
                        CommonData.SystemUrlCode.ODPriceAPI,
                        $"SalesBasePrice/GetSalesPrices",
                        Method.POST,
                        $"{UserToken.Split(" ").Last()}",
                        new
                        {
                            model.TerritoryStructureCode,
                            model.AttributeValue,
                            model.DSAId,
                            model.DSACode,
                            ItemGroups = lstGroupCode,
                            model.DistributorCode,
                            model.OutletAttributes
                        },
                        true
                    );
                    if (salePrices != null && salePrices.Data != null)
                    {
                        foreach (var item in res)
                        {
                            var salePrice = salePrices.Data.ToList().AsQueryable().FirstOrDefault(x => x.ItemGroupCode == item.ItemGroupCode);
                            if (salePrice != null)
                            {
                                item.Prices = salePrice.Prices;
                            }
                        }
                    }

                    var queryOumConventionFactor = $@"SELECT 
                    item.""InventoryItemId"" as ""ItemCode"", 
                    item.""Description"" as ""ItemDescription"",
                    fromunit.""UomId"" as ""FromUnit"", 
                    tounit.""UomId"" as ""ToUnit"",
                    con.""ConversionFactor""
                    FROM ""public"".""ItemsUOMConversions"" con
                    INNER JOIN ""public"".""InventoryItems"" item ON item.""Id"" = con.""ItemID"" AND item.""Status"" = '1' AND item.""Competitor"" = 'f' AND item.""OrderItem"" = 't'
                    LEFT JOIN ""public"".""Uoms"" fromunit ON fromunit.""Id"" = con.""FromUnit""
                    LEFT JOIN ""public"".""Uoms"" tounit ON tounit.""Id"" = con.""ToUnit"" ";
                    var uomConventionFactor = (List<UomConventionFactorModel>)_dapperRepositories.Query<UomConventionFactorModel>(queryOumConventionFactor);

                    if (uomConventionFactor != null && uomConventionFactor.Count > 0)
                    {
                        foreach (var item in res)
                        {
                            item.UomConventionFactor = uomConventionFactor.Where(x => x.ItemCode == item.ItemCode).ToList();
                        }
                    }

                }

                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = res
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }

        }

        public BaseResultModel GetOrderReasonList()
        {
            try
            {
                var query = $@"SELECT 
                ""ReasonCode"", ""Description"" as ""ReasonName"" FROM ""public"".""SO_Reasons"" 
                WHERE ""IsDeleted"" = 'f' AND ""Used"" = 't' ORDER BY ""ReasonCode""
                ";

                var res = (List<dynamic>)_dapperRepositories.Query<dynamic>(query);

                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = res
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
    }
}
