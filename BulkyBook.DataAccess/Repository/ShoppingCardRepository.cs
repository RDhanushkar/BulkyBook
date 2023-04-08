using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class ShoppingCardRepository : Repository<ShoppingCard>, IShoppingCardRepository
    {
        private ApplicationDbContext _db;
        
        public ShoppingCardRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public int DecrementCount(ShoppingCard shoppingCard, int Count)
        {
            shoppingCard.Count -= Count;
            return shoppingCard.Count;
        }

        public int IncrementCount(ShoppingCard shoppingCard, int Count)
        {
            shoppingCard.Count += Count;
            return shoppingCard.Count;
        }
    }
}
