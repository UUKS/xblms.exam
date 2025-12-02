using Datory;
using System.Collections.Generic;
using System.Threading.Tasks;
using XBLMS.Models;
using XBLMS.Utils;

namespace XBLMS.Core.Repositories
{
    public partial class PointShopUserRepository
    {
        public async Task<(int total, List<PointShopUser> list)> Analysis_GetListAsync(int userId, string dateFrom, string dateTo, int pageIndex, int pageSize)
        {
            var query = Q.Where(nameof(PointShopUser.UserId), userId);

            if (!string.IsNullOrEmpty(dateFrom))
            {
                query.Where(nameof(PointShopUser.CreatedDate), ">=", TranslateUtils.ToDateTime(dateFrom));
            }
            if (!string.IsNullOrEmpty(dateTo))
            {
                query.Where(nameof(PointShopUser.CreatedDate), "<=", TranslateUtils.ToDateTime(dateTo));
            }

            query.OrderByDesc(nameof(PointShopUser.Id));

            var total = await _repository.CountAsync(query);
            var list = await _repository.GetAllAsync(query.ForPage(pageIndex, pageSize));

            return (total, list);
        }
        public async Task<int> Analysis_GetListAsync(int userId, string dateFrom, string dateTo)
        {
            var query = Q.Where(nameof(PointShopUser.UserId), userId);

            if (!string.IsNullOrEmpty(dateFrom))
            {
                query.Where(nameof(PointShopUser.CreatedDate), ">=", TranslateUtils.ToDateTime(dateFrom));
            }
            if (!string.IsNullOrEmpty(dateTo))
            {
                query.Where(nameof(PointShopUser.CreatedDate), "<=", TranslateUtils.ToDateTime(dateTo));
            }

            query.OrderByDesc(nameof(PointShopUser.Id));

            var total = await _repository.SumAsync(nameof(PointShopUser.Point), query);

            return total;
        }
    }
}
