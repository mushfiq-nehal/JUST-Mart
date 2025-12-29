using JustMart.DataAccess.Repository.IRepository;
using JustMart.DataAccess.Data;
using JustMart.Models;

namespace JustMart.DataAccess.Repository
{
    public class CouponRepository : Repository<Coupon>, ICouponRepository
    {
        private ApplicationDbContext _db;
        public CouponRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Coupon obj)
        {
            _db.Coupons.Update(obj);
        }

        public Coupon? GetByCode(string code)
        {
            return _db.Coupons.FirstOrDefault(c => c.Code.ToUpper() == code.ToUpper() && c.IsActive);
        }
    }
}
