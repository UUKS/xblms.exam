using Datory;
using System.Collections.Generic;
using System.Threading.Tasks;
using XBLMS.Models;
using XBLMS.Utils;

namespace XBLMS.Core.Repositories
{
    public partial class ExamPracticeRepository
    {
        public async Task<(int total, List<ExamPractice> list)> Analysis_GetListAsync(int userId, string dateFrom, string dateTo, int pageIndex, int pageSize)
        {
            var query = Q.Where(nameof(ExamPractice.UserId), userId);

            if (!string.IsNullOrEmpty(dateFrom))
            {
                query.Where(nameof(ExamPractice.CreatedDate), ">=", TranslateUtils.ToDateTime(dateFrom));
            }
            if (!string.IsNullOrEmpty(dateTo))
            {
                query.Where(nameof(ExamPractice.CreatedDate), "<=", TranslateUtils.ToDateTime(dateTo));
            }

            query.OrderByDesc(nameof(ExamPractice.Id));

            var total = await _repository.CountAsync(query);
            var list = await _repository.GetAllAsync(query.ForPage(pageIndex, pageSize));

            return (total, list);
        }

        public async Task<(int answerTotal,int rightTotal)> Analysis_GetTotalAsync(int userId, string dateFrom, string dateTo)
        {
            var query = Q.Where(nameof(ExamPractice.UserId), userId);

            if (!string.IsNullOrEmpty(dateFrom))
            {
                query.Where(nameof(ExamPractice.CreatedDate), ">=", TranslateUtils.ToDateTime(dateFrom));
            }
            if (!string.IsNullOrEmpty(dateTo))
            {
                query.Where(nameof(ExamPractice.CreatedDate), "<=", TranslateUtils.ToDateTime(dateTo));
            }

            var answerTotal = await _repository.SumAsync(nameof(ExamPractice.AnswerCount), query);
            var rightTtoal = await _repository.SumAsync(nameof(ExamPractice.RightCount), query);

            return (answerTotal, rightTtoal);
        }
    }
}
