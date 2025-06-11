using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using nProx.Helpers.Dapper;
using nProx.Helpers.Services.Paging;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models.PrincipalModel;
using ODSaleOrder.API.Models;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using RestSharp;
using RestSharp.Authenticators;
using SysAdmin.Models.StaticValue;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sys.Common.JWT;
using System;

namespace ODSaleOrder.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class EnhanceGetApplyPromotionForOrderListController : ControllerBase
    {
        private readonly IDapperRepositories _dapperRepositories;
        private readonly ISchemaNavigateService<ODDistributorSchema> _schemaNavigateService;
        private string _schemaName = "public";
        private readonly IPagingService _pagingService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IDynamicBaseRepository<InvoiceOrder> _service;
        private RestClient _client;
        private readonly string _token;
        public EnhanceGetApplyPromotionForOrderListController(RDOSContext dbContext, IDapperRepositories dapperRepositories, IPagingService pagingService, IHttpContextAccessor contextAccessor)
        {
            _schemaNavigateService = new SchemaNavigateService<ODDistributorSchema>(dbContext);
            _contextAccessor = contextAccessor;
            _dapperRepositories = dapperRepositories;
            _pagingService = pagingService;
            _service = new DynamicBaseRepository<InvoiceOrder>(dbContext); ;
            _token = _contextAccessor.HttpContext.Request.Headers["Authorization"];
        }

        private async void TrySetSchemaName(string DistributorCode)
        {
            string text = DistributorCode;
            if (text == "public")
            {
                _schemaName = "public";
                return;
            }

            DynamicSchema.Helper.Models.ResultModelWithObject<ODDistributorSchema> resultModelWithObject = await _schemaNavigateService.NavigateSchemaByDistributorCode(text);
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
        public async Task<IActionResult> Post(RequestEnhanceApplyPromotionByOrderList inp)
        {
            Sys.Common.Models.Result<ApplyPromotionResponse> result = new Sys.Common.Models.Result<ApplyPromotionResponse>();
            try
            {
                var cloneProduct = inp.ProductList;
                ApplyPromotionResponse apply = new ApplyPromotionResponse();
                apply.AppliedPromotions = new List<ApplyPromotionByOrderList>();
                apply.NotApplyPromotions = new List<ProductItem>();
                TrySetSchemaName(inp.DistributorCode);
                List<string> PromotionCodes = new List<string>();
                List<string> lstProduct = new List<string>();
                lstProduct = inp.ProductList.Select(f => f.ItemCode)?.ToList();
                PromotionCodes = inp.PromotionCodes;             


                #region Step 3 So Suat
                for (int i = 0; i < lstProduct.Count; i++)
                {
                    lstProduct[i] = $"'{lstProduct[i]}'";
                }

                for (int i = 0; i < PromotionCodes.Count; i++)
                {
                    PromotionCodes[i] = $"'{PromotionCodes[i]}'";
                }

                string promo = string.Join(",", PromotionCodes);
                string products = string.Join(",", lstProduct);
                foreach (var prod in lstProduct)
                {

                    List<ProductItem> NotApplyPromotions = new List<ProductItem>();
                    List<ApplyPromotionByOrderList> AppliedPromotions = new List<ApplyPromotionByOrderList>();
                    string query = @$"SELECT * FROM ""public"".""f_get_promotion_attribute_summary""('{inp.DistributorCode}', ARRAY[{promo}], ARRAY[{prod}]);";
                    var trans = (List<ApplyPromotionByOrderList>)_dapperRepositories.Query<ApplyPromotionByOrderList>(query);

                    if (trans is null || !trans.Any())
                    {
                        apply.NotApplyPromotions.Add(inp.ProductList.FirstOrDefault(f => f.ItemCode == prod.Replace("'", "")));
                        continue;
                    }

                    var findInp = inp.ProductList.FirstOrDefault(f => f.ItemCode == prod.Replace("'", ""));
                    var element = trans.FirstOrDefault(f => f.ProductCode == prod.Replace("'", ""));
                    if (findInp is null || string.IsNullOrEmpty(findInp.ItemCode))
                        continue;

                    if (element.OrderBy == "Quantity")
                    {
                        var res = CalculatorQuantity(trans, findInp);
                        if (res != null)
                        {
                            NotApplyPromotions.AddRange(res.Item1);
                            AppliedPromotions.AddRange(res.Item2);
                        }

                    }
                    else
                    {
                        var res = CalculatorValue(trans, findInp);
                        if (res != null)
                        {
                            NotApplyPromotions.AddRange(res.Item1);
                            AppliedPromotions.AddRange(res.Item2);
                        }

                    }
                    apply.AppliedPromotions.AddRange(AppliedPromotions);
                    apply.NotApplyPromotions.AddRange(NotApplyPromotions);
                }



                #endregion

                #region CheckBudget
                apply = ReCheckBudget(apply, inp);
                #endregion

                #region CTKM
                if (apply.AppliedPromotions is not null && apply.AppliedPromotions.Any())
                {
                    #region query lấy danh sách KM
                    var lstPromo = apply.AppliedPromotions.Select(f => new { f.PromotionId, f.LevelId });
                    string strPromo = "";
                    string strArr = "";
                    bool IsFirst = true;
                    foreach (var item in lstPromo)
                    {
                        if (IsFirst)
                        {
                            strPromo += $"'{item.PromotionId}'";
                            strArr += @$"'{{{item.PromotionId},{item.LevelId}}}'";
                        }
                        else
                        {
                            strPromo += $", '{item.PromotionId}'";
                            strArr += @$", '{{{item.PromotionId},{item.LevelId}}}'";
                        }
                        IsFirst = false;
                    }
                    string giftQuery = @$"SELECT * FROM ""public"".""f_getgiftsfromapplypromotion""('{inp.DistributorCode}', ARRAY[{strPromo}], ARRAY[{strArr}]);";
                    var gifts = (List<GiftPromotion>)_dapperRepositories.Query<GiftPromotion>(giftQuery);
                    var diff = gifts.Select(f => new { f.LevelId, f.PromotionId })?.Distinct()?.ToList();
                    #endregion

                    foreach (var element in diff)
                    {
                        var items = gifts.Where(f => f.PromotionId == element.PromotionId && f.LevelId == element.LevelId)?.ToList();
                        var applyPromo = apply.AppliedPromotions.FirstOrDefault(f => f.PromotionId == element.PromotionId && f.LevelId == element.LevelId);
                        var productInfo = cloneProduct.FirstOrDefault(f => f.ItemCode == applyPromo.ProductCode);

                        if (items is null || !items.Any())
                            continue;

                        var type = items.Select(f => f.ProductType)?.Distinct().ToList();

                        foreach (var item in type)
                        {
                            var choose = items.Where(f => f.ProductType == item)?.ToList();

                            if (choose[0].FreeItemType == "True") // tang  hang
                            {
                                // Tặng 1 loại hoặc nhiều loại sản phẩm  
                                if (choose[0].RuleOfGiving) // tang nhieu
                                {
                                    applyPromo = CalculatorTotalFreeQty(choose, applyPromo);
                                }
                                else // tang 1
                                {
                                    if (choose[0].FreeSameProduct ?? false)
                                    {
                                        applyPromo = CalculatorTotalFreeQty(choose, applyPromo);
                                    }
                                    else
                                    {
                                        if (choose.Exists(f => f.IsDefaultProduct == true))
                                        {
                                            applyPromo = CalculatorTotalFreeQty(choose, applyPromo);
                                        }
                                    }

                                }
                            }
                            else // tang tien
                            {
                                if (choose[0].FreeAmountType is not null && string.IsNullOrEmpty(choose[0].FreeItemType) && !string.IsNullOrEmpty(choose[0].DiscountType))
                                {
                                    if (choose[0].DiscountType == "Discount")
                                    {


                                        applyPromo = CalculatorDisCountAmount(choose, applyPromo, productInfo);

                                    }
                                    else if (choose[0].DiscountType == "Donate")
                                    {

                                        applyPromo = CalculatorDonateAmount(choose, applyPromo, productInfo);

                                    }
                                }
                            }
                        }
                        // Check tye is Quantity / Amount

                    }
                }
                #endregion

                result.Data = apply;
                result.Success = true;

            }
            catch (Exception ex)
            {

            }
            return Ok(result);
        }

        [HttpPost]
        [Route("GetApplyPromotionByGroup")]
        public async Task<IActionResult> GetApplyPromotionByGroup(RequestEnhanceApplyPromotionByOrderList inp)
        {
            Sys.Common.Models.Result<List<PromotionGroupResponse>> result = new Sys.Common.Models.Result<List<PromotionGroupResponse>>();
            try
            {
                List<PromotionGroupResponse> res = new();
                var cloneProduct = inp.ProductList;
                List<string> PromotionCodes = new List<string>();
                List<string> lstProduct = new List<string>();
                lstProduct = inp.ProductList.Select(f => f.ItemCode)?.ToList();
                PromotionCodes = inp.PromotionCodes;
                lstProduct = inp.ProductList.Select(f => f.ItemCode)?.ToList();               


                #region Step 3 So Suat
                for (int i = 0; i < lstProduct.Count; i++)
                {
                    lstProduct[i] = $"'{lstProduct[i]}'";
                }

                for (int i = 0; i < PromotionCodes.Count; i++)
                {
                    PromotionCodes[i] = $"'{PromotionCodes[i]}'";
                }

                string promo = string.Join(",", PromotionCodes);
                string products = string.Join(",", lstProduct);

                List<ProductItem> NotApplyPromotions = new List<ProductItem>();
                List<ApplyPromotionByOrderList> AppliedPromotions = new List<ApplyPromotionByOrderList>();
                string query = @$"SELECT * FROM ""public"".""f_get_promotion_attribute_summary_v5""('{inp.DistributorCode}', ARRAY[{promo}]);";
                var trans = (List<PromotionAttributeSummary>)_dapperRepositories.Query<PromotionAttributeSummary>(query);

                foreach (var currentPromo in trans)
                {
                    if (currentPromo.SkuFullCase)
                    {
                        var prods = inp.ProductList.Where(f => f.Uom == currentPromo.PackingUomId)?.ToList();
                        var notProds = inp.ProductList.Where(f => f.Uom != currentPromo.PackingUomId)?.ToList();
                        if (notProds is not null && notProds.Any())
                        {
                            int cv = currentPromo.ConversionFactor ?? 0;
                            string currentUOM = currentPromo.PackingUomId;
                            var rmList = new List<ProductItem>();
                            foreach (var element in notProds)
                            {
                                int so_chan = (int)(element.BaseQuantity / cv);
                                int so_le = (int)(element.BaseQuantity % cv);
                                if (so_chan == 0)
                                    continue;
                                var elementProd = DeepCopy(element);
                                elementProd.Quantity = so_chan;
                                elementProd.Uom = currentUOM;
                                elementProd.BaseQuantity = so_chan * cv;
                                prods.Add(elementProd);

                                if (so_le == 0)
                                {
                                    rmList.Add(element);
                                }
                                else if (so_le > 0)
                                {
                                    element.BaseQuantity = so_le;
                                    element.Quantity = null;
                                }
                            }
                            if (rmList.Any())
                                foreach (var rm in rmList)
                                    notProds.Remove(rm);
                        }
                        var castToList = new List<PromotionAttributeSummary>() { currentPromo };
                        var responseCalculator = CalculatorGroupValue(prods, castToList, inp);
                        foreach (var element in responseCalculator)
                        {
                            if (element.NotApplyPromotions is null)
                                element.NotApplyPromotions = new List<NotApplyPromotion>();

                            element.NotApplyPromotions.AddRange(ToNotApplyPromotions(notProds));
                        }
                        res.AddRange(responseCalculator);

                    }
                    else
                    {
                        var prods = inp.ProductList.Where(f => f.ConversionFactor == currentPromo.ConversionFactor)?.ToList();
                        var notProds = inp.ProductList.Where(f => f.ConversionFactor != currentPromo.ConversionFactor)?.ToList();
                        if (notProds is not null && notProds.Any())
                        {

                            int cv = currentPromo.ConversionFactor ?? 0;
                            string currentUOM = currentPromo.PackingUomId;
                            var rmList = new List<ProductItem>();
                            if (currentPromo.ProductCodes.Contains(","))
                            {
                                string[] codes = currentPromo.ProductCodes.Split(",");
                                var package = inp.ProductList.Where(f => codes.Contains(f.ItemCode))?.ToList();
                                if (package is not null && package.Any())
                                {
                                    int totalBase = package.Sum(f => f.BaseQuantity) ?? 0;
                                    if (currentPromo.TotalRequiredBaseUnit <= totalBase)
                                    {
                                        var castToListNonFull = new List<PromotionAttributeSummary>() { currentPromo };
                                        var responseNonCalculator = CalculatorGroupValueForNonFullSku(inp.ProductList, package, castToListNonFull, inp);
                                        res.AddRange(responseNonCalculator);
                                    }
                                }

                            }
                            else
                            {

                                if (notProds.Exists(f => f.BaseQuantity >= cv) || prods.Any())
                                {
                                    foreach (var element in notProds.Where(f => f.BaseQuantity >= cv))
                                    {
                                        int so_chan = (int)(element.BaseQuantity / cv);
                                        int so_le = (int)(element.BaseQuantity % cv);
                                        if (so_chan == 0)
                                            continue;
                                        var elementProd = DeepCopy(element);
                                        elementProd.Quantity = so_chan;
                                        elementProd.Uom = currentUOM;
                                        elementProd.BaseQuantity = so_chan * cv;
                                        prods.Add(elementProd);

                                        if (so_le == 0)
                                        {
                                            rmList.Add(element);
                                        }
                                        else if (so_le > 0)
                                        {
                                            element.BaseQuantity = so_le;
                                            element.Quantity = null;
                                        }
                                    }
                                    if (notProds.Exists(f => f.BaseQuantity < cv))
                                    {
                                        int sum = notProds.Where(f => f.BaseQuantity < cv).Sum(f => f.BaseQuantity) ?? 0;
                                        if (sum >= cv)
                                            prods.AddRange(notProds.Where(f => f.BaseQuantity < cv));
                                    }


                                    var castToList = new List<PromotionAttributeSummary>() { currentPromo };
                                    var responseCalculator = CalculatorGroupValue(prods, castToList, inp, false);
                                    foreach (var element in responseCalculator)
                                    {
                                        if (element.NotApplyPromotions is null)
                                            element.NotApplyPromotions = new List<NotApplyPromotion>();

                                        if (rmList.Any())
                                        {
                                            foreach (var rm in rmList)
                                                notProds.Remove(rm);
                                            element.NotApplyPromotions = ToNotApplyPromotions(notProds);
                                        }
                                    }
                                    res.AddRange(responseCalculator);

                                }
                                else
                                {
                                    var elements = notProds.Where(f => f.BaseQuantity < cv)?.ToList();
                                    int total = notProds.Where(f => f.BaseQuantity < cv).Sum(k => k.BaseQuantity) ?? 0;
                                    var (picked, leftover) = PickBox(elements, cv);
                                    var castToListNonFull = new List<PromotionAttributeSummary>() { currentPromo };
                                    var ress = CalculatorGroupValueForNonFullSku(inp.ProductList, picked, castToListNonFull, inp);
                                    foreach (var element in ress)
                                    {
                                        if (element.NotApplyPromotions is null)
                                            element.NotApplyPromotions = new List<NotApplyPromotion>();
                                        if (leftover is not null && leftover.Any())
                                            element.NotApplyPromotions.AddRange(ToNotApplyPromotions(leftover));
                                    }
                                    res.AddRange(ress);

                                }

                            }
                        }
                        else
                        {
                            var castToListNonFull = new List<PromotionAttributeSummary>() { currentPromo };
                            var responseNonCalculator = CalculatorGroupValue(prods, castToListNonFull, inp, false);
                            res.AddRange(responseNonCalculator);
                        }



                    }
                }

                foreach (var element in res)
                {
                    if (element.NotApplyPromotions is null || !element.NotApplyPromotions.Any())
                        continue;
                    var duplicate = element.NotApplyPromotions.GroupBy(f => f.ItemCode).Select(g => new
                    {
                        PromotionId = g.Key,
                        count = g.Count()
                    }).Where(c => c.count > 1)?.ToList();
                    if (duplicate is not null && duplicate.Any())
                    {
                        foreach (var dup in duplicate)
                        {
                            var dups = DeepCopy(element.NotApplyPromotions);
                            var grouped = dups
                                        .Where(x => !string.IsNullOrEmpty(x.ItemCode)) // nếu cần
                                        .GroupBy(x => x.ItemCode)
                                        .Select(g => new NotApplyPromotion
                                        {
                                            ItemCode = g.Key,
                                            BaseQuantity = g.Sum(x => x.BaseQuantity ?? 0),
                                            BaseUom = g.First().BaseUom,
                                            ConversionFactor = g.First().ConversionFactor,
                                            ItemGroupCode = g.First().ItemGroupCode,
                                            Price = g.First().Price,
                                            Quantity = g.First().Quantity,
                                            TotalAmount = g.First().TotalAmount,
                                            Uom = g.First().Uom
                                        })
                                        .ToList();
                            if (grouped is not null && grouped.Any())
                            {
                                element.NotApplyPromotions = element.NotApplyPromotions.Where(f => !grouped.Exists(k => k.ItemCode == f.ItemCode))?.ToList();
                                element.NotApplyPromotions.AddRange(grouped);
                            }
                        }
                    }
                }


                #endregion

                //#region CheckBudget
                res = ReCheckBudgetGroup(res, inp);
                res = res.Where(f => f.AppliedPromotions.Count > 0)?.ToList();
                //#endregion

                #region CTKM
                res = res.GroupBy(x => new
                {
                    x.PromotionId,
                    LevelId = x.AppliedPromotions?.FirstOrDefault()?.LevelId ?? string.Empty
                })
                        .Select(g => g.First())
                        .ToList();


                if (res.Exists(f => f.AppliedPromotions.Any()))
                {
                    #region query lấy danh sách KM               
                    string strPromo = "";
                    string strArr = "";
                    bool IsFirst = true;
                    foreach (var item in res)
                    {
                        List<string> levelIds = item.AppliedPromotions.Select(f => f.LevelId)?.Distinct()?.ToList();

                        foreach (var level in levelIds)
                        {
                            if (IsFirst)
                            {
                                strPromo += $"'{item.PromotionId}'";
                                strArr += @$"'{{{item.PromotionId},{level}}}'";
                            }
                            else
                            {
                                strPromo += $", '{item.PromotionId}'";
                                strArr += @$", '{{{item.PromotionId},{level}}}'";
                            }
                            IsFirst = false;
                        }

                    }
                    string giftQuery = @$"SELECT * FROM ""public"".""f_getgiftsfromapplypromotionbycode""('{inp.DistributorCode}', ARRAY[{strPromo}], ARRAY[{strArr}]);";
                    var gifts = (List<GiftPromotion>)_dapperRepositories.Query<GiftPromotion>(giftQuery);
                    var diff = gifts.Select(f => new { f.LevelId, f.PromotionId })?.Distinct()?.ToList();
                    #endregion

                    foreach (var element in diff)
                    {
                        var ctkm = res.FirstOrDefault(f => f.PromotionId == element.PromotionId && f.AppliedPromotions.Exists(k => k.LevelId == element.LevelId));
                        var items = gifts.Where(f => f.PromotionId == element.PromotionId && f.LevelId == element.LevelId)?.ToList();
                        var applyPromo = ctkm.AppliedPromotions.FirstOrDefault(f => f.LevelId == element.LevelId);
                        var productInfo = cloneProduct.FirstOrDefault(f => applyPromo.SalesProducts.Exists(l => l.ProductCode == f.ItemCode));

                        if (items is null || !items.Any())
                            continue;

                        var type = items.Select(f => f.ProductType)?.Distinct().ToList();

                        foreach (var item in type)
                        {
                            var choose = items.Where(f => f.ProductType == item)?.ToList();

                            if (choose[0].FreeItemType == "True") // tang  hang
                            {
                                //Tặng 1 loại hoặc nhiều loại sản phẩm
                                if (choose[0].RuleOfGiving) // tang nhieu
                                {
                                    applyPromo = CalculatorTotalFreeQtyByGroup(choose, applyPromo);
                                }
                                else // tang 1
                                {
                                    if (choose[0].FreeSameProduct ?? false)
                                    {
                                        applyPromo = CalculatorTotalFreeQtyByGroup(choose, applyPromo);
                                    }
                                    else
                                    {
                                        if (choose.Exists(f => f.IsDefaultProduct == true))
                                        {
                                            applyPromo = CalculatorTotalFreeQtyByGroup(choose, applyPromo);
                                        }
                                    }

                                }
                            }
                        }
                        // Check tye is Quantity / Amount

                    }
                }
                #endregion

                result.Data = res;
                result.Success = true;

            }
            catch (Exception ex)
            {

            }
            return Ok(result);
        }

        [HttpPost]
        [Route("GetAllPromotion")]
        public async Task<IActionResult> Get(RequestEnhanceApplyPromotionByOrderList inp)
        {
            Sys.Common.Models.Result<List<AllPromotion>> result = new Sys.Common.Models.Result<List<AllPromotion>>();
            try
            {
                List<string> PromotionCodes = new List<string>();
                List<string> lstProduct = new List<string>();
                lstProduct = inp.ProductList.Select(f => f.ItemCode)?.ToList();
                PromotionCodes = inp.PromotionCodes;

                #region Step 2: Prepare data
                for (int i = 0; i < lstProduct.Count; i++)
                {
                    lstProduct[i] = $"'{lstProduct[i]}'";
                }

                for (int i = 0; i < PromotionCodes.Count; i++)
                {
                    PromotionCodes[i] = $"'{PromotionCodes[i]}'";
                }

                string promo = string.Join(",", PromotionCodes);
                string products = string.Join(",", lstProduct);
                string query = @$"SELECT * FROM ""public"".""f_getallpromotionbycode""('{inp.DistributorCode}', ARRAY[{promo}], ARRAY[{products}]);";
                var trans = (List<PromotionLevel>)_dapperRepositories.Query<PromotionLevel>(query);
                #endregion

                #region Step3: Lay list IsFreeProdut
                var lstPromo = trans.Select(f => new { f.PromotionId, f.LevelId });
                string strPromo = "";
                string strArr = "";
                bool IsFirst = true;
                foreach (var item in lstPromo)
                {
                    if (IsFirst)
                    {
                        strPromo += $"'{item.PromotionId}'";
                        strArr += @$"'{{{item.PromotionId},{item.LevelId}}}'";
                    }
                    else
                    {
                        strPromo += $", '{item.PromotionId}'";
                        strArr += @$", '{{{item.PromotionId},{item.LevelId}}}'";
                    }
                    IsFirst = false;
                }
                query = @$"SELECT * FROM ""public"".""f_getgiftsfromapplypromotionbycode""('{inp.DistributorCode}', ARRAY[{strPromo}], ARRAY[{strArr}]);"; ;
                var gifts = (List<GiftPromotion>)_dapperRepositories.Query<GiftPromotion>(query);
                #endregion

                var promotions = trans
                                        .GroupBy(x => new { x.PromotionId, x.PromotionName, x.PromotionType, x.OrderRule, x.OrderBy, }) // Group theo Promotion
                                        .Select(promo => new AllPromotion
                                        {
                                            PromotionId = promo.Key.PromotionId,
                                            PromotionName = promo.Key.PromotionName,
                                            PromotionType = promo.Key.PromotionType,
                                            OrderRule = promo.Key.OrderRule,
                                            OrderBy = promo.Key.OrderBy,
                                            Levels = promo
                                                .GroupBy(x => new { x.LevelId, x.LevelDesc, x.LevelOrderQty, x.LevelOrderAmount, x.LevelFreeQty, x.RuleOfGiving, x.IsApplyBudget, x.BudgetQtyCode, x.BudgetValueCode }) // Group theo Level
                                                .Select(level => new AllPromotionLevel
                                                {
                                                    LevelId = level.Key.LevelId,
                                                    LevelDesc = level.Key.LevelDesc,
                                                    LevelOrderQty = level.Key.LevelOrderQty,
                                                    LevelOrderAmount = level.Key.LevelOrderAmount,
                                                    LevelFreeQty = level.Key.LevelFreeQty,
                                                    RuleOfGiving = level.Key.RuleOfGiving,
                                                    IsApplyBudget = level.Key.IsApplyBudget,
                                                    BudgetQtyCode = level.Key.BudgetQtyCode,
                                                    BudgetValueCode = level.Key.BudgetValueCode,
                                                    SalesProducts = level
                                                        .Where(x => x.IsSalesProduct) // Lọc sản phẩm bán
                                                        .Select(x => new Product
                                                        {
                                                            ProductCode = x.ProductCode,
                                                            ProductName = x.ProductName,
                                                            ProductType = x.ProductType,
                                                            PackingUomId = x.PackingUomId,
                                                            PackingUomName = x.PackingUomName,
                                                            BaseUnit = x.BaseUnit,
                                                            AllowExchange = x.AllowExchange ?? false,
                                                            AttCode = x.AttCode,
                                                            AttCodeName = x.AttCodeName,
                                                            AttId = x.AttId,
                                                            AttName = x.AttName,
                                                            ConversionFactor = x.ConversionFactor,
                                                            ExchangeRate = x.ExchangeRate,
                                                            IsDefaultProduct = x.IsDefaultProduct,
                                                            IsFreeProduct = x.IsFreeProduct,
                                                            IsGiftProduct = x.IsGiftProduct,
                                                            IsSalesProduct = x.IsSalesProduct,
                                                            MinValue = x.MinValue,
                                                            NumberOfFreeItem = x.NumberOfFreeItem,
                                                            OrderProductQty = x.OrderProductQty,
                                                            RequiredMinQty = x.RequiredMinQty
                                                        }).ToList(),
                                                    FreeProducts = gifts
                                                        .Where(x => x.PromotionId == promo.Key.PromotionId && x.LevelId == level.Key.LevelId) // Lọc sản phẩm tặng
                                                        .Select(x => new Product
                                                        {
                                                            ProductCode = x.ProductCode,
                                                            ProductName = x.ProductName,
                                                            ProductType = x.ProductType,
                                                            PackingUomId = x.PackingUomId,
                                                            PackingUomName = x.PackingUomName,
                                                            BaseUnit = x.BaseUnit,
                                                            AllowExchange = x.AllowExchange,
                                                            AttCode = x.AttCode,
                                                            AttCodeName = x.AttCodeName,
                                                            AttId = x.AttId,
                                                            AttName = x.AttName,
                                                            ConversionFactor = x.ConversionFactor ?? 0,
                                                            ExchangeRate = x.ExchangeRate,
                                                            IsDefaultProduct = x.IsDefaultProduct,
                                                            IsFreeProduct = x.IsFreeProduct ?? false,
                                                            IsGiftProduct = x.IsGiftProduct,
                                                            IsSalesProduct = x.IsSalesProduct ?? false,
                                                            MinValue = x.MinValue,
                                                            NumberOfFreeItem = x.NumberOfFreeItem,
                                                            OrderProductQty = x.OrderProductQty,
                                                            RequiredMinQty = x.RequiredMinQty,
                                                            DiscountType = x.DiscountType,
                                                            FreeAmountType = x.FreeAmountType,
                                                            FreeAmount = x.FreeAmount,
                                                            FreePercentAmount = x.FreePercentAmount
                                                        }).ToList()
                                                }).ToList()
                                        }).ToList();
                result.Success = true;
                result.Data = promotions;
            }
            catch (Exception ex)
            {

                throw;
            }
            return Ok(result);
        }

        [HttpPost]
        [Route("GetApplyOrderPromotion")]

        public async Task<IActionResult> GetApplyOrderPromotion(RequestEnhanceApplyPromotionByOrderList inp)
        {
            Sys.Common.Models.Result<ApplyPromotionResponse> result = new Sys.Common.Models.Result<ApplyPromotionResponse>();
            try
            {
                ApplyPromotionResponse apply = new ApplyPromotionResponse();
                apply.AppliedPromotions = new List<ApplyPromotionByOrderList>();
                apply.NotApplyPromotions = new List<ProductItem>();
                TrySetSchemaName(inp.DistributorCode);
                List<string> PromotionCodes = new List<string>();
                List<string> lstProduct = new List<string>();
                PromotionCodes = inp.PromotionCodes;
                lstProduct = inp.ProductList.Select(f => f.ItemCode)?.ToList();


                #region Step 3 So Suat
                for (int i = 0; i < lstProduct.Count; i++)
                {
                    lstProduct[i] = $"'{lstProduct[i]}'";
                }

                for (int i = 0; i < PromotionCodes.Count; i++)
                {
                    PromotionCodes[i] = $"'{PromotionCodes[i]}'";
                }

                string promo = string.Join(",", PromotionCodes);
                string products = string.Join(",", lstProduct);

                #region  Query
                List<ProductItem> NotApplyPromotions = new List<ProductItem>();
                List<ApplyPromotionByOrderList> AppliedPromotions = new List<ApplyPromotionByOrderList>();
                string query = @$"SELECT * FROM ""public"".""f_getorderpromotionsbycode""('{inp.DistributorCode}', ARRAY[{promo}])";
                var trans = (List<ApplyPromotionByOrderList>)_dapperRepositories.Query<ApplyPromotionByOrderList>(query);


                if (trans is null || !trans.Any())
                {
                    apply.NotApplyPromotions = inp.ProductList;
                    result.Data = apply;
                    result.Success = true;
                    return Ok(result);
                }


                var types = trans.Select(f => f.UomType)?.Distinct()?.ToList();
                string arr = "";
                for (int i = 0; i < types.Count; i++)
                {
                    if (i == 0)
                        arr += $"'{types[i]}'";
                    else
                        arr += $", '{types[i]}'";
                }

                string productStr = "";
                List<ProductItemCustomize> customizes = new List<ProductItemCustomize>();
                foreach (var item in inp.ProductList)
                {
                    ProductItemCustomize customize = new ProductItemCustomize()
                    {
                        BaseQuantity = item.BaseQuantity,
                        BaseUom = item.BaseUom,
                        ItemCode = item.ItemCode
                    };
                    customizes.Add(customize);
                }
                string jsonProduct = JsonConvert.SerializeObject(customizes);
                query = @$"SELECT * FROM ""public"".""f_getgiftsfromapplypromotionbycode""('{jsonProduct}', ARRAY[{arr}])";
                var quantitiesForOrders = (List<QuantitiesForOrderPromotion>)_dapperRepositories.Query<QuantitiesForOrderPromotion>(query);

                #endregion

                foreach (var myType in types)
                {
                    foreach (var tran in trans)
                    {
                        if (apply.AppliedPromotions.Any() && apply.AppliedPromotions.Exists(f => f.PromotionId == tran.PromotionId))
                            continue;
                        NotApplyPromotions = new List<ProductItem>();
                        AppliedPromotions = new List<ApplyPromotionByOrderList>();
                        #region  Loop CTKM

                        var groupedQuantities = quantitiesForOrders
                                        .GroupBy(q => new { q.ItemCode, q.BaseUom })
                                        .Select(g =>
                                        {
                                            var first = g.First();
                                            return new QuantitiesForOrderPromotion
                                            {
                                                ItemCode = first.ItemCode,
                                                BaseUom = first.BaseUom,
                                                Quantity = g.Sum(x => x.Quantity),
                                                BaseQuantity = first.BaseQuantity,
                                                ConversionFactor = first.ConversionFactor,
                                                PromotionUom = first.PromotionUom,
                                                UomType = first.UomType
                                            };
                                        })
                                        .ToList();

                        if (tran.SkuFullCase)
                        {
                            foreach (var element in groupedQuantities)
                            {
                                element.Quantity = Math.Floor(element.Quantity);
                            }
                        }
                        decimal rawInfo = groupedQuantities.Where(f => f.UomType == myType).Sum(q => q.Quantity);
                        var raw = groupedQuantities.FirstOrDefault(f => f.UomType == myType);

                        raw.Quantity = rawInfo;

                        var findInp = new ProductItem()
                        {
                            ItemCode = raw.ItemCode,
                            BaseUom = raw.BaseUom,
                            BaseQuantity = raw.BaseQuantity,
                            Quantity = raw.Quantity
                        };


                        if (findInp is null || string.IsNullOrEmpty(findInp.ItemCode))
                            continue;

                        List<ApplyPromotionByOrderList> applyPromotionByOrderList = new List<ApplyPromotionByOrderList>();
                        applyPromotionByOrderList.Add(tran);
                        if (tran.OrderBy == "Quantity")
                        {

                            var res = CalculatorQuantityWithMultiPromo(applyPromotionByOrderList, findInp);
                            if (res != null)
                            {
                                NotApplyPromotions.AddRange(res.Item1);
                                AppliedPromotions.AddRange(res.Item2);
                            }

                        }
                        else
                        {
                            var res = CalculatorValue(applyPromotionByOrderList, findInp);
                            if (res != null)
                            {
                                NotApplyPromotions.AddRange(res.Item1);
                                AppliedPromotions.AddRange(res.Item2);
                            }

                        }
                        apply.AppliedPromotions.AddRange(AppliedPromotions);
                        apply.NotApplyPromotions.AddRange(NotApplyPromotions);

                        #endregion
                    }
                }

                #endregion

                #region CheckBudget
                apply = ReCheckBudget(apply, inp);
                #endregion


                #region CTKM
                if (apply.AppliedPromotions is not null && apply.AppliedPromotions.Any())
                {
                    #region query lấy danh sách KM
                    var lstPromo = apply.AppliedPromotions.Select(f => new { f.PromotionId, f.LevelId });
                    string strPromo = "";
                    string strArr = "";
                    bool IsFirst = true;
                    foreach (var item in lstPromo)
                    {
                        if (IsFirst)
                        {
                            strPromo += $"'{item.PromotionId}'";
                            strArr += @$"'{{{item.PromotionId},{item.LevelId}}}'";
                        }
                        else
                        {
                            strPromo += $", '{item.PromotionId}'";
                            strArr += @$", '{{{item.PromotionId},{item.LevelId}}}'";
                        }
                        IsFirst = false;
                    }
                    string giftQuery = @$"SELECT * FROM ""public"".""f_getgiftsfromapplypromotion""('{inp.DistributorCode}', ARRAY[{strPromo}], ARRAY[{strArr}]);";
                    var gifts = (List<GiftPromotion>)_dapperRepositories.Query<GiftPromotion>(giftQuery);
                    var diff = gifts.Select(f => new { f.LevelId, f.PromotionId })?.Distinct()?.ToList();
                    #endregion

                    foreach (var element in diff)
                    {
                        var items = gifts.Where(f => f.PromotionId == element.PromotionId && f.LevelId == element.LevelId)?.ToList();
                        var applyPromo = apply.AppliedPromotions.FirstOrDefault(f => f.PromotionId == element.PromotionId && f.LevelId == element.LevelId);
                        var rawQuan = quantitiesForOrders.FirstOrDefault();
                        var productInfo = new ProductItem()
                        {
                            ItemCode = rawQuan.ItemCode,
                            BaseUom = rawQuan.BaseUom,
                            BaseQuantity = rawQuan.BaseQuantity,
                            Quantity = rawQuan.Quantity
                        };
                        if (items is null || !items.Any())
                            continue;

                        var type = items.Select(f => f.ProductType)?.Distinct().ToList();

                        foreach (var item in type)
                        {
                            var choose = items.Where(f => f.ProductType == item)?.ToList();

                            foreach (var km in choose)
                            {
                                if (km.FreeItemType == "True") // tang  hang
                                {
                                    var KMasList = new List<GiftPromotion>();
                                    KMasList.Add(km);
                                    // Tặng 1 loại hoặc nhiều loại sản phẩm  
                                    if (km.RuleOfGiving) // tang nhieu
                                    {
                                        applyPromo = CalculatorTotalFreeQty(KMasList, applyPromo);
                                    }
                                    else // tang 1
                                    {
                                        if (km.FreeSameProduct ?? false)
                                        {
                                            applyPromo = CalculatorTotalFreeQty(choose, applyPromo);
                                        }
                                        else
                                        {
                                            applyPromo = CalculatorTotalFreeQty(KMasList, applyPromo);

                                        }

                                    }
                                }
                                else // tang tien
                                {
                                    if (choose[0].FreeAmountType is not null && string.IsNullOrEmpty(choose[0].FreeItemType) && !string.IsNullOrEmpty(choose[0].DiscountType))
                                    {
                                        if (choose[0].DiscountType == "Discount")
                                        {


                                            applyPromo = CalculatorDisCountAmount(choose, applyPromo, productInfo);

                                        }
                                        else if (choose[0].DiscountType == "Donate")
                                        {

                                            applyPromo = CalculatorDonateAmount(choose, applyPromo, productInfo);

                                        }
                                    }
                                }
                            }

                        }
                        // Check tye is Quantity / Amount

                    }
                }
                #endregion

                result.Data = apply;
                result.Success = true;
            }
            catch (Exception ex)
            {


            }
            return Ok(result);
        }

        #region helpers
        public static (List<ProductItem> picked, List<ProductItem> leftover) PickBox(List<ProductItem> input, int capacity)
        {
            var picked = new List<ProductItem>();
            var leftover = new List<ProductItem>();

            int remaining = capacity;

            foreach (var item in input.OrderByDescending(i => i.BaseQuantity ?? 0))
            {
                int itemQty = item.BaseQuantity ?? 0;
                if (remaining == 0)
                {
                    leftover.Add(item);
                    continue;
                }

                if (itemQty <= remaining)
                {
                    picked.Add(item);
                    remaining -= itemQty;
                }
                else
                {
                    // Chia ra 2 phần: phần lấy đủ để tròn thùng và phần dư
                    picked.Add(item.CloneWithNewQuantity(remaining));
                    leftover.Add(item.CloneWithNewQuantity(itemQty - remaining));
                    remaining = 0;
                }
            }

            return (picked, leftover);
        }

        private Tuple<List<ProductItem>, List<ApplyPromotionByOrderList>> CalculatorQuantityWithMultiPromo(List<ApplyPromotionByOrderList> promotions, ProductItem product)
        {
            List<ProductItem> NotApplyPromotions = new List<ProductItem>();
            List<ApplyPromotionByOrderList> AppliedPromotions = new List<ApplyPromotionByOrderList>();
            try
            {
                bool IsCastToBase = false;
                string currentPackingUomId = null;
                int rate = 0;
                if (!promotions.Exists(f => f.PackingUomId == product.Uom) && product.Uom == product.BaseUom)
                {
                    foreach (var item in promotions)
                    {
                        if (item.LevelOrderQty > 0)
                        {
                            if (item.PackingUomId != item.BaseUnit)
                            {
                                item.LevelOrderQty = item.LevelOrderQty * item.ConversionFactor;
                                item.PackingUomId = item.BaseUnit;
                            }
                        }
                    }
                }
                else if (!promotions.Exists(f => f.PackingUomId == product.Uom) && product.Uom != product.BaseUom)
                {
                    foreach (var item in promotions)
                    {
                        if (item.LevelOrderQty > 0)
                        {
                            if (item.PackingUomId != item.BaseUnit)
                            {
                                item.LevelOrderQty = item.LevelOrderQty * item.ConversionFactor;
                                item.PackingUomId = item.BaseUnit;
                            }
                            rate = (product.BaseQuantity ?? 0) / (int)product.Quantity;
                            currentPackingUomId = product.Uom;
                            product.Uom = product.BaseUom;
                            product.Quantity = product.BaseQuantity ?? 0;
                            IsCastToBase = true;
                        }
                    }
                }

                if (promotions.Exists(f => f.LevelOrderQty <= product.Quantity))
                {
                    var promos = promotions
                            .Where(f => f.LevelOrderQty <= product.Quantity)?.ToList();
                    promos = promos
                            .OrderByDescending(f => f.LevelOrderQty) // Sắp xếp tăng dần theo LevelOrderQty
                            .ToList();
                    if (promos.Count == 0)
                    {
                        NotApplyPromotions.Add(product);
                        return System.Tuple.Create(NotApplyPromotions, AppliedPromotions);
                    }
                    List<PromotionManyDiscount> khuyenMai = new List<PromotionManyDiscount>();
                    foreach (var pro in promos)
                    {
                        PromotionManyDiscount manyDiscount = new PromotionManyDiscount()
                        {
                            PromotionId = pro.PromotionId,
                            LevelId = pro.LevelId,
                            LevelOrderAmount = pro.LevelOrderAmount,
                            LevelOrderQuantity = pro.LevelOrderQty
                        };
                        khuyenMai.Add(manyDiscount);
                    }
                    foreach (var promo in promos)
                    {
                        if (promo.OrderRule == "AccordingPassLevel")
                        {
                            if (product.Quantity >= promo.LevelOrderQty)
                            {
                                khuyenMai.Sort((a, b) => (b.LevelOrderQuantity ?? 0).CompareTo(a.LevelOrderQuantity ?? 0));
                                int Sothung = (int)product.Quantity;
                                foreach (var km in khuyenMai)
                                {
                                    Sothung = (int)product.Quantity;
                                    if (Sothung < km.LevelOrderQuantity)
                                        continue;
                                    var clone = promotions.FirstOrDefault(f => f.PromotionId == km.PromotionId);
                                    clone.BudgetQuantity = (int)Sothung / clone.LevelOrderQty;
                                    clone.Quantity = (clone.BudgetQuantity * clone.LevelOrderQty);
                                    Sothung = Sothung % clone.LevelOrderQty;
                                    if (promo.BudgetQuantity > 0)
                                        AppliedPromotions.Add(clone);
                                }
                                decimal so_du = Sothung;
                                if (so_du > 0)
                                {
                                    var clone = new ProductItem();
                                    clone.ItemCode = product.ItemCode;
                                    clone.ItemGroupCode = product.ItemGroupCode;
                                    clone.Uom = product.Uom;
                                    clone.Price = product.Price;
                                    clone.Quantity = so_du;
                                    clone.TotalAmount = clone.Quantity * clone.Price;
                                    clone.BaseQuantity = (int)so_du * promo.ConversionFactor;
                                    NotApplyPromotions.Add(clone);
                                }
                            }
                            else
                            {
                                NotApplyPromotions.Add(product);
                            }

                            return System.Tuple.Create(NotApplyPromotions, AppliedPromotions);
                        }
                        else
                        {
                            if (product.Quantity >= promo.LevelOrderQty)
                            {
                                khuyenMai.Sort((a, b) => (b.LevelOrderQuantity ?? 0).CompareTo(a.LevelOrderQuantity ?? 0));
                                int Sothung = (int)product.Quantity;
                                foreach (var km in khuyenMai)
                                {
                                    if (Sothung < promo.LevelOrderQty)
                                        break;
                                    promo.BudgetQuantity = (int)(product.Quantity / promo.OnEach);
                                    promo.Quantity = (int)(promo.BudgetQuantity * promo.OnEach);
                                    Sothung = Sothung % promo.LevelOrderQty;
                                    AppliedPromotions.Add(promo);
                                }
                                decimal so_du = Sothung;
                                if (so_du > 0)
                                {
                                    var clone = new ProductItem();
                                    clone.ItemCode = product.ItemCode;
                                    clone.ItemGroupCode = product.ItemCode;
                                    clone.Uom = product.Uom;
                                    clone.Price = product.Price;
                                    clone.Quantity = (int)so_du;
                                    clone.TotalAmount = clone.Quantity * clone.Price;
                                    clone.BaseQuantity = (int)so_du * promo.ConversionFactor;
                                    NotApplyPromotions.Add(clone);
                                }
                            }
                            else
                            {
                                NotApplyPromotions.Add(product);
                            }
                        }
                    }

                }
                else
                {
                    NotApplyPromotions.Add(product);
                }

                if (IsCastToBase)
                {
                    if (NotApplyPromotions.Any())
                    {
                        foreach (var app in NotApplyPromotions)
                        {
                            app.Uom = currentPackingUomId;
                            app.Quantity = app.Quantity / rate;
                        }
                    }
                }

            }
            catch
            {

            }
            return System.Tuple.Create(NotApplyPromotions, AppliedPromotions);
        }

        private Tuple<List<ProductItem>, List<ApplyPromotionByOrderList>> CalculatorQuantity(List<ApplyPromotionByOrderList> promotions, ProductItem product)
        {
            List<ProductItem> NotApplyPromotions = new List<ProductItem>();
            List<ApplyPromotionByOrderList> AppliedPromotions = new List<ApplyPromotionByOrderList>();
            try
            {
                bool IsCastToBase = false;
                string currentPackingUomId = null;
                int rate = 0;
                if (!promotions.Exists(f => f.PackingUomId == product.Uom) && product.Uom == product.BaseUom)
                {
                    foreach (var item in promotions)
                    {
                        if (item.LevelOrderQty > 0)
                        {
                            if (item.PackingUomId != item.BaseUnit)
                            {
                                item.LevelOrderQty = item.LevelOrderQty * item.ConversionFactor;
                                item.PackingUomId = item.BaseUnit;
                            }
                        }
                    }
                }
                else if (!promotions.Exists(f => f.PackingUomId == product.Uom) && product.Uom != product.BaseUom)
                {
                    foreach (var item in promotions)
                    {
                        if (item.LevelOrderQty > 0)
                        {
                            if (item.PackingUomId != item.BaseUnit)
                            {
                                item.LevelOrderQty = item.LevelOrderQty * item.ConversionFactor;
                                item.PackingUomId = item.BaseUnit;
                            }
                            rate = (product.BaseQuantity ?? 0) / (int)product.Quantity;
                            currentPackingUomId = product.Uom;
                            product.Uom = product.BaseUom;
                            product.Quantity = product.BaseQuantity ?? 0;
                            IsCastToBase = true;
                        }
                    }
                }

                if (promotions.Exists(f => f.LevelOrderQty <= product.Quantity))
                {
                    var promos = promotions
                            .Where(f => f.LevelOrderQty <= product.Quantity)?.ToList();
                    var promo = promos
                            .OrderByDescending(f => f.LevelOrderQty) // Sắp xếp tăng dần theo LevelOrderQty
                            .FirstOrDefault();
                    if (promos.Count == 0)
                    {
                        NotApplyPromotions.Add(product);
                        return System.Tuple.Create(NotApplyPromotions, AppliedPromotions);
                    }
                    List<PromotionManyDiscount> khuyenMai = new List<PromotionManyDiscount>();
                    foreach (var pro in promos)
                    {
                        PromotionManyDiscount manyDiscount = new PromotionManyDiscount()
                        {
                            PromotionId = pro.PromotionId,
                            LevelId = pro.LevelId,
                            LevelOrderAmount = pro.LevelOrderAmount,
                            LevelOrderQuantity = pro.LevelOrderQty
                        };
                        khuyenMai.Add(manyDiscount);
                    }

                    if (promo.OrderRule == "AccordingPassLevel")
                    {
                        if (product.Quantity >= promo.LevelOrderQty)
                        {
                            khuyenMai.Sort((a, b) => (b.LevelOrderQuantity ?? 0).CompareTo(a.LevelOrderQuantity ?? 0));
                            int Sothung = (int)product.Quantity;
                            foreach (var km in khuyenMai)
                            {
                                if (Sothung < km.LevelOrderQuantity)
                                    continue;
                                promo = promos.FirstOrDefault(f => f.PromotionId == km.PromotionId && f.LevelId == km.LevelId);
                                promo.BudgetQuantity = (int)Sothung / promo.LevelOrderQty;
                                promo.Quantity = (promo.BudgetQuantity * promo.LevelOrderQty);
                                Sothung = Sothung % promo.LevelOrderQty;
                                AppliedPromotions.Add(promo);
                            }
                            decimal so_du = Sothung;
                            if (so_du > 0)
                            {
                                var clone = new ProductItem();
                                clone.ItemCode = product.ItemCode;
                                clone.ItemGroupCode = product.ItemGroupCode;
                                clone.Uom = product.Uom;
                                clone.Price = product.Price;
                                clone.Quantity = so_du;
                                clone.TotalAmount = clone.Quantity * clone.Price;
                                clone.BaseQuantity = (int)so_du * promo.ConversionFactor;
                                NotApplyPromotions.Add(clone);
                            }
                        }
                        else
                        {
                            NotApplyPromotions.Add(product);
                        }
                    }
                    else
                    {
                        if (product.Quantity >= promo.LevelOrderQty)
                        {
                            khuyenMai.Sort((a, b) => (b.LevelOrderQuantity ?? 0).CompareTo(a.LevelOrderQuantity ?? 0));
                            int Sothung = (int)product.Quantity;
                            foreach (var km in khuyenMai)
                            {
                                if (Sothung < promo.LevelOrderQty)
                                    break;
                                promo = promos.FirstOrDefault(f => f.PromotionId == km.PromotionId && f.LevelId == km.LevelId);
                                promo.BudgetQuantity = (int)(product.Quantity / promo.OnEach);
                                promo.Quantity = (int)(promo.BudgetQuantity * promo.OnEach);
                                Sothung = Sothung % promo.LevelOrderQty;
                                AppliedPromotions.Add(promo);
                            }
                            decimal so_du = Sothung;
                            if (so_du > 0)
                            {
                                var clone = new ProductItem();
                                clone.ItemCode = product.ItemCode;
                                clone.ItemGroupCode = product.ItemCode;
                                clone.Uom = product.Uom;
                                clone.Price = product.Price;
                                clone.Quantity = (int)so_du;
                                clone.TotalAmount = clone.Quantity * clone.Price;
                                clone.BaseQuantity = (int)so_du * promo.ConversionFactor;
                                NotApplyPromotions.Add(clone);
                            }
                        }
                        else
                        {
                            NotApplyPromotions.Add(product);
                        }
                    }
                }
                else
                {
                    NotApplyPromotions.Add(product);
                }

                if (IsCastToBase)
                {
                    if (NotApplyPromotions.Any())
                    {
                        foreach (var app in NotApplyPromotions)
                        {
                            app.Uom = currentPackingUomId;
                            app.Quantity = app.Quantity / rate;
                        }
                    }
                }

            }
            catch
            {

            }
            return System.Tuple.Create(NotApplyPromotions, AppliedPromotions);
        }
        private Tuple<List<ProductItem>, List<ApplyPromotionByOrderList>> CalculatorValue(List<ApplyPromotionByOrderList> promotions, ProductItem product)
        {
            List<ProductItem> NotApplyPromotions = new List<ProductItem>();
            List<ApplyPromotionByOrderList> AppliedPromotions = new List<ApplyPromotionByOrderList>();
            try
            {
                if (promotions.Exists(f => f.LevelOrderQty <= product.TotalAmount))
                {
                    var promos = promotions
                            .Where(f => f.LevelOrderAmount <= product.TotalAmount && f.ProductCode == product.ItemCode)?.ToList();
                    var promo = promos
                            .OrderByDescending(f => f.LevelOrderAmount) // Sắp xếp tăng dần theo LevelOrderQty
                            .FirstOrDefault();
                    if (promos.Count == 0)
                    {
                        NotApplyPromotions.Add(product);
                        return System.Tuple.Create(NotApplyPromotions, AppliedPromotions);
                    }
                    List<PromotionManyDiscount> khuyenMai = new List<PromotionManyDiscount>();
                    foreach (var pro in promos)
                    {
                        PromotionManyDiscount manyDiscount = new PromotionManyDiscount()
                        {
                            PromotionId = pro.PromotionId,
                            LevelId = pro.LevelId,
                            LevelOrderAmount = pro.LevelOrderAmount,
                            LevelOrderQuantity = pro.LevelOrderQty
                        };
                        khuyenMai.Add(manyDiscount);
                    }
                    if (promo is null)
                    {
                        NotApplyPromotions.Add(product);
                        return System.Tuple.Create(NotApplyPromotions, AppliedPromotions);
                    }
                    if (promo.OrderRule == "AccordingPassLevel")
                    {
                        if (product.TotalAmount >= promo.LevelOrderAmount)
                        {
                            khuyenMai.Sort((a, b) => (b.LevelOrderAmount ?? 0).CompareTo(a.LevelOrderAmount ?? 0));
                            decimal Sothung = (int)product.TotalAmount;
                            foreach (var km in khuyenMai)
                            {
                                if (Sothung <= 0)
                                    break;
                                promo = promos.FirstOrDefault(f => f.PromotionId == km.PromotionId && f.LevelId == km.LevelId);
                                promo.BudgetQuantity = (int)(product.TotalAmount / promo.LevelOrderAmount);
                                Sothung = Sothung % promo.LevelOrderAmount ?? 0;
                                AppliedPromotions.Add(promo);
                            }
                            decimal diff = Sothung;

                            if (diff > 0)
                            {
                                int so_nguyen = (int)(diff / product.Price);
                                promo.Quantity = (int)product.Quantity - so_nguyen;

                                var clone = new ProductItem();
                                clone.Quantity = so_nguyen;
                                clone.TotalAmount = so_nguyen * product.Price;
                                clone.Price = product.Price;
                                clone.ItemCode = product.ItemCode;
                                clone.ItemGroupCode = product.ItemCode;
                                clone.BaseQuantity = null;
                                NotApplyPromotions.Add(clone);
                            }
                            else
                            {
                                promo.Quantity = (int)product.Quantity;
                            }
                            AppliedPromotions.Add(promo);
                        }
                        else
                        {
                            NotApplyPromotions.Add(product);
                        }
                    }
                    else
                    {
                        if (product.TotalAmount >= promo.LevelOrderAmount)
                        {
                            khuyenMai.Sort((a, b) => (b.LevelOrderAmount ?? 0).CompareTo(a.LevelOrderAmount ?? 0));
                            decimal Sothung = (int)product.TotalAmount;
                            foreach (var km in khuyenMai)
                            {
                                if (Sothung <= 0)
                                    break;
                                promo = promos.FirstOrDefault(f => f.PromotionId == km.PromotionId && f.LevelId == km.LevelId);
                                promo.BudgetQuantity = (int)(product.TotalAmount / promo.OnEach);
                                Sothung = Sothung % promo.LevelOrderAmount ?? 0;
                                AppliedPromotions.Add(promo);
                            }
                            decimal diff = (int)(product.TotalAmount % promo.LevelOrderAmount);

                            if (diff > 0)
                            {
                                int so_nguyen = (int)(diff / product.Price);
                                promo.Quantity = (int)product.Quantity - so_nguyen;

                                var clone = new ProductItem();
                                clone.Quantity = so_nguyen;
                                clone.TotalAmount = so_nguyen * product.Price;
                                clone.Price = product.Price;
                                clone.ItemCode = product.ItemCode;
                                clone.ItemGroupCode = product.ItemCode;
                                clone.BaseQuantity = null;
                                NotApplyPromotions.Add(clone);
                            }
                            else
                            {
                                promo.Quantity = (int)product.Quantity;
                            }
                            AppliedPromotions.Add(promo);
                        }

                    }
                }
            }
            catch (Exception ex)
            {

            }
            return System.Tuple.Create(NotApplyPromotions, AppliedPromotions);
        }
        private ApplyPromotionByOrderList CalculatorTotalFreeQty(List<GiftPromotion> promo, ApplyPromotionByOrderList apply)
        {
            int totalFreeQty = 0;
            if (promo[0].FreeItemType == "True")
            {
                bool IsSpilit = false;
                var _default = FindDefault(promo, out IsSpilit);
                if (IsSpilit)
                {
                    foreach (var item in promo)
                    {
                        item.TotalFreeQuantity = apply.BudgetQuantity * (item.LevelFreeQty ?? item.NumberOfFreeItem) ?? 0;
                    }
                    apply.Gifts = promo;
                    apply.LevelTotalFreeQuantity = promo.Select(f => f.TotalFreeQuantity).Sum();
                }
                else if (!IsSpilit && _default is not null)
                {
                    if (promo.Count == 1)
                    {
                        var el = promo[0];
                        el.TotalFreeQuantity = apply.BudgetQuantity * (el.LevelFreeQty ?? el.NumberOfFreeItem) ?? 0;
                        if (apply.Gifts is null)
                            apply.Gifts = new List<GiftPromotion>();
                        apply.Gifts.Add(el);
                        apply.LevelTotalFreeQuantity = apply.BudgetQuantity * (el.LevelFreeQty ?? el.NumberOfFreeItem) ?? 0;
                    }
                    else
                    {
                        var el = promo.FirstOrDefault(f => f.IsDefaultProduct == true);
                        promo.FirstOrDefault(f => f.IsDefaultProduct == true).TotalFreeQuantity = apply.BudgetQuantity * (el.LevelFreeQty ?? el.NumberOfFreeItem) ?? 0;
                        if (apply.Gifts is null)
                            apply.Gifts = new List<GiftPromotion>();
                        apply.Gifts.Add(el);
                        apply.LevelTotalFreeQuantity = apply.BudgetQuantity * (el.LevelFreeQty ?? el.NumberOfFreeItem) ?? 0;
                    }

                }
            }
            return apply;
        }

        private ApplyPromotion CalculatorTotalFreeQtyByGroup(List<GiftPromotion> promo, ApplyPromotion apply)
        {
            int totalFreeQty = 0;
            if (promo[0].FreeItemType == "True")
            {
                bool IsSpilit = false;
                var _default = FindDefault(promo, out IsSpilit);
                if (IsSpilit)
                {
                    foreach (var item in promo)
                    {
                        item.TotalFreeQuantity = (int?)(apply.BudgetQuantity * (item.LevelFreeQty ?? item.NumberOfFreeItem) ?? 0);
                    }

                    apply.Gifts = promo;
                    apply.LevelTotalFreeQuantity = promo.Select(f => f.TotalFreeQuantity).Sum();
                }
                else if (!IsSpilit && _default is not null)
                {
                    if (promo.Count == 1)
                    {
                        var el = promo[0];
                        el.TotalFreeQuantity = (int?)(apply.BudgetQuantity * (el.LevelFreeQty ?? el.NumberOfFreeItem) ?? 0);
                        if (apply.Gifts is null)
                            apply.Gifts = new List<GiftPromotion>();
                        apply.Gifts.Add(el);
                        apply.LevelTotalFreeQuantity = apply.BudgetQuantity * (el.LevelFreeQty ?? el.NumberOfFreeItem) ?? 0;
                    }
                    else
                    {
                        var el = promo.FirstOrDefault(f => f.IsDefaultProduct == true);
                        promo.FirstOrDefault(f => f.IsDefaultProduct == true).TotalFreeQuantity = (int?)(apply.BudgetQuantity * (el.LevelFreeQty ?? el.NumberOfFreeItem) ?? 0);
                        if (apply.Gifts is null)
                            apply.Gifts = new List<GiftPromotion>();
                        apply.Gifts.Add(el);
                        apply.LevelTotalFreeQuantity = apply.BudgetQuantity * (el.LevelFreeQty ?? el.NumberOfFreeItem) ?? 0;
                    }

                }
            }
            return apply;
        }
        private ApplyPromotionByOrderList CalculatorDonateAmount(List<GiftPromotion> promo, ApplyPromotionByOrderList apply, ProductItem prodItem)
        {
            int totalFreeQty = 0;
            if (!string.IsNullOrEmpty(promo[0].FreeAmountType))
            {

                if (promo.Count == 1 && promo[0].FreeAmountType == "Amount")
                {
                    var el = promo[0];
                    el.OrgUnitPrice = prodItem.Price;
                    el.UnitPrice = prodItem.Price;
                    el.OrgTotalAmount = prodItem.TotalAmount;
                    decimal KM = apply.BudgetQuantity * el.FreeAmount ?? 0;
                    el.TotalFreeAmount = KM;
                    el.TotalAmount = prodItem.TotalAmount - KM;
                    apply.LevelTotalFreeAmount = KM;
                    el.TotalFreeQuantity = (int)(el.TotalFreeAmount / prodItem.Price);
                    if (apply.Gifts is null)
                        apply.Gifts = new List<GiftPromotion>();
                    apply.Gifts.Add(el);
                }
                else if (promo.Count == 1 && promo[0].FreeAmountType == "Percent")
                {
                    var el = promo[0];
                    decimal KM = el.FreePercentAmount ?? 0;
                    el.OrgUnitPrice = prodItem.Price;
                    el.UnitPrice = prodItem.Price;
                    el.OrgTotalAmount = prodItem.TotalAmount;
                    el.TotalFreeAmount = prodItem.TotalAmount * (el.FreePercentAmount / 100);
                    el.TotalAmount = prodItem.TotalAmount - el.TotalFreeAmount;
                    apply.LevelTotalFreeAmount = el.TotalFreeAmount;
                    el.TotalFreeQuantity = (int)(el.TotalFreeAmount / prodItem.Price);
                    if (apply.Gifts is null)
                        apply.Gifts = new List<GiftPromotion>();
                    apply.Gifts.Add(el);
                }
            }
            return apply;
        }
        private ApplyPromotionByOrderList CalculatorDisCountAmount(List<GiftPromotion> promo, ApplyPromotionByOrderList apply, ProductItem prodItem)
        {
            int totalFreeQty = 0;
            if (!string.IsNullOrEmpty(promo[0].FreeAmountType))
            {

                if (promo.Count == 1 && promo[0].FreeAmountType == "Amount")
                {
                    var el = promo[0];
                    el.OrgUnitPrice = prodItem.Price;
                    el.OrgTotalAmount = el.OrgUnitPrice * apply.Quantity;
                    decimal KM = el.FreeAmount ?? 0;

                    el.UnitPrice = prodItem.Price - el.FreeAmount;
                    el.TotalFreeAmount = prodItem.TotalAmount - (el.UnitPrice * prodItem.Quantity);
                    el.TotalAmount = el.UnitPrice * prodItem.Quantity;
                    apply.LevelTotalFreeAmount = el.TotalFreeAmount;
                    el.TotalFreeQuantity = (int)(el.TotalFreeAmount / prodItem.Price);
                    if (apply.Gifts is null)
                        apply.Gifts = new List<GiftPromotion>();
                    apply.Gifts.Add(el);
                }
                else if (promo.Count == 1 && promo[0].FreeAmountType == "Percent")
                {
                    var el = promo[0];
                    decimal KM = el.FreePercentAmount ?? 0;
                    el.OrgUnitPrice = prodItem.Price;
                    el.UnitPrice = prodItem.Price - (prodItem.Price * (KM / 100));
                    el.OrgTotalAmount = el.OrgUnitPrice * apply.Quantity;
                    el.TotalAmount = el.UnitPrice * apply.Quantity; //prodItem.TotalAmount - (KM  / 100) * prodItem.TotalAmount;
                    el.TotalFreeAmount = el.OrgTotalAmount - el.TotalAmount;
                    el.TotalFreeQuantity = (int)((el.OrgTotalAmount - el.TotalAmount) / prodItem.Price);
                    apply.LevelTotalFreeAmount = el.TotalFreeAmount;
                    if (apply.Gifts is null)
                        apply.Gifts = new List<GiftPromotion>();
                    apply.Gifts.Add(el);
                }
            }
            return apply;
        }
        private GiftPromotion FindDefault(List<GiftPromotion> promo, out bool IsSpilit)
        {
            IsSpilit = false;
            try
            {
                if (promo.Count == 1)
                    return promo[0];
                else if (promo.Exists(f => f.IsDefaultProduct == true))
                {
                    var element = promo.FirstOrDefault(f => f.IsDefaultProduct == true);
                    return element;
                }
                else
                    IsSpilit = true;
            }
            catch (Exception ex)
            {

            }
            return null;
        }
        private ApplyPromotionResponse ReCheckBudget(ApplyPromotionResponse promotionResponse, RequestEnhanceApplyPromotionByOrderList inp)
        {
            try
            {
                if (promotionResponse.AppliedPromotions.Any() && promotionResponse.AppliedPromotions.Exists(f => f.IsApplyBudget))
                {
                    var promotions = promotionResponse.AppliedPromotions.Where(f => f.IsApplyBudget)?.ToList();
                    var codes = promotions.Select(f => f.BudgetValueCode ?? f.BudgetQtyCode)?.ToList();
                    List<BudgetRequest> Budgets = new List<BudgetRequest>();
                    foreach (var code in promotions)
                    {
                        BudgetRequest budget = new BudgetRequest()
                        {
                            BudgetCode = code.BudgetValueCode ?? code.BudgetQtyCode,
                            LevelId = code.LevelId,
                            PromotionId = code.PromotionId,
                            BudgetQuantity = code.BudgetQuantity
                        };
                        Budgets.Add(budget);
                    }

                    #region call API
                    _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == "ODTpAPI").Select(x => x.Url).FirstOrDefault());
                    _client.Authenticator = new JwtAuthenticator($"Rdos {_token}");
                    var request = new RestRequest("external_checkbudget/getcustomerbudget", Method.POST, DataFormat.Json);
                    request.AddHeader("accept", "*/*");
                    request.AddHeader("DistributorCode", inp.DistributorCode);
                    request.AddHeader("Content-Type", "application/json");
                    var body = @"{" + "\n" +
                    @$"  ""SaleOrgCode"": ""{inp.SaleOrgCode}""," + "\n" +
                    @$"  ""SicCode"": ""{inp.SicCode}""," + "\n" +
                    @$"  ""CustomerCode"": ""{inp.CustomerCode ?? "null"}""," + "\n" +
                    @$"  ""ShiptoCode"": ""{inp.ShiptoCode ?? "null"}""," + "\n" +
                    @$"  ""RouteZoneCode"": ""{inp.RouteZoneCode ?? "null"}""," + "\n" +
                    @$"  ""DsaCode"": ""{inp.DsaCode ?? "null"}""," + "\n" +
                    @$"  ""Branch"": {inp.Branch ?? "null"}," + "\n" +
                    @$"  ""Region"": ""{inp.Region ?? "null"}""," + "\n" +
                    @$"  ""SubRegion"": {inp.SubRegion ?? "null"}," + "\n" +
                    @$"  ""Area"": ""{inp.Area ?? "null"}""," + "\n" +
                    @$"  ""SubArea"": {inp.SubArea ?? "null"}," + "\n" +
                    @$"  ""DistributorCode"": ""{inp.DistributorCode ?? "null"}""," + "\n" +
                    @$"  ""budgets"": {JsonConvert.SerializeObject(Budgets)}" + "\n" +
                    @"}";
                    request.AddBody(body);
                    var response = _client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        CheckBudgetResponse content = JsonConvert.DeserializeObject<CheckBudgetResponse>(response.Content);
                        if (content.Data.Any())
                        {
                            var data = content.Data;
                            foreach (var item in data)
                            {
                                int availableBudget = item.BudgetRemains ?? int.MaxValue;
                                int maxAllowed = item.CustomerBudget.HasValue ? (item.BudgetBooked == 0 ? (item.CustomerBudget ?? 0) : (item.BudgetBooked ?? 0)) : int.MaxValue;

                                var thisPromo = promotionResponse.AppliedPromotions.Where(f => f.IsApplyBudget && ((f.BudgetQtyCode?.Equals(item.BudgetCode) ?? false) || (f.BudgetValueCode?.Equals(item.BudgetCode) ?? false)))?.FirstOrDefault();

                                int applied = Math.Min(thisPromo.BudgetQuantity, Math.Min(maxAllowed, availableBudget));
                                int notApplied = thisPromo.BudgetQuantity - applied;



                                if (applied > 0)
                                {
                                    int levelQty = (int)(thisPromo.LevelOrderQty == 0 ? thisPromo.LevelOrderAmount : thisPromo.LevelOrderQty);
                                    thisPromo.BudgetQuantity = applied;
                                    thisPromo.Quantity = applied * levelQty;
                                }

                                if (notApplied > 0)
                                {
                                    var element = promotionResponse.NotApplyPromotions.FirstOrDefault(f => thisPromo.ProductCode == f.ItemCode);
                                    if (element is not null)
                                    {
                                        if (thisPromo.OrderBy == "Quantity")
                                        {
                                            decimal calculator = notApplied * thisPromo.LevelOrderQty;
                                            element.Quantity += calculator;
                                            element.TotalAmount = element.Quantity * element.Price;
                                        }

                                        if (applied != element.Quantity && applied > 0)
                                        {
                                            element.Quantity = applied;
                                        }
                                        else if (applied != element.Quantity && applied == 0)
                                        {

                                        }
                                    }
                                    else
                                    {
                                        ProductItem notApplyProduct = new ProductItem();
                                        if (thisPromo.ProductCode is not null)
                                            notApplyProduct = inp.ProductList.Where(f => f.ItemCode == thisPromo.ProductCode)?.FirstOrDefault();
                                        else
                                            notApplyProduct = inp.ProductList.FirstOrDefault();
                                        if (promotionResponse.NotApplyPromotions is null)
                                            promotionResponse.NotApplyPromotions = new List<ProductItem>();
                                        if (thisPromo.OrderBy == "Quantity")
                                        {
                                            decimal calculator = notApplied * thisPromo.LevelOrderQty;
                                            element = notApplyProduct;
                                            element.Quantity = calculator;
                                            element.TotalAmount = element.Quantity * element.Price;
                                            promotionResponse.NotApplyPromotions.Add(element);
                                        }
                                    }

                                    if (applied == 0 && thisPromo is not null && promotionResponse.AppliedPromotions.Count <= 1)
                                    {
                                        promotionResponse.AppliedPromotions = null;
                                    }
                                    else if (promotionResponse.NotApplyPromotions.Count > 0)
                                    {
                                        var find = promotionResponse.AppliedPromotions.Where(f => promotionResponse.NotApplyPromotions.Exists(k => k.ItemCode == f.ProductCode && k.Quantity == f.Quantity))?.ToList();
                                        if (find is not null && find.Any())
                                            promotionResponse.AppliedPromotions = promotionResponse.AppliedPromotions.Where(f => !find.Exists(k => f.ProductCode == k.ProductCode))?.ToList();
                                    }
                                }

                            }
                        }

                    }
                    #endregion
                }
                else
                    return promotionResponse;
            }
            catch (Exception ex)
            {

                throw;
            }
            return promotionResponse;
        }
        private int ResizeBudgetGroup(PromotionAttributeSummary promotionResponse, int slot, RequestEnhanceApplyPromotionByOrderList inp)
        {
            try
            {
                List<BudgetRequest> lst = new List<BudgetRequest>();
                BudgetRequest budget = new BudgetRequest()
                {
                    BudgetCode = promotionResponse.BudgetValueCode ?? promotionResponse.BudgetQtyCode,
                    LevelId = promotionResponse.LevelId,
                    PromotionId = promotionResponse.PromotionId,
                    BudgetQuantity = slot
                };
                lst.Add(budget);
                _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == "ODTpAPI").Select(x => x.Url).FirstOrDefault());
                _client.Authenticator = new JwtAuthenticator($"Rdos {_token}");
                var request = new RestRequest("external_checkbudget/getcustomerbudget", Method.POST, DataFormat.Json);
                request.AddHeader("accept", "*/*");
                request.AddHeader("DistributorCode", inp.DistributorCode);
                request.AddHeader("Content-Type", "application/json");
                var body = @"{" + "\n" +
                @$"  ""SaleOrgCode"": ""{inp.SaleOrgCode}""," + "\n" +
                @$"  ""SicCode"": ""{inp.SicCode}""," + "\n" +
                @$"  ""CustomerCode"": ""{inp.CustomerCode ?? "null"}""," + "\n" +
                @$"  ""ShiptoCode"": ""{inp.ShiptoCode ?? "null"}""," + "\n" +
                @$"  ""RouteZoneCode"": ""{inp.RouteZoneCode ?? "null"}""," + "\n" +
                @$"  ""DsaCode"": ""{inp.DsaCode ?? "null"}""," + "\n" +
                @$"  ""Branch"": {inp.Branch ?? "null"}," + "\n" +
                @$"  ""Region"": ""{inp.Region ?? "null"}""," + "\n" +
                @$"  ""SubRegion"": {inp.SubRegion ?? "null"}," + "\n" +
                @$"  ""Area"": ""{inp.Area ?? "null"}""," + "\n" +
                @$"  ""SubArea"": {inp.SubArea ?? "null"}," + "\n" +
                @$"  ""DistributorCode"": ""{inp.DistributorCode ?? "null"}""," + "\n" +
                @$"  ""budgets"": {JsonConvert.SerializeObject(lst)}" + "\n" +
                @"}";
                request.AddBody(body);
                var response = _client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    CheckBudgetResponse content = JsonConvert.DeserializeObject<CheckBudgetResponse>(response.Content);
                    if (content.Data.Any())
                    {
                        var data = content.Data;
                        foreach (var item in data)
                        {
                            int availableBudget = (int)((item.BudgetRemains is null || item.BudgetRemains == 0) ? int.MaxValue : item.BudgetRemains);
                            int maxAllowed = (int)((item.CustomerBudget is null || item.CustomerBudget == 0) ? int.MaxValue : item.CustomerBudget);
                            int limit = item.BudgetBooked ?? 0;
                            maxAllowed = maxAllowed - limit;
                            int applied = Math.Min(slot, Math.Min(maxAllowed, availableBudget));
                            return applied;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                return 0;
            }
            return 0;
        }
        private List<PromotionGroupResponse> ReCheckBudgetGroup(List<PromotionGroupResponse> promotionResponse, RequestEnhanceApplyPromotionByOrderList inp)
        {
            List<PromotionGroupResponse> rmList = new List<PromotionGroupResponse>();
            try
            {
                if (promotionResponse.Exists(f => f.AppliedPromotions.Exists(k => k.IsApplyBudget ?? false)))
                {
                    foreach (var promotionGroup in promotionResponse)
                    {
                        var promotions = promotionGroup.AppliedPromotions.Where(f => f.IsApplyBudget ?? false)?.ToList();
                        if (promotions is null || promotions.Count == 0)
                            continue;
                        var codes = promotions.Select(f => f.BudgetValueCode ?? f.BudgetQtyCode)?.ToList();
                        List<BudgetRequest> Budgets = new List<BudgetRequest>();
                        foreach (var code in promotions)
                        {
                            BudgetRequest budget = new BudgetRequest()
                            {
                                BudgetCode = code.BudgetValueCode ?? code.BudgetQtyCode,
                                LevelId = code.LevelId,
                                PromotionId = promotionGroup.PromotionId,
                                BudgetQuantity = (int)(code.BudgetQuantity ?? 0)
                            };
                            Budgets.Add(budget);
                        }

                        #region call API
                        _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == "ODTpAPI").Select(x => x.Url).FirstOrDefault());
                        _client.Authenticator = new JwtAuthenticator($"Rdos {_token}");
                        var request = new RestRequest("external_checkbudget/getcustomerbudget", Method.POST, DataFormat.Json);
                        request.AddHeader("accept", "*/*");
                        request.AddHeader("DistributorCode", inp.DistributorCode);
                        request.AddHeader("Content-Type", "application/json");
                        var body = @"{" + "\n" +
                        @$"  ""SaleOrgCode"": ""{inp.SaleOrgCode}""," + "\n" +
                        @$"  ""SicCode"": ""{inp.SicCode}""," + "\n" +
                        @$"  ""CustomerCode"": ""{inp.CustomerCode ?? "null"}""," + "\n" +
                        @$"  ""ShiptoCode"": ""{inp.ShiptoCode ?? "null"}""," + "\n" +
                        @$"  ""RouteZoneCode"": ""{inp.RouteZoneCode ?? "null"}""," + "\n" +
                        @$"  ""DsaCode"": ""{inp.DsaCode ?? "null"}""," + "\n" +
                        @$"  ""Branch"": {inp.Branch ?? "null"}," + "\n" +
                        @$"  ""Region"": ""{inp.Region ?? "null"}""," + "\n" +
                        @$"  ""SubRegion"": {inp.SubRegion ?? "null"}," + "\n" +
                        @$"  ""Area"": ""{inp.Area ?? "null"}""," + "\n" +
                        @$"  ""SubArea"": {inp.SubArea ?? "null"}," + "\n" +
                        @$"  ""DistributorCode"": ""{inp.DistributorCode ?? "null"}""," + "\n" +
                        @$"  ""budgets"": {JsonConvert.SerializeObject(Budgets)}" + "\n" +
                        @"}";
                        request.AddBody(body);
                        var response = _client.Execute(request);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            CheckBudgetResponse content = JsonConvert.DeserializeObject<CheckBudgetResponse>(response.Content);
                            if (content.Data.Any())
                            {
                                var data = content.Data;
                                foreach (var item in data)
                                {
                                    int availableBudget = item.BudgetRemains ?? int.MaxValue;
                                    int maxAllowed = item.CustomerBudget ?? int.MaxValue;

                                    var thisPromo = promotionGroup.AppliedPromotions.Where(f => (f.IsApplyBudget ?? false) && ((f.BudgetQtyCode?.Equals(item.BudgetCode) ?? false) || (f.BudgetValueCode?.Equals(item.BudgetCode) ?? false)))?.FirstOrDefault();

                                    int limit = item.BudgetBooked ?? 0;
                                    maxAllowed = maxAllowed - limit;
                                    int applied = Math.Min((int)(thisPromo.BudgetQuantity ?? 0), Math.Min(maxAllowed, availableBudget));
                                    int notApplied = (int)(thisPromo.BudgetQuantity ?? 0) - applied;



                                    if (applied > 0)
                                    {
                                        int levelQty = (int)(thisPromo.LevelOrderQty == 0 ? thisPromo.LevelOrderAmount : thisPromo.LevelOrderQty);
                                        thisPromo.BudgetQuantity = applied;
                                        thisPromo.Quantity = applied * levelQty;
                                    }
                                    else
                                    {
                                        rmList.Add(promotionGroup);
                                    }

                                }
                            }
                        }

                    }
                    #endregion
                }
                else
                    return promotionResponse;
            }
            catch (Exception ex)
            {

                throw;
            }
            promotionResponse = promotionResponse.Where(f => !rmList.Exists(l => l.PromotionId == f.PromotionId))?.ToList();
            return promotionResponse;
        }
        private List<NotApplyPromotion> ToNotApplyPromotions(List<ProductItem> productList)
        {
            if (productList == null) return new List<NotApplyPromotion>();

            return productList.Select(p => new NotApplyPromotion
            {
                ItemCode = p.ItemCode,
                Quantity = p.Quantity,
                TotalAmount = p.TotalAmount,
                BaseQuantity = p.BaseQuantity,
                BaseUom = p.BaseUom,
                ConversionFactor = p.ConversionFactor,
                ItemGroupCode = p.ItemGroupCode,
                Price = p.Price,
                Uom = p.Uom
            }).ToList();
        }

        private List<PromotionGroupResponse> CalculatorGroupValue(List<ProductItem> products, List<PromotionAttributeSummary> promotions, RequestEnhanceApplyPromotionByOrderList inp, bool isFullSKU = true)
        {
            List<PromotionGroupResponse> promotionGroupResponse = new();

            try
            {

                // Kiểm tra đầu vào
                if (products == null || !products.Any() || promotions == null || !promotions.Any())
                {
                    // Trả về response rỗng với NotApplyPromotions nếu có products
                    if (products != null && products.Any())
                    {
                        promotionGroupResponse.Add(new PromotionGroupResponse
                        {
                            NotApplyPromotions = products.Select(p => new NotApplyPromotion
                            {
                                ItemCode = p.ItemCode,
                                Quantity = p.Quantity,
                                TotalAmount = p.TotalAmount,
                                BaseQuantity = p.BaseQuantity,
                                BaseUom = p.BaseUom,
                                ItemGroupCode = p.ItemGroupCode,
                                Price = p.Price,
                                Uom = p.Uom,
                                ConversionFactor = p.ConversionFactor
                            }).ToList()
                        });
                    }
                    return promotionGroupResponse;
                }
                if (isFullSKU)
                    products = products.Where(f => promotions.Exists(k => k.PackingUomId == f.Uom))?.ToList();

                #region Filter list promotion valid
                int SumProd = products.Sum(f => f.BaseQuantity ?? 0);
                List<PromotionAttributeSummary> ValidPromo = new List<PromotionAttributeSummary>();
                promotions = promotions.Where(f => (f.LevelOrderQty * f.ConversionFactor) <= SumProd).ToList();
                foreach (var promo in promotions)
                {
                    bool isHasRequireMin = promo.RuleType == "RequireMin";
                    // Xử lý requireMin nếu có

                    if (isHasRequireMin)
                    {
                        var productCodes = promo.ProductCodes?.Split(',')
                                  .Select(x => x.Trim())
                                  .Where(x => !string.IsNullOrEmpty(x))
                                  .ToList();

                        if (productCodes == null || !productCodes.Any())
                            continue;

                        var matchedProducts = products.Where(p => productCodes.Contains(p.ItemCode)).ToList();

                        if (!matchedProducts.Any())
                        {
                            continue;
                        }

                        if (matchedProducts.Any(p => (p.BaseQuantity ?? 0) >= promo.TotalRequiredBaseUnit))
                        {
                            ValidPromo.Add(promo);

                        }
                    }
                    else
                    {
                        var filterProducts = products.Where(f => promo.ProductCodes.Contains(f.ItemCode))?.ToList();
                        int total = filterProducts.Sum(f => f.BaseQuantity) ?? 0;
                        if (total >= (promo.LevelOrderQty * promo.ConversionFactor))
                            ValidPromo.Add(promo);
                    }

                }

                var lstPromo = ValidPromo.Select(f => f.PromotionId)?.Distinct()?.ToList();
                if (lstPromo.Any())
                {
                    foreach (var item in lstPromo)
                    {
                        if (ValidPromo.Count(f => f.PromotionId == item) > 1 && !ValidPromo.Exists(f => f.PromotionId == item && f.RuleType == "RequireMin"))
                        {
                            var aavc = ValidPromo.Where(f => f.PromotionId == item)?.ToList();
                            var first = aavc[0];
                            var others = aavc.Where(f => f.AttCode != first.AttCode)?.ToList();
                            string codes = string.Join(",", others.Select(f => f.ProductCodes));
                            first.ProductCodes += codes;
                            ValidPromo.RemoveAll(f => f.PromotionId == item);
                            ValidPromo.Add(first);
                        }
                    }
                }
                #endregion

                #region Calculator Suat
                foreach (var promo in ValidPromo)
                {
                    PromotionGroupResponse response = new PromotionGroupResponse()
                    {
                        PromotionId = promo.PromotionId,
                        PromotionType = promo.PromotionType,
                        PromotionName = promo.PromotionName,
                        AppliedPromotions = new List<ApplyPromotion>()
                    };
                    List<ApplyPromotion> applies = new();
                    if (promo.OrderRule == "AccordingPassLevel")
                    {
                        var matchingProducts = products.Where(f => promo.ProductCodes.Contains(f.ItemCode)).OrderByDescending(k => k.BaseQuantity).ToList();
                        var notMatching = products.Where(f => !promo.ProductCodes.Contains(f.ItemCode))?.ToList();

                        decimal TotalMatching = matchingProducts.Sum(f => f.BaseQuantity) ?? 0;
                        decimal TotalNonMatching = notMatching.Sum(f => f.BaseQuantity) ?? 0;

                        if (promo.RuleType == "RequireMin")
                        {
                            int slots = 0;
                            decimal diff = (promo.LevelOrderQty * promo.ConversionFactor) ?? 0;
                            slots = (int)Math.Min(
                                       Math.Floor((TotalMatching + TotalNonMatching) / diff),
                                       Math.Floor(TotalMatching / (promo.TotalRequiredBaseUnit ?? 0))
                                   );

                            slots = ResizeBudgetGroup(promo, slots, inp);

                            if (slots > 0)
                            {
                                var slotInfo = DetectItemSlot((promo.TotalRequiredBaseUnit ?? 0) * slots, (diff - (promo.TotalRequiredBaseUnit ?? 0)) * slots, matchingProducts, notMatching);

                                if (slotInfo.RequiredUsed.Any())
                                {
                                    ApplyPromotion apply = new ApplyPromotion()
                                    {
                                        BudgetQuantity = slots,
                                        LevelId = promo.LevelId,
                                        LevelDesc = promo.LevelDesc,
                                        BudgetQtyCode = promo.BudgetQtyCode,
                                        BudgetValueCode = promo.BudgetQtyCode,
                                        IsApplyBudget = promo.IsApplyBudget,
                                        LevelOrderQty = promo.LevelOrderQty,
                                        OrderBy = promo.OrderBy,
                                        SalesProducts = new List<SalesProduct>()
                                    };
                                    List<SalesProduct> sales = new List<SalesProduct>();
                                    foreach (var sale in slotInfo.RequiredUsed)
                                    {
                                        var element = matchingProducts.FirstOrDefault(f => f.ItemCode == sale.ProductCode);
                                        SalesProduct salesProduct = new SalesProduct()
                                        {
                                            ProductCode = sale.ProductCode,
                                            MinValue = promo.MinValue,
                                            AttCode = promo.AttCode,
                                            PackingUomId = element.Uom,
                                            BaseUnit = element.BaseUom,
                                            Uom = element.Uom,
                                            ConversionFactor = element.ConversionFactor,
                                            AttName = promo.AttCodeName,
                                            RequiredMinQty = true,
                                            Quantity = null,
                                            BaseQuantity = sale.UsedQuantity,
                                            Price = element.Price,
                                            AttCodeName = promo.AttCodeName
                                        };
                                        sales.Add(salesProduct);
                                    }
                                    if (slotInfo.OtherUsed.Any())
                                    {
                                        foreach (var sale in slotInfo.OtherUsed)
                                        {
                                            var element = matchingProducts.FirstOrDefault(f => f.ItemCode == sale.ProductCode);
                                            if (element is null)
                                                element = notMatching.FirstOrDefault(f => f.ItemCode == sale.ProductCode);
                                            if (sales.Any() && sales.Exists(f => f.ProductCode == sale.ProductCode))
                                            {
                                                var find = sales.FirstOrDefault(f => f.ProductCode == sale.ProductCode);
                                                find.BaseQuantity += sale.UsedQuantity;
                                            }
                                            else
                                            {
                                                SalesProduct salesProduct = new SalesProduct()
                                                {
                                                    ProductCode = sale.ProductCode,
                                                    MinValue = promo.MinValue,
                                                    AttCode = promo.AttCode,
                                                    PackingUomId = element.Uom,
                                                    BaseUnit = element.BaseUom,
                                                    Uom = element.Uom,
                                                    ConversionFactor = element.ConversionFactor,
                                                    AttName = promo.AttCodeName,
                                                    RequiredMinQty = true,
                                                    Quantity = null,
                                                    BaseQuantity = sale.UsedQuantity,
                                                    Price = element.Price,
                                                    AttCodeName = promo.AttCodeName
                                                };
                                                sales.Add(salesProduct);
                                            }
                                        }
                                    }
                                    apply.SalesProducts.AddRange(sales);
                                    applies.Add(apply);
                                }
                                if (slotInfo.Remaining.Any())
                                {
                                    List<NotApplyPromotion> notApplyPromotions = new List<NotApplyPromotion>();
                                    foreach (var remain in slotInfo.Remaining)
                                    {
                                        var element = products.FirstOrDefault(f => f.ItemCode == remain.ProductCode);
                                        var OrgUom = inp.ProductList.FirstOrDefault(f => f.ItemCode == remain.ProductCode);
                                        NotApplyPromotion notApply = new NotApplyPromotion()
                                        {
                                            Uom = OrgUom.Uom,
                                            Price = element.Price,
                                            BaseQuantity = remain.UsedQuantity,
                                            ItemCode = remain.ProductCode,
                                            BaseUom = element.BaseUom,
                                            TotalAmount = remain.UsedQuantity * element.Price,
                                            ConversionFactor = element.ConversionFactor
                                        };
                                        notApplyPromotions.Add(notApply);
                                    }
                                    response.NotApplyPromotions = notApplyPromotions;
                                }
                            }
                        }
                        else
                        {
                            int slots = 0;
                            decimal diff = (promo.LevelOrderQty * promo.ConversionFactor) ?? 0;
                            slots = (int)Math.Floor(TotalMatching / diff);
                            if (slots > 0)
                            {
                                var slotInfo = DetectItemSlot(promo.TotalRequiredBaseUnit ?? 0, diff, matchingProducts, notMatching);

                                ApplyPromotion apply = new ApplyPromotion()
                                {
                                    BudgetQuantity = slots,
                                    LevelId = promo.LevelId,
                                    LevelDesc = promo.LevelDesc,
                                    BudgetQtyCode = promo.BudgetQtyCode,
                                    BudgetValueCode = promo.BudgetQtyCode,
                                    IsApplyBudget = promo.IsApplyBudget,
                                    LevelOrderQty = promo.LevelOrderQty,
                                    SalesProducts = new List<SalesProduct>()
                                };

                                List<SalesProduct> sales = new List<SalesProduct>();
                                foreach (var sale in slotInfo.OtherUsed)
                                {
                                    var element = matchingProducts.FirstOrDefault(f => f.ItemCode == sale.ProductCode);
                                    SalesProduct salesProduct = new SalesProduct()
                                    {
                                        ProductCode = sale.ProductCode,
                                        MinValue = promo.MinValue,
                                        AttCode = promo.AttCode,
                                        PackingUomId = element.Uom,
                                        BaseUnit = element.BaseUom,
                                        Uom = element.Uom,
                                        ConversionFactor = ((element.BaseQuantity ?? 0) / (int)element.Quantity),
                                        AttName = promo.AttCodeName,
                                        RequiredMinQty = true,
                                        BaseQuantity = sale.UsedQuantity,
                                        Price = element.Price
                                    };
                                    sales.Add(salesProduct);
                                }
                                apply.SalesProducts.AddRange(sales);
                                applies.Add(apply);
                                if (slotInfo.Remaining.Any())
                                {
                                    List<NotApplyPromotion> notApplyPromotions = new List<NotApplyPromotion>();
                                    foreach (var remain in slotInfo.Remaining)
                                    {
                                        var element = products.FirstOrDefault(f => f.ItemCode == remain.ProductCode);
                                        NotApplyPromotion notApply = new NotApplyPromotion()
                                        {
                                            Uom = element.Uom,
                                            Price = element.Price,
                                            BaseQuantity = remain.UsedQuantity,
                                            ItemCode = remain.ProductCode,
                                            BaseUom = element.BaseUom,
                                            Quantity = 0,
                                            TotalAmount = remain.UsedQuantity * element.Price,
                                            ConversionFactor = element.ConversionFactor
                                        };
                                        notApplyPromotions.Add(notApply);
                                    }
                                    response.NotApplyPromotions = notApplyPromotions;
                                }
                            }

                        }

                    }

                    response.AppliedPromotions = applies;
                    promotionGroupResponse.Add(response);
                }
                #endregion

            }
            catch (Exception ex)
            {
                if (products != null && products.Any())
                {
                    promotionGroupResponse.Add(new PromotionGroupResponse
                    {
                        NotApplyPromotions = products.Select(p => new NotApplyPromotion
                        {
                            ItemCode = p.ItemCode,
                            Quantity = p.Quantity,
                            TotalAmount = p.TotalAmount,
                            BaseQuantity = p.BaseQuantity,
                            BaseUom = p.BaseUom,
                            ItemGroupCode = p.ItemGroupCode,
                            Price = p.Price,
                            Uom = p.Uom
                        }).ToList()
                    });
                }
            }

            return promotionGroupResponse;
        }
        private List<PromotionGroupResponse> CalculatorGroupValueForNonFullSku(List<ProductItem> products, List<ProductItem> package, List<PromotionAttributeSummary> promotions, RequestEnhanceApplyPromotionByOrderList inp)
        {
            List<PromotionGroupResponse> promotionGroupResponse = new();

            try
            {

                // Kiểm tra đầu vào
                if (products == null || !products.Any() || promotions == null || !promotions.Any())
                {
                    // Trả về response rỗng với NotApplyPromotions nếu có products
                    if (products != null && products.Any())
                    {
                        promotionGroupResponse.Add(new PromotionGroupResponse
                        {
                            NotApplyPromotions = products.Select(p => new NotApplyPromotion
                            {
                                ItemCode = p.ItemCode,
                                Quantity = p.Quantity,
                                TotalAmount = p.TotalAmount,
                                BaseQuantity = p.BaseQuantity,
                                BaseUom = p.BaseUom,
                                ItemGroupCode = p.ItemGroupCode,
                                Price = p.Price,
                                Uom = p.Uom,
                                ConversionFactor = p.ConversionFactor
                            }).ToList()
                        });
                    }
                    return promotionGroupResponse;
                }


                #region Filter list promotion valid
                int SumProd = products.Sum(f => f.BaseQuantity ?? 0);
                List<PromotionAttributeSummary> ValidPromo = new List<PromotionAttributeSummary>();
                promotions = promotions.Where(f => (f.LevelOrderQty * f.ConversionFactor) <= SumProd).ToList();
                foreach (var promo in promotions)
                {
                    bool isHasRequireMin = promo.RuleType == "RequireMin";
                    // Xử lý requireMin nếu có

                    if (isHasRequireMin)
                    {
                        var productCodes = promo.ProductCodes?.Split(',')
                                  .Select(x => x.Trim())
                                  .Where(x => !string.IsNullOrEmpty(x))
                                  .ToList();

                        if (productCodes == null || !productCodes.Any())
                            continue;

                        var matchedProducts = products.Where(p => productCodes.Contains(p.ItemCode)).ToList();

                        if (!matchedProducts.Any())
                        {
                            continue;
                        }

                        ValidPromo.Add(promotions[0]);
                    }
                    else
                    {
                        var filterProducts = products.Where(f => promo.ProductCodes.Contains(f.ItemCode))?.ToList();
                        int total = filterProducts.Sum(f => f.BaseQuantity) ?? 0;
                        if (total >= (promo.LevelOrderQty * promo.ConversionFactor))
                            ValidPromo.Add(promo);
                    }

                }

                var lstPromo = ValidPromo.Select(f => f.PromotionId)?.Distinct()?.ToList();
                if (lstPromo.Any())
                {
                    foreach (var item in lstPromo)
                    {
                        if (ValidPromo.Count(f => f.PromotionId == item) > 1 && !ValidPromo.Exists(f => f.PromotionId == item && f.RuleType == "RequireMin"))
                        {
                            var aavc = ValidPromo.Where(f => f.PromotionId == item)?.ToList();
                            var first = aavc[0];
                            var others = aavc.Where(f => f.AttCode != first.AttCode)?.ToList();
                            string codes = string.Join(",", others.Select(f => f.ProductCodes));
                            first.ProductCodes += codes;
                            ValidPromo.RemoveAll(f => f.PromotionId == item);
                            ValidPromo.Add(first);
                        }
                    }
                }
                #endregion

                #region Calculator Suat
                foreach (var promo in ValidPromo)
                {
                    PromotionGroupResponse response = new PromotionGroupResponse()
                    {
                        PromotionId = promo.PromotionId,
                        PromotionType = promo.PromotionType,
                        PromotionName = promo.PromotionName,
                        AppliedPromotions = new List<ApplyPromotion>()
                    };
                    List<ApplyPromotion> applies = new();
                    if (promo.OrderRule == "AccordingPassLevel")
                    {
                        var notMatching = products.Where(f => !package.Exists(c => c.ItemCode == f.ItemCode))?.ToList();

                        decimal TotalMatching = package.Sum(f => f.BaseQuantity) ?? 0;
                        decimal TotalNonMatching = notMatching.Sum(f => f.BaseQuantity) ?? 0;

                        if (promo.RuleType == "RequireMin")
                        {
                            int slots = 0;
                            decimal diff = (promo.LevelOrderQty * promo.ConversionFactor) ?? 0;
                            slots = (int)Math.Min(
                                       Math.Floor((TotalMatching + TotalNonMatching) / diff),
                                       Math.Floor(TotalMatching / (promo.TotalRequiredBaseUnit ?? 0))
                                   );

                            slots = ResizeBudgetGroup(promo, slots, inp);

                            if (slots > 0)
                            {
                                var slotInfo = DetectItemSlot((promo.TotalRequiredBaseUnit ?? 0) * slots, (diff - (promo.TotalRequiredBaseUnit ?? 0)) * slots, package, notMatching);


                                if (slotInfo.RequiredUsed.Any())
                                {
                                    ApplyPromotion apply = new ApplyPromotion()
                                    {
                                        BudgetQuantity = slots,
                                        LevelId = promo.LevelId,
                                        LevelDesc = promo.LevelDesc,
                                        BudgetQtyCode = promo.BudgetQtyCode,
                                        BudgetValueCode = promo.BudgetQtyCode,
                                        IsApplyBudget = promo.IsApplyBudget,
                                        LevelOrderQty = promo.LevelOrderQty,
                                        OrderBy = promo.OrderBy,
                                        SalesProducts = new List<SalesProduct>()
                                    };
                                    List<SalesProduct> sales = new List<SalesProduct>();
                                    foreach (var sale in slotInfo.RequiredUsed)
                                    {
                                        var element = package.FirstOrDefault(f => f.ItemCode == sale.ProductCode);
                                        SalesProduct salesProduct = new SalesProduct()
                                        {
                                            ProductCode = sale.ProductCode,
                                            MinValue = promo.MinValue,
                                            AttCode = promo.AttCode,
                                            PackingUomId = element.Uom,
                                            BaseUnit = element.BaseUom,
                                            Uom = element.Uom,
                                            ConversionFactor = element.ConversionFactor,
                                            AttName = promo.AttCodeName,
                                            RequiredMinQty = true,
                                            Quantity = null,
                                            BaseQuantity = sale.UsedQuantity,
                                            Price = element.Price,
                                            AttCodeName = promo.AttCodeName
                                        };
                                        sales.Add(salesProduct);
                                    }
                                    if (slotInfo.OtherUsed.Any())
                                    {
                                        foreach (var sale in slotInfo.OtherUsed)
                                        {
                                            var element = package.FirstOrDefault(f => f.ItemCode == sale.ProductCode);
                                            if (element is null)
                                                element = notMatching.FirstOrDefault(f => f.ItemCode == sale.ProductCode);
                                            if (sales.Any() && sales.Exists(f => f.ProductCode == sale.ProductCode))
                                            {
                                                var find = sales.FirstOrDefault(f => f.ProductCode == sale.ProductCode);
                                                find.BaseQuantity += sale.UsedQuantity;
                                            }
                                            else
                                            {
                                                SalesProduct salesProduct = new SalesProduct()
                                                {
                                                    ProductCode = sale.ProductCode,
                                                    MinValue = promo.MinValue,
                                                    AttCode = promo.AttCode,
                                                    PackingUomId = element.Uom,
                                                    BaseUnit = element.BaseUom,
                                                    Uom = element.Uom,
                                                    ConversionFactor = element.ConversionFactor,
                                                    AttName = promo.AttCodeName,
                                                    RequiredMinQty = true,
                                                    Quantity = null,
                                                    BaseQuantity = sale.UsedQuantity,
                                                    Price = element.Price,
                                                    AttCodeName = promo.AttCodeName
                                                };
                                                sales.Add(salesProduct);
                                            }
                                        }
                                    }
                                    apply.SalesProducts.AddRange(sales);
                                    applies.Add(apply);
                                }
                                if (slotInfo.Remaining.Any())
                                {
                                    List<NotApplyPromotion> notApplyPromotions = new List<NotApplyPromotion>();
                                    foreach (var remain in slotInfo.Remaining)
                                    {
                                        var element = products.FirstOrDefault(f => f.ItemCode == remain.ProductCode);
                                        NotApplyPromotion notApply = new NotApplyPromotion()
                                        {
                                            Uom = element.Uom,
                                            Price = element.Price,
                                            BaseQuantity = remain.UsedQuantity,
                                            ItemCode = remain.ProductCode,
                                            BaseUom = element.BaseUom,
                                            TotalAmount = remain.UsedQuantity * element.Price,
                                            ConversionFactor = element.ConversionFactor
                                        };
                                        notApplyPromotions.Add(notApply);
                                    }
                                    response.NotApplyPromotions = notApplyPromotions;
                                }
                            }
                        }
                        else
                        {
                            int slots = 0;
                            decimal diff = (promo.LevelOrderQty * promo.ConversionFactor) ?? 0;
                            slots = (int)Math.Floor(TotalMatching / diff);
                            if (slots > 0)
                            {
                                var slotInfo = DetectItemSlot(promo.TotalRequiredBaseUnit ?? 0, diff, package, notMatching);

                                ApplyPromotion apply = new ApplyPromotion()
                                {
                                    BudgetQuantity = slots,
                                    LevelId = promo.LevelId,
                                    LevelDesc = promo.LevelDesc,
                                    BudgetQtyCode = promo.BudgetQtyCode,
                                    BudgetValueCode = promo.BudgetQtyCode,
                                    IsApplyBudget = promo.IsApplyBudget,
                                    LevelOrderQty = promo.LevelOrderQty,
                                    SalesProducts = new List<SalesProduct>()
                                };

                                List<SalesProduct> sales = new List<SalesProduct>();
                                foreach (var sale in slotInfo.OtherUsed)
                                {
                                    var element = package.FirstOrDefault(f => f.ItemCode == sale.ProductCode);
                                    SalesProduct salesProduct = new SalesProduct()
                                    {
                                        ProductCode = sale.ProductCode,
                                        MinValue = promo.MinValue,
                                        AttCode = promo.AttCode,
                                        PackingUomId = element.Uom,
                                        BaseUnit = element.BaseUom,
                                        Uom = element.Uom,
                                        ConversionFactor = ((element.BaseQuantity ?? 0) / (int)element.Quantity),
                                        AttName = promo.AttCodeName,
                                        RequiredMinQty = true,
                                        BaseQuantity = sale.UsedQuantity,
                                        Price = element.Price
                                    };
                                    sales.Add(salesProduct);
                                }
                                apply.SalesProducts.AddRange(sales);
                                applies.Add(apply);
                                if (slotInfo.Remaining.Any())
                                {
                                    List<NotApplyPromotion> notApplyPromotions = new List<NotApplyPromotion>();
                                    foreach (var remain in slotInfo.Remaining)
                                    {
                                        var element = products.FirstOrDefault(f => f.ItemCode == remain.ProductCode);
                                        NotApplyPromotion notApply = new NotApplyPromotion()
                                        {
                                            Uom = element.Uom,
                                            Price = element.Price,
                                            BaseQuantity = remain.UsedQuantity,
                                            ItemCode = remain.ProductCode,
                                            BaseUom = element.BaseUom,
                                            Quantity = 0,
                                            TotalAmount = remain.UsedQuantity * element.Price,
                                            ConversionFactor = element.ConversionFactor
                                        };
                                        notApplyPromotions.Add(notApply);
                                    }
                                    response.NotApplyPromotions = notApplyPromotions;
                                }
                            }

                        }

                    }

                    response.AppliedPromotions = applies;
                    promotionGroupResponse.Add(response);
                }
                #endregion

            }
            catch (Exception ex)
            {
                if (products != null && products.Any())
                {
                    promotionGroupResponse.Add(new PromotionGroupResponse
                    {
                        NotApplyPromotions = products.Select(p => new NotApplyPromotion
                        {
                            ItemCode = p.ItemCode,
                            Quantity = p.Quantity,
                            TotalAmount = p.TotalAmount,
                            BaseQuantity = p.BaseQuantity,
                            BaseUom = p.BaseUom,
                            ItemGroupCode = p.ItemGroupCode,
                            Price = p.Price,
                            Uom = p.Uom
                        }).ToList()
                    });
                }
            }

            return promotionGroupResponse;
        }
        private PromotionSlotAllocation DetectItemSlot(decimal requiredLeft, decimal otherLeft, List<ProductItem> requiredProducts, List<ProductItem> otherProducts)
        {
            var allocation = new PromotionSlotAllocation
            {
                SlotIndex = 1,
                RequiredUsed = new List<ProductUsed>(),
                OtherUsed = new List<ProductUsed>(),
                Remaining = new List<ProductUsed>()
            };



            var remainingFromRequired = new List<(string ProductCode, decimal Qty, bool IsRequire)>();

            // 🔁 Lấy đủ từ nhóm yêu cầu (ưu tiên xả hết hàng yêu cầu)
            foreach (var r in requiredProducts.OrderByDescending(x => x.BaseQuantity ?? 0))
            {
                if (requiredLeft <= 0)
                {
                    remainingFromRequired.Add((r.ItemCode, r.BaseQuantity ?? 0, true));
                    continue;
                }

                var qty = r.BaseQuantity ?? 0;
                var use = Math.Min(requiredLeft, qty);

                allocation.RequiredUsed.Add(new ProductUsed
                {
                    ProductCode = r.ItemCode,
                    UsedQuantity = use,
                    Role = "Required"
                });

                var leftover = qty - use;
                remainingFromRequired.Add((r.ItemCode, leftover, true));
                requiredLeft -= use;
            }

            // 🔁 Dùng phần dư yêu cầu để góp vào nhóm Other

            if (otherLeft > 0)
            {
                if (otherProducts.Any())
                {
                    otherProducts = otherProducts.OrderByDescending(f => f.Quantity)?.ToList();
                    foreach (var other in otherProducts)
                    {
                        if (!remainingFromRequired.Exists(f => other.ItemCode == f.ProductCode && other.Quantity == f.Qty))
                            remainingFromRequired.Add((other.ItemCode, other.BaseQuantity ?? 0, false));
                    }
                }

                var sortPriority = remainingFromRequired.OrderBy(f => f.IsRequire)?.ToList();
                foreach (var extra in sortPriority)
                {
                    if (otherLeft <= 0)
                        break;
                    if (extra.Qty <= 0)
                        continue;
                    var use = Math.Min(otherLeft, extra.Qty);
                    var exist = remainingFromRequired.FirstOrDefault(e => e.ProductCode == extra.ProductCode && e.Qty > 0);
                    allocation.OtherUsed.Add(new ProductUsed
                    {
                        ProductCode = extra.ProductCode,
                        UsedQuantity = use,
                        Role = "Other (From Required Leftover)"
                    });
                    otherLeft -= use;
                    exist.Qty = exist.Qty - use;
                    var index = remainingFromRequired.FindIndex(e => e.ProductCode == extra.ProductCode && e.Qty > 0);
                    remainingFromRequired[index] = exist;
                }

            }


            // 🔁 Nếu chưa đủ, lấy thêm từ sản phẩm không yêu cầu
            foreach (var o in otherProducts.OrderByDescending(x => x.BaseQuantity ?? 0))
            {
                if (otherLeft <= 0) break;

                var qty = o.BaseQuantity ?? 0;
                var use = Math.Min(otherLeft, qty);
                allocation.OtherUsed.Add(new ProductUsed
                {
                    ProductCode = o.ItemCode,
                    UsedQuantity = use,
                    Role = "Other"
                });
                otherLeft -= use;
            }

            // 🔁 Nếu chưa đủ yêu cầu/other thì không áp dụng
            if (requiredLeft > 0 || otherLeft > 0)
                return null;

            // 🧮 Tính phần dư còn lại chưa dùng
            foreach (var r in remainingFromRequired.Where(f => f.IsRequire))
            {
                if (r.Qty > 0)
                {
                    allocation.Remaining.Add(new ProductUsed
                    {
                        ProductCode = r.ProductCode,
                        UsedQuantity = r.Qty,
                        Role = "Remaining (From Required)"
                    });
                }
            }

            foreach (var o in remainingFromRequired.Where(f => !f.IsRequire))
            {

                if (o.Qty > 0)
                {
                    allocation.Remaining.Add(new ProductUsed
                    {
                        ProductCode = o.ProductCode,
                        UsedQuantity = o.Qty,
                        Role = "Remaining (From Other)"
                    });
                }
            }

            return allocation;
        }

        private static T DeepCopy<T>(T obj)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(obj);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
        #endregion
    }
}
