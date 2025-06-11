using Sys.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ODSaleOrder.API.Services.Base
{
    public interface IBaseRepository<T> where T : class
    {
        IEnumerable<T> GetAll();

        IEnumerable<T> GetByFunction(string sqlQuery);

        T SingleOrDefault(Expression<Func<T, bool>> filter, params Expression<Func<T, object>>[] includes);

        T FirstOrDefault(Expression<Func<T, bool>> filter = null, params Expression<Func<T, object>>[] includes);

        IEnumerable<T> Find(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy, params Expression<Func<T, object>>[] includes);

        IEnumerable<T> Find(Expression<Func<T, bool>> filter);
        T Insert(T entity);

        T Update(T entity);

        T Delete(object id);

        T GetById(object id);

        List<T> Search(SearchModel _search, out int Total);

        List<T> Search(SearchModel _search, string dateColumnName, out int Total);

        bool Contains(Expression<Func<T, bool>> precidate);
        public void Add(T entity);
        public void UpdateUnSaved(T entity);
        public void Save();
        public IQueryable<T> GetAllQueryable(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = null);

        public void Remove(T entity);
        public void RemoveRange(IEnumerable<T> entity);
        public void AddRange(IEnumerable<T> entities);
        public void UpdateRange(IEnumerable<T> entities);

        bool InsertMany(List<T> entity);
        bool UpdateMany(List<T> entity);
        public void Dispose();
        public void DetachEntity();
    }
}
