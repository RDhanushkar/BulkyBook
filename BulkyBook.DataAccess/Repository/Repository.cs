using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        //internal DbSet<T> DbSet;
        //internal DbSet<T> dbSet;
        internal Microsoft.EntityFrameworkCore.DbSet<T> dbSet;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
            this.dbSet = _db.Set<T>();
        }

        void IRepository<T>.Add(T entity)
        {
            //throw new NotImplementedException();
            dbSet.Add(entity);
        }

        IEnumerable<T> IRepository<T>.GetAll()
        {
            //throw new NotImplementedException();
            IQueryable<T> query = dbSet;
            return query.ToList();
        }

        T IRepository<T>.GetFirstOrDefault(Expression<Func<T, bool>> filter)
        {
            //throw new NotImplementedException();
            IQueryable<T> query = dbSet;
            query.Where(filter);
            return query.FirstOrDefault();
        }

        void IRepository<T>.Remove(T entity)
        {
            dbSet.Remove(entity);
        }

        void IRepository<T>.RemoveRange(IEnumerable<T> entity)
        {
            //IQueryable<T> query = dbSet;
            dbSet.RemoveRange(entity);
        }
    }
}
