using System.Threading.Tasks;

namespace XBLMS.Services
{
    public partial interface IDatabaseManager
    {
        Task ExecuteBackupAsync();
    }
}
