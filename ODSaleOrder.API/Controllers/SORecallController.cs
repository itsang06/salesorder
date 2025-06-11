using DynamicSchema.Helper.Models;
using DynamicSchema.Helper.Models.Header;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.SORecallModels;
using ODSaleOrder.API.Services;
using ODSaleOrder.API.Services.SORecallService;
using Sys.Common.JWT;
using Sys.Common.Utils;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]

    [ApiVersion("1.0")]
    public class SORecallController : ControllerBase
    {
        private readonly ISORecallReqService _recallReqService;
        private readonly ISORecallService _recallService;

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _token;

        static AsyncLocker<string> AsyncLocker = new AsyncLocker<string>();
        public SORecallController(
            IHttpContextAccessor contextAccessor, 
            ISORecallReqService recallReqService,
            ISORecallService recallService
        )
        {
            _contextAccessor = contextAccessor;
            _recallReqService = recallReqService;
            _recallService = recallService;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }

        [HttpGet]
        [Route("DetailOrderRequest/{code}")]
        [MapToApiVersion("1.0")]
        [Authorize]
        public async Task<IActionResult> DetailOrderRequest(string code)
        {
            return Ok(await _recallReqService.GetDetailReq(code));
        }

        [HttpDelete]
        [Route("DeleteOrderRequest/{code}")]
        [MapToApiVersion("1.0")]
        [Authorize]
        public async Task<IActionResult> DeleteOrderRequest(string code)
        {
            using (await AsyncLocker.LockAsync(User.GetName()))
            {
                return Ok(await _recallReqService.DeleteReq(code, User.GetName()));
            }
        }

        [HttpPost]
        [Route("CreateOrderRequest")]
        [MapToApiVersion("1.0")]
        [Authorize]
        public async Task<IActionResult> CreateOrderRequest(SORecallReqModel model)
        {
            using (await AsyncLocker.LockAsync(User.GetName()))
            {
                var result = await _recallReqService.InsertOrderRequest(model, User.GetName(), _token, false);
                return Ok(result);
            }
        }

        [HttpPut]
        [Route("ConfirmOrderRequest/{code}")]
        [MapToApiVersion("1.0")]
        [Authorize]
        public async Task<IActionResult> ConfirmOrderRequest(string code)
        {
            using (await AsyncLocker.LockAsync(User.GetName()))
            {
                var result = await _recallReqService.ConfirmOrderRequest(code, User.GetName(), _token);
                return Ok(result);
            }
        }

        [HttpPost]
        [Route("Sync/CreateOrderRequest")]
        [MapToApiVersion("1.0")]
        [Authorize]
        public async Task<IActionResult> SyncCreateOrderRequest(SORecallReqModel model)
        {
            using (await AsyncLocker.LockAsync(User.GetName()))
            {
                var result = await _recallReqService.InsertOrderRequest(model, User.GetName(), _token, true);
                return Ok(result);
            }
        }

        [HttpPut]
        [Route("UpdateOrderRequest")]
        [MapToApiVersion("1.0")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderRequest(SORecallReqModel model)
        {
            using (await AsyncLocker.LockAsync(model.Code))
            {
                return Ok(await _recallReqService.UpdateOrderRequest(model, User.GetName(), _token));
            }

        }
        
        [HttpPost]
        [Route("SearchOrderRequest")]
        [MapToApiVersion("1.0")]
        [Authorize]
        public async Task<IActionResult> SearchOrderRequest(SORecallReqSearch parameters)
        {
            return Ok(await _recallReqService.SearchReq(parameters));
        }

        [HttpPost]
        [Route("GetDetailRecallReqForRecall")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> GetDetailRecallReqForRecall(GetDetailRecallReqForRecallModel parameters)
        {
            return Ok(await _recallReqService.GetDetailRecallReqForRecall(parameters));
        }


        [HttpPost]
        [Route("CreateOrder")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> CreateOrder(SORecallModel model)
        {
            using (await AsyncLocker.LockAsync(User.GetName()))
            {
                var result = await _recallService.InsertOrderRecall(model, User.GetName(), _token);
                return Ok(result);
            }
        }

        [HttpGet]
        [Route("DetailOrder/{code}")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> DetailOrder(string code)
        {
            return Ok(await _recallService.GetDetail(code));
        }

        [HttpDelete]
        [Route("DeleteOrder/{code}")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> DeleteOrder(string code)
        {
            using (await AsyncLocker.LockAsync(User.GetName()))
            {
                return Ok(await _recallService.Delete(code, User.GetName()));
            }
        }

        [HttpPut]
        [Route("ConfirmOrder/{code}")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> ConfirmOrder(string code)
        {
            using (await AsyncLocker.LockAsync(User.GetName()))
            {
                return Ok(await _recallService.ConfirmOrder(code, User.GetName(), _token));
            }
        }

        [HttpPut]
        [Route("UpdateOrder")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> UpdateOrder(SORecallModel model)
        {
            using (await AsyncLocker.LockAsync(model.Code))
            {
                var result = await _recallService.UpdateOrderRecall(model, User.GetName(), _token);
                return Ok(result);
            }

        }

        [HttpPost]
        [Route("SearchOrder")]
        [MapToApiVersion("1.0")]
        [Authorize]
        [HeaderModel]
        public async Task<IActionResult> SearchOrder(SORecallSearch parameters)
        {
            return Ok(await _recallService.Search(parameters));
        }
    }
}
