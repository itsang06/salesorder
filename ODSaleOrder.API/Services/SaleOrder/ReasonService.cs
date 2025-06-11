using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sys.Common.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Sys.Common.Helper;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using ODSaleOrder.API.Services.Base;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using static SysAdmin.API.Constants.Constant;
using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;

namespace ODSaleOrder.API.Services.SaleOrder
{
    public class ReasonService : IReasonService
    {
        private readonly ILogger<ReasonService> _logger;
        private readonly IMapper _mapper;
        private readonly IDynamicBaseRepository<SO_Reason> _reasonRepository;
        private readonly IDynamicBaseRepository<SO_OrderInformations> _orderInformationRepository;

        public ReasonService(
            ILogger<ReasonService> logger,
            IMapper mapper,
            RDOSContext db
        )
        {
            _logger = logger;
            _mapper = mapper;
            _orderInformationRepository = new DynamicBaseRepository<SO_OrderInformations>(db);
            _reasonRepository = new DynamicBaseRepository<SO_Reason>(db);
        }

        public async Task<ResultModelWithObject<ListReasonModel>> SearchReason(EcoparamsWithGenericFilter parameters)
        {
            try
            {
                var query = _reasonRepository.GetAllQueryable().Where(x => !x.IsDeleted).AsNoTracking();

                IEnumerable<SO_Reason> res;
                //if has filter expression 
                if (parameters.Filter != null && parameters.Filter.Trim() != string.Empty && parameters.Filter.Trim() != "NA_EMPTY")
                {
                    var optionsAssembly = ScriptOptions.Default.AddReferences(typeof(SO_Reason).Assembly);
                    var filterExpressionTemp = CSharpScript.EvaluateAsync<Func<SO_Reason, bool>>(($"s=> {parameters.Filter}"), optionsAssembly);
                    Func<SO_Reason, bool> filterExpression = filterExpressionTemp.Result;
                    var checkCondition = query.Where(filterExpression);
                    res = checkCondition.ToList();
                }
                else
                {
                    res = query.ToList();
                }

                if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
                {
                    res = res.Where(x => (!string.IsNullOrWhiteSpace(x.ReasonCode) && x.ReasonCode.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim())) ||
                    (!string.IsNullOrWhiteSpace(x.Description) && x.Description.ToLower().Trim().Contains(parameters.SearchValue.ToLower().Trim()))).ToList();
                }
                var reasonList = res.OrderBy(x => x.ReasonCode).ToList();

                //if has filter expression
                if (parameters.Filters != null && parameters.Filters.Count > 0)
                {
                    foreach (var filter in parameters.Filters)
                    {
                        var getter = typeof(SO_Reason).GetProperty(filter.Property);
                        reasonList = reasonList.Where(x => filter.Values.Any(a => a == "" || a == null) ?
                                string.IsNullOrEmpty(getter.GetValue(x, null).EmptyIfNull().ToString()) || filter.Values.Contains(getter.GetValue(x, null).ToString().ToLower().Trim()) :
                                !string.IsNullOrEmpty(getter.GetValue(x, null).EmptyIfNull().ToString()) && filter.Values.Contains(getter.GetValue(x, null).ToString().ToLower().Trim())).ToList();
                    }
                }

                if (parameters.IsDropdown)
                {
                    var page1 = PagedList<SO_Reason>.ToPagedList(reasonList, 0, reasonList.Count);

                    var reponse = new ListReasonModel { Items = reasonList };
                    return new ResultModelWithObject<ListReasonModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = reponse
                    };
                }

                var poTempPagged = PagedList<SO_Reason>.ToPagedList(reasonList, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                var repsonse = new ListReasonModel { Items = poTempPagged, MetaData = poTempPagged.MetaData };

                //return metadata
                return new ResultModelWithObject<ListReasonModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = repsonse
                };

            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<ListReasonModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> CheckInUsed(string ReasonCode)
        {
            try
            {
                bool result = await _orderInformationRepository.GetAllQueryable().AsNoTracking().AnyAsync(x => x.ReasonCode != null && x.ReasonCode == ReasonCode);
                _reasonRepository.Dispose();
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = result
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

        public async Task<BaseResultModel> BulkUpsertReason(List<SO_Reason> models, string username)
        {
            try
            {   //Generate RefNumber
                var prefix = ReasonCodePrefix;
                var orderRefNumberIndb = await _reasonRepository.GetAllQueryable().Where(x => x.ReasonCode.Contains(prefix)).AsNoTracking().Select(x => x.ReasonCode).OrderByDescending(x => x).FirstOrDefaultAsync();

                foreach (var model in models)
                {
                    if (model.Id == null || model.Id == Guid.Empty)
                    {
                        var generatedNumber = StringsHelper.GennerateReasonCode(prefix, orderRefNumberIndb != null ? orderRefNumberIndb : null);
                        model.ReasonCode = generatedNumber;
                        model.CreatedBy = username;
                        model.CreatedDate = DateTime.Now;
                        _reasonRepository.Add(model);
                        orderRefNumberIndb = generatedNumber;
                    }
                    else
                    {
                        var reasonInDB = await _reasonRepository.GetAllQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id);
                        if (reasonInDB == null)
                        {
                            return new BaseResultModel
                            {
                                Code = 400,
                                IsSuccess = false,
                                Message = "reasonCode notFound",
                            };
                        }
                        if (model.IsDeleted && await _orderInformationRepository.GetAllQueryable().AnyAsync(x => x.ReasonCode != null && x.ReasonCode == reasonInDB.ReasonCode))
                        {
                            return new BaseResultModel
                            {
                                Code = 400,
                                IsSuccess = false,
                                Message = $"Reason {model.ReasonCode} is Inused in another Feature",
                            };
                        }

                        model.CreatedDate = reasonInDB.CreatedDate;
                        model.CreatedBy = reasonInDB.CreatedBy;
                        model.UpdatedBy = username;
                        model.UpdatedDate = DateTime.Now;
                        _reasonRepository.UpdateUnSaved(model);
                    }
                }
                _reasonRepository.Save();
                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = models
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
    }
}
