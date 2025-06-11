using System.Collections.Generic;
using System.Linq;
using DynamicSchema.Helper.Services;
using DynamicSchema.Helper.Services.Interface;
using Microsoft.Extensions.Logging;
using nProx.Helpers.Dapper;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models.Customer;
using ODSaleOrder.API.Models.DistributorSalesOrder;
using Sys.Common.Models;
using System;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using static SysAdmin.API.Constants.Constant;
using Sys.Common.Helper;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using AutoMapper;
using ODSaleOrder.API.Models;
using RestSharp;
using SysAdmin.Models.StaticValue;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using Microsoft.EntityFrameworkCore;
using RestSharp.Authenticators;
using DocumentFormat.OpenXml.Vml.Office;
using Nest;
using ODSaleOrder.API.Models.SalesOrder;
using ODSaleOrder.API.Services.CaculateTax;
using ODSaleOrder.API.Models.SalesOrder;
using Elastic.Apm.Api;

namespace ODSaleOrder.API.Services.DistributorOrder
{
    public class DistributorSalesOrderService : IDistributorSalesOrderService
    {
        private readonly ILogger<DistributorSalesOrderService> _logger;
        protected readonly RDOSContext _dataContext;
        private readonly IMapper _mapper;
        // private readonly IMapperData _mapperData;

        private readonly IDapperRepositories _dapperRepositories;
        private readonly IDynamicBaseRepository<SO_OrderInformations> _orderInformationsRepository;
        private readonly IDynamicBaseRepository<SO_OrderItems> _orderItemsRepository;
        private readonly IDynamicBaseRepository<SO_SalesOrderSetting> _settingOrderRepository;
        private readonly ICalculateTaxService _caculateTaxService;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        private string _distributorCode = null;

        public IRestClient _client;

        private readonly ISalesOrderService _salesOrderService;

        public DistributorSalesOrderService(
            ILogger<DistributorSalesOrderService> logger,
            RDOSContext dataContext,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IDapperRepositories dapperRepositories,
            ISalesOrderService salesOrderService,
            ICalculateTaxService caculateTaxService
        )
        {
            _logger = logger;
            _mapper = mapper;

            _dapperRepositories = dapperRepositories;

            _orderInformationsRepository = new DynamicBaseRepository<SO_OrderInformations>(dataContext);
            _orderItemsRepository = new DynamicBaseRepository<SO_OrderItems>(dataContext);
            _settingOrderRepository = new DynamicBaseRepository<SO_SalesOrderSetting>(dataContext);

            _salesOrderService = salesOrderService;

            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
            _distributorCode = _httpContextAccessor.HttpContext?.Items["DistributorCode"] as string ?? _distributorCode;
            _caculateTaxService = caculateTaxService;
        }

        public BaseResultModel GetCustomerByDistributorPaging(SearchCustomerModel input, string DistributorCode)
        {
            try
            {
                var query = $@"
                SELECT * FROM ""public"".""f_getcustomerbydistributorpaging""(
                    '{DistributorCode}',
                    {(string.IsNullOrEmpty(input.SearchValue) ? "NULL" : $"'{input.SearchValue}'")},
                    {input.PageNumber},
                    {input.PageSize}
                )";

                var res = (List<DistributorCustomerModel>)_dapperRepositories.Query<DistributorCustomerModel>(query);
                int totalCount = res.Any() ? res.First().TotalCount : 0;
                int skip = (input.PageNumber - 1) * input.PageSize;
                int top = input.PageSize;

                var result = new PagedList<DistributorCustomerModel>(res, totalCount, (skip / top) + 1, top);
                var repsonse = new ListDistributorCustomerModel { Items = result, MetaData = result.MetaData };

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = repsonse
                };
            }
            catch (System.Exception ex)
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

        public BaseResultModel GenerateOrderRefNumber(string DistributorCode)
        {
            try
            {
                var prefix = StringsHelper.GetPrefixYYM();
                var orderRefNumberIndb = _orderInformationsRepository.Find(x => x.OrderRefNumber.Contains(prefix), _schemaName).Select(x => x.OrderRefNumber).OrderByDescending(x => x).FirstOrDefault();
                var generatedNumber = StringsHelper.GennerateCodeWithYearMonthFormat(prefix, orderRefNumberIndb != null ? orderRefNumberIndb : null);

                bool checkExisted = false;
                do
                {
                    var settingInDb = _settingOrderRepository.SingleOrDefault(null, _schemaName);
                    if (settingInDb != null && settingInDb.OrderRefNumber == generatedNumber)
                    {
                        checkExisted = false;
                        generatedNumber = String.Format("{0}{1:00000}", prefix, generatedNumber != null ? generatedNumber.Substring(3).TryParseInt() + 1 : 0);
                    }
                    else
                    {
                        // check order ref number
                        SO_OrderInformations dataInDb = _orderInformationsRepository.Find(x => x.OrderRefNumber == generatedNumber, _schemaName).FirstOrDefault();
                        if (dataInDb == null)
                        {
                            checkExisted = true;
                            if (settingInDb != null)
                            {
                                settingInDb.OrderRefNumber = generatedNumber;
                                settingInDb.UpdatedDate = DateTime.Now;
                                settingInDb.UpdatedBy = DistributorCode;
                                settingInDb.OwnerCode = _distributorCode;
                                settingInDb.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                                _settingOrderRepository.Update(settingInDb, _schemaName);
                            }
                            else
                            {
                                SO_SalesOrderSetting insertSetting = new();
                                insertSetting.Id = Guid.NewGuid();
                                insertSetting.OrderRefNumber = generatedNumber;
                                insertSetting.LeadDate = 0; // Chỗ này sẽ giải quyết sau, set tạm
                                insertSetting.CreatedBy = DistributorCode;
                                insertSetting.CreatedDate = DateTime.Now;
                                insertSetting.OwnerCode = _distributorCode;
                                insertSetting.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                                _settingOrderRepository.Insert(insertSetting, _schemaName);
                            }
                        }
                        else
                        {
                            checkExisted = false;
                            generatedNumber = String.Format("{0}{1:00000}", prefix, generatedNumber != null ? generatedNumber.Substring(3).TryParseInt() + 1 : 0);
                        }
                    }
                } while (!checkExisted);

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = generatedNumber
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

        public BaseResultModel CreateDistributorOrder(DistributorOrderModel request, string DistributorCode)
        {
            try
            {
                BaseResultModel result = new BaseResultModel();

                if (request == null || request.OrderInformations == null || (request.OrderItems == null || request.OrderItems.Count == 0))
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = "Bad request";

                    return result;
                }

                if (string.IsNullOrWhiteSpace(request.OrderInformations.OrderRefNumber))
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = "OrderRefNumber is null";

                    return result;
                }

                if (string.IsNullOrWhiteSpace(request.OrderInformations.Status))
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = "Status is null";

                    return result;
                }

                //Step 1: Check status = SO_ST_DRAFT ||  SO_ST_OPEN
                //if (!request.OrderInformations.Status.Equals("SO_ST_DRAFT") && !request.OrderInformations.Status.Equals("SO_ST_OPEN"))
                //{
                //    result.IsSuccess = false;
                //    result.Code = 400;
                //    result.Message = "Status is not allow";

                //    return result;
                //}

                //step 2:
                SO_OrderInformations orderInfo = _orderInformationsRepository.Find(o => o.OrderRefNumber.Equals(request.OrderInformations.OrderRefNumber), _schemaName).FirstOrDefault();
                if (orderInfo != null)
                {
                    //Delete
                    _orderInformationsRepository.Delete(orderInfo.Id, _schemaName);
                    List<SO_OrderItems> orderItems = _orderItemsRepository.Find(i => i.OrderRefNumber == orderInfo.OrderRefNumber, _schemaName).ToList();
                    foreach (var item in orderItems)
                    {
                        _orderItemsRepository.Delete(item.Id, _schemaName);
                    }
                }

                //Insert
                request.OrderInformations.Id = new Guid();
                request.OrderInformations.CreatedDate = DateTime.Now;
                request.OrderInformations.CreatedBy = DistributorCode;
                request.OrderInformations.UpdatedDate = DateTime.Now;
                request.OrderInformations.UpdatedBy = DistributorCode;

                _orderInformationsRepository.Insert(request.OrderInformations, _schemaName);

                request.OrderItems.ForEach(order =>
                {
                    order.Id = new Guid();
                    order.CreatedDate = DateTime.Now;
                    order.CreatedBy = DistributorCode;
                    order.UpdatedDate = DateTime.Now;
                    order.UpdatedBy = DistributorCode;
                });
                _orderItemsRepository.InsertMany(request.OrderItems, _schemaName);


                return result;
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

        public BaseResultModel GetCustomerShiptoDetail(string CustomerCode, string DistributorCode)
        {
            try
            {
                BaseResultModel result = new BaseResultModel();
                CustomerShiptoModel shipto = null;

                string query = @$"SELECT * FROM  ""public"".""f_getlistshiptobycustomer""('{DistributorCode}', '{CustomerCode}') LIMIT 1;";
                var res = (List<CustomerShiptoModel>)_dapperRepositories.Query<CustomerShiptoModel>(query);
                if (res == null || res.Count == 0)
                {
                    result.IsSuccess = false;
                    result.Code = 404;
                    result.Message = "Customer Shipto not found.";
                    result.Data = null;
                }

                shipto = res[0];
                query = $@"SELECT * FROM  ""public"".""f_getdetailshipto""('{DistributorCode}', '{shipto.Id}');";
                res = (List<CustomerShiptoModel>)_dapperRepositories.Query<CustomerShiptoModel>(query);
                if (res != null && res.Count != 0)
                {
                    shipto = MergeObjects(shipto, res[0]);
                }

                query = $@"SELECT * FROM ""public"".""f_getemployeesbyshipto""('{DistributorCode}', '{CustomerCode}','{shipto.ShiptoCode}')";
                res = (List<CustomerShiptoModel>)_dapperRepositories.Query<CustomerShiptoModel>(query);
                if (res != null && res.Count != 0)
                {
                    shipto = MergeObjects(shipto, res[0]);
                }

                result.Data = shipto;
                result.IsSuccess = true;
                result.Message = "OK";
                return result;
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

        public BaseResultModel GetOrderSetting()
        {
            try
            {
                var query = $@"
                WITH ""GetTaxSettings"" AS (
                SELECT ""Key"" as ""SettingKey"", ""Value"" as ""SettingValue"" FROM ""public"".""PrincipalSettings"" WHERE ""Key"" = 'SalesPriceIncludeVat'),
                ""GetPeriod"" AS (
                        SELECT 'PeriodId' as ""SettingKey"", ""SalesPeriodId"" as ""SettingValue"" FROM ""public"".""VVSaleCalendarCurrentYear"" WHERE ""Date""::Date = now()::Date
                )
                SELECT * FROM ""GetTaxSettings""
                UNION
                SELECT * FROM ""GetPeriod""
                ";

                var res = (List<dynamic>)_dapperRepositories.Query<dynamic>(query);
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = res.ToList()
                };
            }
            catch (System.Exception ex)

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

        public BaseResultModel CancelDistributorOrder(DistributorCancelOrderModel request, string token)
        {
            try
            {
                BaseResultModel result = new BaseResultModel();

                if (request == null)
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = "Bad request";

                    return result;
                }

                if (string.IsNullOrWhiteSpace(request.OrderRefNumber))
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = "OrderRefNumber is null.";

                    return result;
                }


                SO_OrderInformations orderInfo = _orderInformationsRepository.Find(x => !string.IsNullOrWhiteSpace(x.OrderRefNumber) && x.OrderRefNumber == request.OrderRefNumber, _schemaName).FirstOrDefault();
                if (orderInfo == null)
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = $@"Order {request.OrderRefNumber}  is not found.";

                    return result;
                }

                if (orderInfo.Status.Equals("SO_ST_DELIVERED") || orderInfo.Status.Equals("SO_ST_SHIPPING") || orderInfo.Status.Equals("SO_ST_PARTIALDELIVERED"))
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = @$"Status {orderInfo.Status} of order {orderInfo.OrderRefNumber}  is not allow.";

                    return result;
                }

                orderInfo.Status = SO_SaleOrderStatusConst.CANCEL;
                orderInfo.CancelDate = DateTime.Now;
                orderInfo.UpdatedBy = request.DistributorCode;
                orderInfo.UpdatedDate = DateTime.Now;

                List<SO_OrderItems> orderItem = _orderItemsRepository.Find(o => o.OrderRefNumber == orderInfo.OrderRefNumber, _schemaName).ToList();
                if (orderItem == null || orderItem.Count == 0)
                {
                    _orderInformationsRepository.Update(orderInfo, _schemaName);

                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = @$"Order {orderInfo.OrderRefNumber} not have item.";

                    return result;
                }

                var returnStockResult = HandleCancelBookedInventory(orderItem, request, token);
                if (!returnStockResult.IsSuccess) return returnStockResult;

                var returnBudgetResult = HandleCancelBookedBudget(request.AppliedPromotionList, request, token);

                _orderInformationsRepository.Update(orderInfo, _schemaName);

                result.IsSuccess = true;
                result.Code = 200;
                result.Message = "Ok";
                result.Data = returnBudgetResult.Data != null ? returnBudgetResult.Data : null;

                return result;
            }
            catch (System.Exception ex)
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

        public BaseResultModel UpdateStatus(UpdateDistributorOrderModel request, string token)
        {
            try
            {
                BaseResultModel result = new BaseResultModel();

                if (request == null)
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = "Bad request";

                    return result;
                }

                if (string.IsNullOrWhiteSpace(request.OrderRefNumber))
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = "OrderRefNumber is null.";

                    return result;
                }

                if (string.IsNullOrWhiteSpace(request.Status))
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = "Status is null.";

                    return result;
                }

                SO_OrderInformations orderInfo = _orderInformationsRepository.Find(x => !string.IsNullOrWhiteSpace(x.OrderRefNumber) && x.OrderRefNumber == request.OrderRefNumber, _schemaName).FirstOrDefault();
                if (orderInfo == null)
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = $@"Order {request.OrderRefNumber}  is not found.";

                    return result;
                }

                var oldStatus = orderInfo.Status;
                orderInfo.Status = request.Status;
                orderInfo.DeliveredDate = DateTime.Now;
                double Shipped_TotalBeforeTax_Amt = 0.0, Shipped_TotalAfterTax_Amt = 0.0;
                decimal Shipped_Extend_Amt = 0;
                decimal Shipped_Amt = 0;
                List<SO_OrderItems> orderItems = _orderItemsRepository.Find(o => o.OrderRefNumber == request.OrderRefNumber, _schemaName).ToList();
                if (request.Status.Equals("SO_ST_DELIVERED"))
                {
                    if (orderItems != null && orderItems.Count > 0)
                    {
                        var salesPriceIncludeVaT = _caculateTaxService.GetSalesPriceIncludeVaT();
                        // tính các column shipped. lầy source Đăng nhỏ qua (chỉ đáp ứng cho màn hình mới order cho trường hợp hoàn tất đơn ko có update số giao ở màn hình mới)
                        foreach (var item in orderItems)
                        {
                            ShippedLineTax shippedLineTax = new ShippedLineTax()
                            {
                                disCountAmount = item.DisCountAmount,
                                shipped_Line_Amt = item.Ord_Line_Amt,
                                vatValue = item.VatValue,
                                shipped_line_Disc_Amt = item.Ord_line_Disc_Amt,
                                salespriceincludeVAT = salesPriceIncludeVaT
                            };
                            double Shipped_Line_TaxAfter_Amt, Shipped_Line_TaxBefore_Amt;
                            decimal Shipped_Line_Extend_Amt;
                            _caculateTaxService.CaculateShippingTax(shippedLineTax, out Shipped_Line_TaxAfter_Amt, out Shipped_Line_TaxBefore_Amt, out Shipped_Line_Extend_Amt);
                            item.Shipped_Line_TaxAfter_Amt = Shipped_Line_TaxAfter_Amt;
                            item.Shipped_Line_TaxBefore_Amt = Shipped_Line_TaxBefore_Amt;
                            item.Shipped_Line_Extend_Amt = Shipped_Line_Extend_Amt;
                            item.ShippedQuantities = item.OrderQuantities;
                            item.ShippedBaseQuantities = item.OrderBaseQuantities;
                            item.Shipped_Line_Amt = item.Ord_Line_Amt;
                            Shipped_TotalBeforeTax_Amt += Shipped_Line_TaxBefore_Amt;
                            Shipped_TotalAfterTax_Amt += Shipped_Line_TaxAfter_Amt;
                            Shipped_Extend_Amt += Shipped_Line_Extend_Amt;
                            Shipped_Amt += item.Shipped_Line_Amt;
                        }

                        _orderItemsRepository.UpdateMany(orderItems, _schemaName);
                    }


                }

                //tính sum các column shipped cho OrderInformation
                orderInfo.Shipped_TotalBeforeTax_Amt = Shipped_TotalBeforeTax_Amt;
                orderInfo.Shipped_TotalAfterTax_Amt = Shipped_TotalAfterTax_Amt;
                orderInfo.Shipped_Extend_Amt = Shipped_Extend_Amt;
                orderInfo.Shipped_Amt = Shipped_Amt;
                orderInfo.Shipped_Qty = orderInfo.Ord_Qty;
                orderInfo.Shipped_Promotion_Qty = orderInfo.Promotion_Qty;
                orderInfo.Shipped_Disc_Amt = orderInfo.Ord_Disc_Amt;
                orderInfo.Shipped_Qty = orderInfo.Ord_Qty;
                orderInfo.Shipped_line_Disc_Amt = orderInfo.Ordline_Disc_Amt;
                orderInfo.Shipped_Promotion_Amt = orderInfo.Promotion_Amt;
                _orderInformationsRepository.Update(orderInfo, _schemaName);

                if (oldStatus.Equals("SO_ST_OPEN") && request.Status.Equals("SO_ST_DELIVERED") || oldStatus.Equals("SO_ST_WAITINGSHIPPING") && request.Status.Equals("SO_ST_DELIVERED"))
                {
                    //call api BulkCreate inventory transaction
                    _caculateTaxService.InventoryBulkCreate(orderInfo, orderItems, oldStatus, request.Status, token);
                }

                result.IsSuccess = true;
                result.Code = 200;
                result.Message = "OK";
                return result;
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

        public BaseResultModel GetReturnOrder(string OrderRefNumber)
        {
            try
            {
                BaseResultModel result = new BaseResultModel();

                if (string.IsNullOrWhiteSpace(OrderRefNumber))
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = "OrderRefNumber is null.";

                    return result;
                }


                var orderInfo = _orderInformationsRepository.Find(o => o.OrderRefNumber == OrderRefNumber && o.isReturn == true, _schemaName).FirstOrDefault();
                if (orderInfo == null)
                {
                    result.Data = null;
                    result.IsSuccess = false;
                    result.Code = 404;
                    result.Message = "Order can not found.";

                    return result;
                }

                DistributorOrderModel disOrder = new DistributorOrderModel();
                disOrder.OrderInformations = orderInfo;
                disOrder.OrderItems = _orderItemsRepository.Find(x => x.OrderRefNumber == OrderRefNumber, _schemaName).ToList();

                result.Data = disOrder;
                result.IsSuccess = true;
                result.Code = 200;
                result.Message = "OK";

                return result;
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

        public BaseResultModel GetOrderDetail(string OrderRefNumber)
        {
            try
            {
                BaseResultModel result = new BaseResultModel();

                if (string.IsNullOrWhiteSpace(OrderRefNumber))
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = "OrderRefNumber is null.";

                    return result;
                }


                var orderInfo = _orderInformationsRepository.Find(o => o.OrderRefNumber == OrderRefNumber, _schemaName).FirstOrDefault();
                if (orderInfo == null)
                {
                    result.Data = null;
                    result.IsSuccess = false;
                    result.Code = 404;
                    result.Message = "Order can not found.";

                    return result;
                }

                DistributorOrderModel disOrder = new DistributorOrderModel();
                disOrder.OrderInformations = orderInfo;
                disOrder.OrderItems = _orderItemsRepository.Find(x => x.OrderRefNumber == OrderRefNumber, _schemaName).ToList();

                result.Data = disOrder;
                result.IsSuccess = true;
                result.Code = 200;
                result.Message = "OK";

                return result;
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

        public BaseResultModel HandleCancelBookedInventory(List<SO_OrderItems> orderItems, DistributorCancelOrderModel request, string token)
        {
            try
            {
                List<INV_TransactionModel> transactionData = new();

                foreach (var item in orderItems)
                {
                    transactionData.Add(new INV_TransactionModel
                    {
                        OrderCode = item.OrderRefNumber,
                        ItemId = item.ItemId,
                        ItemCode = item.ItemCode,
                        ItemDescription = item.ItemDescription,
                        Uom = item.UOM,
                        Quantity = item.OrderQuantities, // số lượng cần đặt
                        BaseQuantity = item.OrderBaseQuantities, //base cua thằng tr
                        OrderBaseQuantity = item.OrderBaseQuantities,
                        TransactionDate = DateTime.Now,
                        TransactionType = INV_TransactionType.SO_BOOKED_CANCEL,
                        WareHouseCode = request.DistributorShiptoCode,
                        LocationCode = item.LocationID ?? "1",
                        DistributorCode = request.DistributorCode,
                        DSACode = request.DsaCode,
                        ReasonCode = request.ReasonCode,
                        ReasonDescription = request.ReasonName
                    });
                }

                //call api transaction
                _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
                _client.Authenticator = new JwtAuthenticator($"{token}");
                var json = JsonConvert.SerializeObject(transactionData);
                var req = new RestRequest($"InventoryTransaction/BulkCreate", Method.POST);
                req.AddHeader(OD_Constant.KeyHeader, _distributorCode);
                req.AddJsonBody(json);

                var result = _client.Execute(req);

                var resultData = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(result.Content).ToString());
                if (!resultData.IsSuccess)
                {
                    resultData.Message = "Inventory transaction: " + resultData.Message;
                    resultData.Data = null;

                    return resultData;
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Ok"
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

        public BaseResultModel HandleCancelBookedBudget(List<AppliedPromotionModel> AppliedPromotionList, DistributorCancelOrderModel request, string token)
        {
            try
            {
                List<AppliedPromotionResultModel> ErrorList = new List<AppliedPromotionResultModel>();
                if (AppliedPromotionList != null && AppliedPromotionList.Count > 0)
                {
                    foreach (var item in AppliedPromotionList)
                    {
                        var budgetDataReq = new BudgetReqModel
                        {
                            budgetCode = item.BudgetCode,
                            customerCode = request.CustomerCode,
                            customerShipTo = request.ShiptoCode,
                            saleOrg = request.SaleOrgCode,
                            budgetBook = -(item.BudgetQuantity),
                            promotionCode = item.PromotionCode,
                            promotionLevel = item.LevelId,
                            routeZoneCode = request.RouteZoneCode,
                            dsaCode = request.DsaCode,
                            subAreaCode = request.SubArea,
                            areaCode = request.Area,
                            subRegionCode = request.SubRegion,
                            regionCode = request.Region,
                            branchCode = request.Branch,
                            nationwideCode = "VN",
                            salesOrgCode = request.SaleOrgCode,
                            referalCode = request.OrderRefNumber,
                            distributorCode = request.DistributorCode
                        };

                        //call api check budget
                        _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODTpAPI).Select(x => x.Url).FirstOrDefault());
                        _client.Authenticator = new JwtAuthenticator($"{token}");
                        var req = new RestRequest($"external_checkbudget/checkbudget", Method.POST);
                        req.AddHeader(OD_Constant.KeyHeader, _distributorCode);
                        var json = JsonConvert.SerializeObject(budgetDataReq);
                        req.AddJsonBody(json);
                        var result = _client.Execute(req);

                        var resultData = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(result.Content).ToString());
                        if (!resultData.IsSuccess)
                        {
                            AppliedPromotionResultModel errorItem = _mapper.Map<AppliedPromotionResultModel>(item);
                            errorItem.ErrorMessage = resultData.Message;
                            ErrorList.Add(errorItem);

                            continue;
                        }
                    }
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Ok",
                    Data = ErrorList.Count > 0 ? ErrorList : null
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

        public static T MergeObjects<T>(T obj1, T obj2) where T : class, new()
        {
            if (obj1 == null) return obj2;
            if (obj2 == null) return obj1;

            T mergedObject = new T();
            foreach (var prop in typeof(T).GetProperties())
            {
                var value1 = prop.GetValue(obj1);
                var value2 = prop.GetValue(obj2);
                prop.SetValue(mergedObject, value2 ?? value1); // Prefer obj2 values unless null
            }
            return mergedObject;
        }
    }
}