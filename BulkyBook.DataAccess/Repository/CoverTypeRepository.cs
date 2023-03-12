using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class CoverTypeRepository : Repository<CoverType>, ICoverTypeRepository 
    {
        private ApplicationDbContext _db;
        
        public CoverTypeRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
        //unit of work will contain save class
        //public void Save()
        //{
        //    //throw new NotImplementedException();
        //    _db.SaveChanges();
        //}

        public void Update(CoverType obj)
        {
            //throw new NotImplementedException();
            _db.CoverTypes.Update(obj);
        }
    }
}
