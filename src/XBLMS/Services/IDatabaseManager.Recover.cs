using System.Threading.Tasks;

namespace XBLMS.Services
{
    public partial interface IDatabaseManager
    {
        Task<bool> ExecuteRecoverAsync(int jobId);
    }
}
