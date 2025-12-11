using System;
using System.Threading.Tasks;
using XBLMS.Models;

namespace XBLMS.Core.Services
{
    public partial class ScheduledHostedService
    {
        private async Task DbBackupAsync()
        {
            var config = await _databaseManager.ConfigRepository.GetAsync();
            var isBackuping = await _databaseManager.DbBackupRepository.ExistsBackupingAsync();
            if (isBackuping)
            {
                await _databaseManager.ExecuteBackupAsync();
            }
            else
            {
                var existsToday = await _databaseManager.DbBackupRepository.ExistsTodayAsync();
                if (!existsToday && config.DbBackupAuto)
                {
                    await _databaseManager.DbBackupRepository.InsertAsync(new DbBackup
                    {
                        BeginTime = DateTime.Now,
                        Status = 0
                    });
                    await _databaseManager.ExecuteBackupAsync();
                }
            }
        }
    }
}
