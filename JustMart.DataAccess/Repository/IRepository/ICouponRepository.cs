using JustMart.Models;

namespace JustMart.DataAccess.Repository.IRepository
{
    public interface ICouponRepository : IRepository<Coupon>
    {
        void Update(Coupon obj);
        Coupon? GetByCode(string code);
    }
}
