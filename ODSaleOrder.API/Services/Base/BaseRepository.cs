using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using Sys.Common.Helper;
using Sys.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ODSaleOrder.API.Services.Base
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity>
          where TEntity : class
    {
        protected readonly RDOSContext _dataContext;
        internal DbSet<TEntity> dbSet;

        public BaseRepository(RDOSContext dataContext)
        {
            _dataContext = dataContext;
            this.dbSet = _dataContext.Set<TEntity>();

        }

        public bool Contains(Expression<Func<TEntity, bool>> precidate)
        {
            return _dataContext.Set<TEntity>().Count(precidate) > 0;
        }

        public virtual TEntity Delete(object id)
        {
            var entity = _dataContext.Set<TEntity>().Find(id);
            if (entity == null)
                return entity;
            _dataContext.Set<TEntity>().Remove(entity);
            _dataContext.SaveChanges();
            return entity;
        }

        public virtual TEntity GetById(object id)
        {
            var entity = _dataContext.Set<TEntity>().Find(id);
            return entity;
        }

        public virtual IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy, params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dataContext.Set<TEntity>();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return query;
        }
        public virtual IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> filter)
        {
            IQueryable<TEntity> query = _dataContext.Set<TEntity>();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return query;
        }
        public virtual TEntity FirstOrDefault(Expression<Func<TEntity, bool>> filter = null, params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dataContext.Set<TEntity>();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return query.FirstOrDefault();
        }

        public virtual IEnumerable<TEntity> GetAll()
        {
            var query = this._dataContext.Set<TEntity>().AsNoTracking();
            return query;
        }

        public virtual IEnumerable<TEntity> GetByFunction(string sqlQuery)
        {
            var query = this._dataContext.Set<TEntity>().FromSqlRaw(sqlQuery);
            return query;
        }

        public virtual List<TEntity> Search(SearchModel _search, out int Total)
        {
            Total = 0;
            _search.PageSize = _search.PageSize == 0 || _search.PageSize == null ? 10 : _search.PageSize;
            _search.PageIndex = _search.PageIndex == 0 || _search.PageIndex == null ? 1 : _search.PageIndex;
            var items = this._dataContext.Set<TEntity>().ToList();
            if (_search.CompanyId > 0)
                items = items.Where(x => int.Parse(typeof(TEntity).GetProperties().Where(x => x.Name.Equals("CompanyId")).FirstOrDefault().GetValue(x).ToString()) == _search.CompanyId).ToList();

            items = items.Where(x => typeof(TEntity).GetProperties().Where(x => x.Name.Equals("DeletedDate")).FirstOrDefault().GetValue(x)?.ToString() == null).ToList();

            #region Search date

            if (_search.FromDate != null || _search.ToDate != null)
            {
                if (_search.FromDate != null)
                    items = items.Where(x => DateTime.Parse(typeof(TEntity).GetProperties().Where(x => x.Name.Equals("CreatedDate")).FirstOrDefault().GetValue(x).ToString()) >= _search.FromDate).ToList();

                if (_search.ToDate != null)
                    items = items.Where(x => DateTime.Parse(typeof(TEntity).GetProperties().Where(x => x.Name.Equals("CreatedDate")).FirstOrDefault().GetValue(x).ToString()) <= _search.ToDate).ToList();
            }

            #endregion Search date

            if (items != null)
                Total = items.Count;
            var query = items.Skip((_search.PageIndex - 1) * _search.PageSize ?? 0).Take(_search.PageSize ?? 10).ToList();
            return query;
        }

        public virtual List<TEntity> Search(SearchModel _search, string dateColumnName, out int Total)
        {
            Total = 0;
            _search.PageSize = _search.PageSize == 0 || _search.PageSize == null ? 10 : _search.PageSize;
            _search.PageIndex = _search.PageIndex == 0 || _search.PageIndex == null ? 1 : _search.PageIndex;
            var items = this._dataContext.Set<TEntity>().ToList();
            if (_search.CompanyId > 0)
                items = items.Where(x => int.Parse(typeof(TEntity).GetProperties().Where(x => x.Name.Equals("CompanyId")).FirstOrDefault().GetValue(x).ToString()) == _search.CompanyId).ToList();

            items = items.Where(x => typeof(TEntity).GetProperties().Where(x => x.Name.Equals("DeletedDate")).FirstOrDefault().GetValue(x)?.ToString() == null && typeof(TEntity).GetProperties().Where(x => x.Name.Equals(dateColumnName)).FirstOrDefault().GetValue(x)?.ToString() != null).ToList();

            #region Search condition

            if (_search.FromDate != null || _search.ToDate != null)
            {
                if (_search.FromDate != null)
                    items = items.Where(x => DateTime.Parse(typeof(TEntity).GetProperties().Where(x => x.Name.Equals(dateColumnName))?.FirstOrDefault()?.GetValue(x)?.ToString()) >= _search.FromDate)?.ToList();

                if (_search.ToDate != null)
                    items = items.Where(x => DateTime.Parse(typeof(TEntity).GetProperties().Where(x => x.Name.Equals(dateColumnName))?.FirstOrDefault()?.GetValue(x)?.ToString()) <= _search.ToDate)?.ToList();
            }
            if (_search.SearchACondition.IsNotNullOrWriteSpace())
            {
                items = items.Where(x => (bool)(typeof(TEntity).GetProperties().Where(x => x.Name.Equals("EventName")).FirstOrDefault().GetValue(x)?.ToString().ToLower().Contains(_search.SearchACondition.ToLower()))).ToList();
            }

            #endregion Search condition

            if (items != null)
                Total = items.Count;
            var query = items.OrderByDescending(x => int.Parse(typeof(TEntity).GetProperties().Where(x => x.Name.Equals("Id")).FirstOrDefault().GetValue(x).ToString())).Skip((_search.PageIndex - 1) * _search.PageSize ?? 0).Take(_search.PageSize ?? 10).ToList();
            return query;
        }

        public virtual TEntity Insert(TEntity entity)
        {
            TEntity thisEntity = _dataContext.Set<TEntity>().Add(entity).Entity;
            if (_dataContext.SaveChanges() > 0)
            {
                return thisEntity;
            }

            return null;
        }

        public virtual TEntity SingleOrDefault(Expression<Func<TEntity, bool>> filter, params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dataContext.Set<TEntity>();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return query.SingleOrDefault();
        }

        public virtual TEntity Update(TEntity entity)
        {
            var local = _dataContext.Set<TEntity>().Local.FirstOrDefault();
            if (local != null)
                _dataContext.Entry(local).State = EntityState.Detached;

            _dataContext.Entry(entity).State = EntityState.Modified;
            if (_dataContext.SaveChanges() > 0)
            {
                return entity;
            }
            return null;
        }
        public void Add(TEntity entity)
        {
            dbSet.Add(entity);
        }

        public void UpdateUnSaved(TEntity entity)
        {
            dbSet.Update(entity);
            _dataContext.Entry(entity).State = EntityState.Modified;
        }

        public void Save()
        {
            try
            {

                _dataContext.SaveChanges();
            }
            catch (NpgsqlException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                if (!(ex.InnerException?.Message ?? ex.Message).Contains("Cannot insert duplicate key"))
                    throw;
            }
        }

        public IQueryable<TEntity> GetAllQueryable(Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, string includeProperties = null)
        {
            IQueryable<TEntity> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            if (includeProperties != null)
            {
                foreach (var includeProp in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }
            if (orderBy != null)
            {
                return orderBy(query);
            }
            return query;
        }

        public void Remove(TEntity entity)
        {
            dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<TEntity> entity)
        {
            dbSet.RemoveRange(entity);
        }

        public void AddRange(IEnumerable<TEntity> entities)
        {
            dbSet.AddRange(entities);
            //foreach (var entity in entities)
            //{
            //    _db.Entry(entity).State = EntityState.Added;
            //}
        }

        public void UpdateRange(IEnumerable<TEntity> entities)
        {
            dbSet.UpdateRange(entities);
            //foreach (var entity in entities)
            //{
            //    _db.Entry(entity).State = EntityState.Added;
            //}
        }

        public virtual bool InsertMany(List<TEntity> entity)
        {
            using (var dbContextTransaction = _dataContext.Database.BeginTransaction())
            {
                try
                {
                    _dataContext.Set<TEntity>().AddRange(entity);
                    _dataContext.SaveChanges();
                    // Nếu mọi thứ OK, commit 
                    dbContextTransaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi, rollback giao dịch
                    dbContextTransaction.Rollback();
                    return false;
                }
            }
        }

        public virtual bool UpdateMany(List<TEntity> entity)
        {
            using (var dbContextTransaction = _dataContext.Database.BeginTransaction())
            {
                try
                {
                    _dataContext.Set<TEntity>().UpdateRange(entity);
                    _dataContext.SaveChanges();
                    // Nếu mọi thứ OK, commit 
                    dbContextTransaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi, rollback giao dịch
                    dbContextTransaction.Rollback();
                    return false;
                }
            }
        }

        public void DetachEntity()
        {
            _dataContext.ChangeTracker.Clear();
        }

        private bool _isDisposed;
        public void Dispose()
        {
            if (_isDisposed) return;
            _dataContext.Dispose();
            _isDisposed = true;
        }
    }
}
