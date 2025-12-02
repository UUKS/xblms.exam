using Datory;
using System.Collections.Generic;
using System.Threading.Tasks;
using XBLMS.Models;
using XBLMS.Utils;

namespace XBLMS.Core.Repositories
{
    public partial class ExamCerUserRepository
    {
        public async Task<(int total, List<ExamCerUser> list)> Analysis_GetListAsync(int userId, string dateFrom, string dateTo, int pageIndex, int pageSize)
        {
            var query = Q.Where(nameof(ExamCerUser.UserId), userId);

            if (!string.IsNullOrEmpty(dateFrom))
            {
                query.Where(nameof(ExamCerUser.CreatedDate), ">=", TranslateUtils.ToDateTime(dateFrom));
            }
            if (!string.IsNullOrEmpty(dateTo))
            {
                query.Where(nameof(ExamCerUser.CreatedDate), "<=", TranslateUtils.ToDateTime(dateTo));
            }

            query.OrderByDesc(nameof(ExamCerUser.Id));

            var total = await _repository.CountAsync(query);
            var list = await _repository.GetAllAsync(query.ForPage(pageIndex, pageSize));

            return (total, list);
        }
        public async Task<int> CountAsync(int userId)
        {
            var query = Q.Where(nameof(ExamCerUser.UserId), userId);

            return await _repository.CountAsync(query);
        }
        public async Task<int> Analysis_GetTotalAsync(int userId, string dateFrom, string dateTo)
        {
            var query = Q.Where(nameof(ExamCerUser.UserId), userId);

            if (!string.IsNullOrEmpty(dateFrom))
            {
                query.Where(nameof(ExamCerUser.CreatedDate), ">=", TranslateUtils.ToDateTime(dateFrom));
            }
            if (!string.IsNullOrEmpty(dateTo))
            {
                query.Where(nameof(ExamCerUser.CreatedDate), "<=", TranslateUtils.ToDateTime(dateTo));
            }

            return await _repository.CountAsync(query);
        }
    }
}
