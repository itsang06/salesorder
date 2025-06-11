using Microsoft.AspNetCore.Mvc;
using ODSaleOrder.API.Models.Distributor;
using ODSaleOrder.API.Services.Distributor;
using ODSaleOrder.API.Services.Manager;
using Sys.Common.JWT;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    public class ManagerController : ControllerBase
    {
        private readonly IManagerInfoService _service;

        public ManagerController(IManagerInfoService service)
        {
            _service = service;
        }

        [HttpGet]
        [Route("GetSaleman/{EmployeeCode}")]
        public IActionResult GetSaleman(string EmployeeCode)
        {
            return Ok(_service.GetSalemanList(EmployeeCode));
        }


        [HttpPost]
        [Route("GetDistributors")]
        public IActionResult GetDistributors(DistributorListRequestModel request)
        {
            return Ok(_service.GetDistributors(request.ManagerCode, request.SaleOrgCode));
        }
    }
}
