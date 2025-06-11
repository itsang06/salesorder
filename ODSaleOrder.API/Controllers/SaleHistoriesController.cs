using DynamicSchema.Helper.Models.Header;
using Microsoft.AspNetCore.Mvc;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.SaleHistories;
using ODSaleOrder.API.Services.SaleHistories;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using Sys.Common.JWT;
using Sys.Common.Models;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/External_[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    public class SaleHistoriesController : ControllerBase
    {
        private readonly ISaleHistoriesService _service;
        private readonly ISalesOrderService _serviceHistories;

        public SaleHistoriesController(ISaleHistoriesService service, ISalesOrderService serviceHistories)
        {
            _service = service;
            _serviceHistories = serviceHistories;
        }

        [HttpPost]
        [Route("SalesHistories")]
        [HeaderModel]
        public IActionResult SalesHistories(SearchModelv2 _search)
        {
            return Ok(_service.SaleHistories(_search));
        }


        [HttpPost]
        [Route("SaleOrderDetail")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public IActionResult SaleOrderDetail(SaleOrderDetailRequestModel request)
        {
            return Ok(_serviceHistories.SaleOrderDetail(request.ExternalOrdNBR, request.OrderType));
        }

        [HttpPost]
        [Route("OrderResult")]
        [MapToApiVersion("1.0")]
        public IActionResult OrderResult(SaleResultRequest request)
        {
            return Ok(_service.OrderResult(request.EmployeeCode, request.VisitDate));
        }

        [HttpPost]
        [Route("SalesVolumnReport")]
        public IActionResult SalesVolumnReport(SaleVolumnReportRequest _search)
        {
            return Ok(_service.SalesVolumnReport(_search));
        }
    }
}
