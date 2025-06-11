using ODSaleOrder.API.Models.SyncHistory;
using System.Threading.Tasks;
using System;
using ODSaleOrder.API.Services.Base;
using AutoMapper;
using Sys.Common.Models;
using static SysAdmin.API.Constants.Constant;
using ODSaleOrder.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;

namespace ODSaleOrder.API.Services.SaleOrder
{
    public class SyncCommonService : ISyncCommonService
    {
        private readonly IDynamicBaseRepository<StagingSyncDataHistory> _logRepository;
        private readonly IDynamicBaseRepository<SoorderRecallReq> _soRecallReqRepository;
        private readonly RDOSContext _db;
        public SyncCommonService(
            RDOSContext db
        )
        {
            _db = db;
            _logRepository = new DynamicBaseRepository<StagingSyncDataHistory>(_db);
            _soRecallReqRepository = new DynamicBaseRepository<SoorderRecallReq>(_db);
        }
        public async Task<BaseResultModel> SaveLogSync(StagingSyncDataHistoryModel logNew)
        {
            try
            {
                logNew.Id = Guid.NewGuid();
                if (logNew.DataType == DataType.SORECALLREQ_SYNC && logNew.RollbackId != Guid.Empty)
                {
                    SoorderRecallReq itemInDb = await _soRecallReqRepository.GetAllQueryable().FirstOrDefaultAsync(x => x.Id == logNew.RollbackId);

                    if (itemInDb != null)
                    {
                        if (logNew.InsertStatus == HistoryStatus.FAILED)
                        {
                            itemInDb.UpdatedDate = DateTime.Now;
                            itemInDb.UpdatedBy = logNew.UpdatedBy;
                            itemInDb.IsSync = false;
                            itemInDb.Status = SORECALLSTATUS.NEW;
                            itemInDb.OwnerCode = null;
                            _soRecallReqRepository.Update(itemInDb);
                        }

                        if (logNew.InsertStatus == HistoryStatus.SUCCESS)
                        {
                            itemInDb.UpdatedDate = DateTime.Now;
                            itemInDb.UpdatedBy = logNew.UpdatedBy;
                            itemInDb.IsSync = true;
                            itemInDb.OwnerCode = null;
                            _soRecallReqRepository.Update(itemInDb);
                        }
                    }
                }

                if (logNew.InsertStatus == HistoryStatus.FAILED)
                {
                    _logRepository.Add(logNew);
                    _logRepository.Save();
                }
                    
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200
                };
            }
            catch (Exception ex)
            {
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }
    }
}
