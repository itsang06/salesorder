using DynamicSchema.Helper.Models.Header;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RDOS.PurchaseOrderAPI.Services;
using Sys.Common.JWT;
using Sys.Common.Models;
using Sys.Common.Utils;
using System.Threading.Tasks;
using ODSaleOrder.API.Models.OS;
using ODSaleOrder.API.Services.BaseLine;
using ODSaleOrder.API.Services.OneShop.Interface;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    public class OneShopController : ControllerBase
    {
        private readonly IOSImportOrderService _service;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;
        static AsyncLocker<string> userLock = new AsyncLocker<string>();
        private readonly IBaseLineService _baselineService;

        public OneShopController(
            IHttpContextAccessor contextAccessor, 
            IOSImportOrderService service,
            IBaseLineService baselineService)
        {
            _contextAccessor = contextAccessor;
            _service = service;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
            _baselineService = baselineService;
        }

        [HttpGet]
        [Route("GetFromDate")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<BaseResultModel> GetFormDate()
        {
            return await _baselineService.HandleCalculateBaselineDate();
        }

        [HttpPost]
        [Route("GetList")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> Search(SearchOsOrderModel req)
        {
            return Ok(await _service.GetListOrder(req));
        }


        [HttpPost]
        [Route("Import")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> ImportListOrder(ImportListOSOrder req)
        {
            using (await userLock.LockAsync(User.GetName()))
            {
                return Ok(await _service.ImportListOrder(req, _token, User.GetName()));
            }
        }

        [HttpPost]
        [Route("Cancel")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> CancelListOrder(ImportListOSOrder req)
        {
            using (await userLock.LockAsync(User.GetName()))
            {
                return Ok(await _service.CancelListOrder(req, _token, User.GetName(), true));
            }
        }

        [HttpPost]
        [Route("Web/Cancel")]
        [MapToApiVersion("1.0")]
        [HeaderModel]
        public async Task<IActionResult> WebCancelListOrder(ImportListOSOrder req)
        {
            using (await userLock.LockAsync(User.GetName()))
            {
                return Ok(await _service.CancelListOrder(req, _token, User.GetName(), false));
            }
        }
    }
}
