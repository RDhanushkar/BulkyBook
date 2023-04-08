using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IShoppingCardRepository : IRepository<ShoppingCard>
    {
       int IncrementCount(ShoppingCard shoppingCard, int Count);
       int DecrementCount(ShoppingCard shoppingCard, int Count);
    }
}
