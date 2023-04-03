using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository 
    {
        private ApplicationDbContext _db;
        
        public CompanyRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
        //unit of work will contain save class
        //public void Save()
        //{
        //    //throw new NotImplementedException();
        //    _db.SaveChanges();
        //}

        public void Update(Company obj)
        {
            //throw new NotImplementedException();
            _db.Companys.Update(obj);
        }
    }
}
