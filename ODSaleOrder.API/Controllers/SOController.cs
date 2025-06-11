using AutoMapper;
using DynamicSchema.Helper.Models.Header;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.OS;
using ODSaleOrder.API.Services;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using Sys.Common.Extensions;
using Sys.Common.JWT;
using Sys.Common.Models;
using Sys.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]

    [ApiVersion("1.0")]
    public class SOController : ControllerBase
    {
        private readonly ISalesOrderService _service;
        private readonly IMapper mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;

        static AsyncLocker<string> AsyncLocker = new AsyncLocker<string>();
        public SOController(IHttpContextAccessor contextAccessor, ISalesOrderService service, IMapper mapper)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            this.mapper = mapper;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }

        #region SO
        [HttpPost]
        [Route("Create")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> InsertOrder(SaleOrderModel model)
        {
            using (await AsyncLocker.LockAsync(model.CustomerId + model.CustomerShiptoID))
            {
                var result = await _service.InsertOrder(model, _token, User.GetName(), false);
                return Ok(result);
            }
        }

        [HttpPost]
        [Route("SaveWithConfirm")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> SaveWithConfirm(SaleOrderModel model)
        {
            using (await AsyncLocker.LockAsync(model.CustomerPhone))
            {

                return Ok(await _service.SaveWithConfirm(model, _token, User.GetName()));
            }
        }

        [HttpPost]
        [Route("PrintDeliveryNote")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> PrintDeliveryNote(List<string> refNumbers)
        {
            return Ok(await _service.PrintDeliveryNote(refNumbers, _token, User.GetName()));
        }

        [HttpPut]
        [Route("Update")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> UpdateOrder(SaleOrderModel model)
        {
            using (await AsyncLocker.LockAsync(model.OrderRefNumber))
            {
                return Ok(await _service.UpdateSO(model, _token, User.GetName()));
            }

        }

        [HttpPut]
        [Route("Confirm")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> Confirm(SaleOrderModel model)
        {
            using (await AsyncLocker.LockAsync(model.OrderRefNumber))
            {
                return Ok(await _service.ConfirmSO(model, _token, User.GetName()));
            }
        }

        [HttpPost]
        [Route("Detail")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> GetDetailSO(SaleOrderDetailQueryModel query)
        {
            return Ok(await _service.GetDetailSO(query));
        }

        [HttpPost]
        [Route("CompleteSO")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> CompleteSO(SaleOrderDetailQueryModel query)
        {
            using (await AsyncLocker.LockAsync(query.OrderRefNumber))
            {
                return Ok(await _service.CompleteSO(query, _token, User.GetName()));
            }
        }

        [HttpPost]
        [Route("Search")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> SearchSO(SaleOrderSearchParamsModel parameters)
        {
            return Ok(await _service.SearchSO(parameters, _token, true));
        }
        
        [HttpPost]
        [Route("Search-v2")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> SearchSOv2(SaleOrderSearchParamsModel parameters)
        {
            return Ok(await _service.SearchSOv2(parameters, _token, true));
        }

        [HttpPost]
        [Route("CancelSO")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> CancelSO(SaleOrderModel model)
        {
            // return Ok(await _service.CancelNewSO(model, _token, User.GetName()));
            using (await AsyncLocker.LockAsync(model.OrderRefNumber))
            {
                return Ok(await _service.CancelNewSO(model, _token, User.GetName()));
            }
        }

        [HttpPost]
        [Route("GetSuggestionItem")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> GetSuggestionItems(SaleOrderSearchParamsModel parameters, bool getbuyed)
        {
            return Ok(await _service.GetSuggestionItems(parameters, _token, getbuyed));
        }

        [HttpPost]
        [Route("CancelDeliveredSO")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> CancelMutipleDeliveredSO(SO_CancelRequestModel model)
        {
            using (await AsyncLocker.LockAsync(model.distributorCode))
            {
                return Ok(await _service.CancelMutipleDeliveredSO(model.OrderList, model.distributorCode, User.GetName(), _token));
            }
        }

        [HttpDelete]
        [Route("Delete")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> DeleteSO(SaleOrderDetailQueryModel query)
        {
            using (await AsyncLocker.LockAsync(User.GetName()))
            {
                return Ok(await _service.DeleteSO(query, User.GetName()));
            }
        }

        [HttpPut]
        [Route("UpdateDeliveryResult")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> UpdateDeliveryResult(List<SaleOrderModel> models, string status)
        {
            var locker = models.Select(x => x.OrderRefNumber).OrderByDescending(x => x).ToString();
            using (await AsyncLocker.LockAsync(locker))
            {
                return Ok(await _service.UpdateDeliveryResult(models, status, User.GetName(), _token));
            }
        }

        [HttpPut]
        [Route("UpdateDeliveryResultv2")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> UpdateDeliveryResultv2(List<string> OrderRefNumberList, string status, string reasonCode)
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            var locker = OrderRefNumberList.OrderByDescending(x => x).ToString();
            using (await AsyncLocker.LockAsync(locker))
            {
                return Ok(await _service.UpdateDeliveryResultv2(OrderRefNumberList, status, reasonCode, distributorCode, User.GetName(), _token));
            }
        }
        #endregion

        #region FirstTimeCustomer
        [HttpPost]
        [Route("FirstTimeCustomer/Create")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> CreateFTC(SO_FirstTimeCustomer model)
        {
            return Ok(await _service.CreateFTC(model, User.GetName()));
        }

        [HttpPost]
        [Route("FirstTimeCustomer/Search")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> SearchFTC(FTCEcoparams parameters)
        {
            return Ok(await _service.SearchFTC(parameters));
        }

        #endregion

        #region Report
        [HttpPost]
        [Route("Report/AdsByItemGroupCode")]
        [MapToApiVersion("1.0")]
        [Authorize]
        public async Task<IActionResult> AdsReport(SaleOrderAdsParams parameters)
        {
            return Ok(await _service.AdsReport(parameters, _token));
        }

        [HttpPost]
        [Route("Report/SalesSynthesisReport")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> SalesSynthesisReport(SummaryRevenueReportQuery parameters)
        {
            return Ok(await _service.SalesSynthesisReport(parameters, User.GetName(), _token));
        }

        [HttpPost]
        [Route("Report/SalesDetailReport")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ReportDoanhSoChiTiet(SummaryRevenueReportQuery parameters)
        {
            return Ok(await _service.SalesDetailReport(parameters, User.GetName(), _token));
        }


        [HttpPost]
        [Route("Report/ReportTrackingOrder")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ReportTrackingOrder(ReportTrackingOrderQueryModel parameters)
        {
            return Ok(await _service.ReportTrackingOrder(parameters, User.GetName(), _token));
        }


        [HttpPost]
        [Route("Report/ReportShippingStatus")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ReportShippingStatus(ReportOrderShippingQueryModel parameters)
        {
            return Ok(await _service.ReportShippingStatus(parameters, User.GetName(), _token));
        }

        [HttpPost]
        [Route("Report/ProductivityReport")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ProductivityReport(ProductivityReportRequest parameters)
        {
            return Ok(await _service.ProductivityReport(parameters, _token));
        }

        [HttpPost]
        [Route("Report/ProductivityByDayReport")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ProductivityByDayReport(ProductivityReportRequest parameters)
        {
            return Ok(await _service.ProductivityByDayReport(parameters, _token));
        }

        [HttpPost]
        [Route("Report/ProductivityBySalesReport")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ProductivityBySalesReport(ProductivityBySalesReportRequest parameters)
        {
            return Ok(await _service.ProductivityBySalesReport(parameters, User.GetName(), _token));
        }

        [HttpPost]
        [Route("Report/ProductivityByProductReport")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ProductivityByProductReport(PBPReportRequest parameters)
        {
            return Ok(await _service.ProductivityByProductReport(parameters, User.GetName(), _token));
        }
        #endregion

        [HttpPost]
        [Route("Report/GetListWareHouseByCustomerCode")]
        [MapToApiVersion("1.0")]
        [Authorize]
        public async Task<IActionResult> GetListWareHouseByCustomerId(SaleOrderSearchParamsModel parameters)
        {
            return Ok(await _service.GetListWareHouseByCustomerId(parameters));
        }

        [HttpGet]
        [Route("GetSaleRepIdByDistributorCode/{DistributorCode}")]
        [MapToApiVersion("1.0")]
        [Authorize]
        public async Task<IActionResult> GetSaleRepIdByDistributorCode(string DistributorCode)
        {
            return Ok(await _service.GetSaleRepIdByDistributorCode(DistributorCode));
        }

        [HttpPost]
        [Route("TransactionPendingData")]
        [MapToApiVersion("1.0")]
        [Authorize]
        public async Task<IActionResult> ProcessPendingDataTrans(OrderPendingTransModel model)
        {
            using (await AsyncLocker.LockAsync(model.BaselineDate.Date.ToString()))
            {
                return Ok(await _service.ProcessPendingDataTrans(model, User.GetName(), _token));
            }
        }
        [HttpPost]
        [Route("ODTransactionPendingData")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ODProcessPendingDataTrans(ODOrderPendingTransModel model)
        {
            string lockKey = $"{model.DistributorCode}{model.BaselineDate.Date.ToString("dd/MM/yyyy")}";
            using (await AsyncLocker.LockAsync(lockKey))
            {
                OrderPendingTransModel mappedModel = mapper.Map<OrderPendingTransModel>(model);
                return Ok(await _service.ProcessPendingDataTrans(mappedModel, User.GetName(), _token));
            }
        }

        #region External API
        [HttpPost]
        [Route("ExUpdateOrderSetting/{DeliveryLeadDate}")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ExUpdateOrderSetting(int DeliveryLeadDate)
        {
            return Ok(await _service.ExUpdateOrderSetting(DeliveryLeadDate, User.GetName()));
        }

        [HttpPost]
        [Route("ExTpGetDataForReportDetail")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ExTpGetDataForReportDetail(ExPromotionReportEcoParameters parameters)
        {
            return Ok(await _service.ExTpGetDataForReportDetail(parameters));
        }

        [HttpPost]
        [Route("ExTpGetOrdersForPopupPromotionReport")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ExTpGetOrdersForPopupPromotionReport(ExPromotionReportEcoParameters parameters)
        {
            return Ok(await _service.ExTpGetOrdersForPopupPromotionReport(parameters));
        }

        [HttpPost]
        [Route("ExTpGetRouteZonesOrderForPromotionReport")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ExTpGetRouteZonesOrderForPromotionReport(ExPromotionReportEcoParameters parameters)
        {
            return Ok(await _service.ExTpGetRouteZonesOrderForPromotionReport(parameters));
        }

        [HttpPost]
        [Route("ExTpGetRouteZonesOrderForPopupPromotionReport")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ExTpGetRouteZonesOrderForPopupPromotionReport(ExPromotionReportEcoParameters parameters)
        {
            return Ok(await _service.ExTpGetRouteZonesOrderForPopupPromotionReport(parameters));
        }

        [HttpPost]
        [Route("ExTpGetCustomersOrderForPromotionReport")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ExTpGetCustomersOrderForPromotionReport(ExPromotionReportEcoParameters parameters)
        {
            return Ok(await _service.ExTpGetCustomersOrderForPromotionReport(parameters));
        }

        [HttpPost]
        [Route("ExTpGetCustomersOrderForPopupPromotionReport")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ExTpGetCustomersOrderForPopupPromotionReport(ExPromotionReportEcoParameters parameters)
        {
            return Ok(await _service.ExTpGetCustomersOrderForPopupPromotionReport(parameters));
        }

        #endregion

        //[HttpPost]
        //[Route("TestNoti")]
        //[MapToApiVersion("1.0")]
        //[Authorize]
        //[HeaderModel]
        //public async Task<IActionResult> TestNoti(SaleOrderTestNotiModel input)
        //{
        //    return Ok(await _service.TestSendNoti(input, _token));
        //}
    }
}
