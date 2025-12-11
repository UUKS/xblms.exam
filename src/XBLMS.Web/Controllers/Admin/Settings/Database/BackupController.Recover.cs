using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using XBLMS.Dto;
using XBLMS.Enums;
using XBLMS.Utils;

namespace XBLMS.Web.Controllers.Admin.Settings.Database
{
    public partial class BackupController
    {
        [HttpPost, Route(RouteBackupRecoverlog)]
        public async Task<ActionResult<GetRecoverResult>> GetRecoverLog([FromBody] GetRequest request)
        {
            if (!await _authManager.HasPermissionsAsync())
            {
                return this.NoAuth();
            }

            var (total, list) = await _dbRecoverRepository.GetListAsync(request.PageIndex, request.PageSize);
            return new GetRecoverResult
            {
                Total = total,
                List = list
            };
        }
        [HttpPost, Route(RouteBackupRecoverlogDel)]
        public async Task<ActionResult<BoolResult>> DeleteRecoverLog()
        {
            if (!await _authManager.HasPermissionsAsync(MenuPermissionType.Delete))
            {
                return this.NoAuth();
            }

            await _dbRecoverRepository.DeleteAsync();
            await _authManager.AddAdminLogAsync("清空数据库还原日志");
            return new BoolResult { Value = true };
        }

        [HttpPost, Route(RouteBackupRecover)]
        public async Task<ActionResult<BoolResult>> Recover([FromBody] GetRecoverRequest request)
        {
            if (_settingsManager.SecurityKey != request.SecurityKey)
            {
                return this.Error("SecurityKey 输入错误！");
            }
            var result = await _databaseManager.ExecuteRecoverAsync(request.Id);

            await _authManager.AddAdminLogAsync("恢复数据库", $"备份任务id：{request.Id}");

            return new BoolResult { Value = result };
        }
    }
}
