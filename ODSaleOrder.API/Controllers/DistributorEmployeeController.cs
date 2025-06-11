using DynamicSchema.Helper.Models.Header;
using Microsoft.AspNetCore.Mvc;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models.Customer;
using ODSaleOrder.API.Models.Distributor;
using ODSaleOrder.API.Services.Distributor;
using ODSaleOrder.API.Services.SaleHistories;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using Sys.Common.JWT;
using Sys.Common.Models;
using System;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    public class DistributorEmployeeController : ControllerBase
    {
        private readonly IDistributorService _service;

        public DistributorEmployeeController(IDistributorService service)
        {
            _service = service;
        }

        [HttpGet]
        [Route("GetBasicInfo/{DistributorCode}")]
        public IActionResult GetBasicInfo(string DistributorCode)
        {
            return Ok(_service.GetBasicInfo(DistributorCode));
        }

        [HttpGet]
        [Route("GetSaleman")]
        [HeaderModel]
        public IActionResult GetSaleman()
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GetSalemanList(distributorCode));
        }

        [HttpGet]
        [Route("GetSaleman/{DistributorCode}")]
        public IActionResult GetSalemanByCode(string DistributorCode)
        {
            return Ok(_service.GetSalemanList(DistributorCode));
        }

        [HttpGet]
        [Route("GetShipper")]
        [HeaderModel]
        public IActionResult GetShipper()
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GetShipperList(distributorCode));
        }

        [HttpGet]
        [Route("GetRouteZones")]
        [HeaderModel]
        public IActionResult GetRouteZones()
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GetRoutZoneList(distributorCode));
        }

        [HttpGet]
        [Route("GetRouteZones/{DistributorCode}")]
        public IActionResult GetRouteZonesByCode(string DistributorCode)
        {
            return Ok(_service.GetRoutZoneList(DistributorCode));
        }

        [HttpGet]
        [Route("GetCustomers")]
        [HeaderModel]
        public IActionResult GetCustomers()
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GetCustomerList(distributorCode));
        }

        [HttpPost]
        [Route("GetCustomersWithPaging")]
        [HeaderModel]
        public IActionResult GetCustomersWithPaging(SearchCustomerModel model){
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GetCustomerListWithPaging(model, distributorCode));
        }

        [HttpGet]
        [Route("GetCustomerShiptosByCustomerCode")]
        [HeaderModel]
        public IActionResult GetCustomerShiptos(string customerCode)
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GetShiptoByCustomer(distributorCode, customerCode));
        }

        [HttpGet]
        [Route("GetDetailCusShiptoById")]
        [HeaderModel]
        public IActionResult GetDetailCusShiptoByShiptoId(Guid shiptoId)
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GetDetailShiptoByShiptoId(distributorCode, shiptoId));
        }

        [HttpPost]
        [Route("GetRouteZoneBasicByPayload")]
        [HeaderModel]
        public IActionResult GetRouteZoneBasicByPayload(DisRouteZoneBasicReqModel input)
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GetRouteZoneBasicByPayload(distributorCode, input));
        }

        [HttpGet]
        [Route("GetEmployeesByShipto")]
        [HeaderModel]
        public IActionResult GetEmployeesByShipto(string CustomerCode, string ShiptoCode)
        {
            var distributorCode = HttpContext.Request.Headers["DistributorCode"].ToString();
            return Ok(_service.GetEmployeesByShipto(distributorCode, CustomerCode, ShiptoCode));
        }
    }
}
