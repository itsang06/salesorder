using DynamicSchema.Helper.Models.Header;
using Elastic.Apm.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models.Customer;
using ODSaleOrder.API.Models.Distributor;
using ODSaleOrder.API.Models.DistributorSalesOrder;
using ODSaleOrder.API.Services.Distributor;
using ODSaleOrder.API.Services.DistributorOrder;
using ODSaleOrder.API.Services.SaleHistories;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using Sys.Common.JWT;
using Sys.Common.Models;
using System;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    //[Authorize]
    [ApiVersion("1.0")]
    public class DistributorSalesOrderController : ControllerBase
    {
        private readonly IDistributorSalesOrderService _service;

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;
        public DistributorSalesOrderController(IDistributorSalesOrderService service, IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];

        }

        [HttpPost]
        [Route("GetCustomers")]
        [HeaderModel]
        public IActionResult GetCustomerByDistributorPaging(SearchCustomerModel model)
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GetCustomerByDistributorPaging(model, distributorCode));
        }

        [HttpGet]
        [Route("GetCustomerShiptoDetail/{CustomerCode}")]
        [HeaderModel]
        public IActionResult GetCustomerShiptoDetail(string CustomerCode)
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GetCustomerShiptoDetail(CustomerCode, distributorCode));
        }

        [HttpGet]
        [Route("GetReturnOrder/{OrderRefNumber}")]
        [HeaderModel]
        public IActionResult GetReturnOrder(string OrderRefNumber)
        {
            return Ok(_service.GetReturnOrder(OrderRefNumber));
        }

        [HttpGet]
        [Route("GetOrderDetail/{OrderRefNumber}")]
        [HeaderModel]
        public IActionResult GetOrderDetail(string OrderRefNumber)
        {
            return Ok(_service.GetOrderDetail(OrderRefNumber));
        }

        [HttpGet]
        [Route("GenerateOrderRefNumber")]
        [HeaderModel]
        public IActionResult GenerateOrderRefNumber()
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GenerateOrderRefNumber(distributorCode));
        }

        [HttpPost]
        [Route("Create")]
        [HeaderModel]
        public IActionResult CreateDistributorOrder(DistributorOrderModel request)
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.CreateDistributorOrder(request, distributorCode));
        }

        [HttpPost]
        [Route("Cancel")]
        [HeaderModel]
        public IActionResult CancelDistributorOrder(DistributorCancelOrderModel request)
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            request.DistributorCode = distributorCode;
            return Ok(_service.CancelDistributorOrder(request, _token));
        }

        [HttpGet]
        [Route("GetOrderSetting")]
        [HeaderModel]
        public IActionResult GetOrderSetting()
        {
            return Ok(_service.GetOrderSetting());
        }

        [HttpPost]
        [Route("Update")]
        [HeaderModel]
        public IActionResult UpdateStatus(UpdateDistributorOrderModel request)
        {
            return Ok(_service.UpdateStatus(request, _token));
        }
    }
}
