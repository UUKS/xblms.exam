using Datory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XBLMS.Configuration;
using XBLMS.Core.Repositories;
using XBLMS.Enums;
using XBLMS.Models;
using XBLMS.Repositories;
using XBLMS.Services;
using XBLMS.Utils;

namespace XBLMS.Web.Controllers.Admin.Settings.Database
{
    [OpenApiIgnore]
    [Authorize(Roles = Types.Roles.Administrator)]
    [Route(Constants.ApiAdminPrefix)]
    public partial class BackupController : ControllerBase
    {
        private const string RouteBackup = "settings/database/backup";
        private const string RouteBackupExcution = "settings/database/backup/excution";
        private const string RouteBackupDelete = "settings/database/backup/delbackup";
        private const string RouteBackupConfig = "settings/database/backup/config";

        private const string RouteBackupRecover = "settings/database/backup/recover";
        private const string RouteBackupRecoverlog = "settings/database/backup/recoverlog";
        private const string RouteBackupRecoverlogDel = "settings/database/backup/recoverlogdel";

        private const string RouteBackupDownload = "settings/database/backup/download";
        private const string RouteBackupUpload = "settings/database/backup/upload";


        private readonly IPathManager _pathManager;
        private readonly IDatabaseManager _databaseManager;
        private readonly IAuthManager _authManager;
        private readonly ISettingsManager _settingsManager;
        private readonly IDbBackupRepository _dbBackupRepository;
        private readonly IDbRecoverRepository _dbRecoverRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IScheduledTaskRepository _scheduledTaskRepository;
        public BackupController(IPathManager pathManager, IDatabaseManager databaseManager, IAuthManager authManager, ISettingsManager settingsManager, IConfigRepository configRepository,
             IDbBackupRepository dbBackupRepository, IDbRecoverRepository dbRecoverRepository, IScheduledTaskRepository scheduledTaskRepository)
        {
            _pathManager = pathManager;
            _databaseManager = databaseManager;
            _authManager = authManager;
            _settingsManager = settingsManager;
            _dbBackupRepository = dbBackupRepository;
            _dbRecoverRepository = dbRecoverRepository;
            _configRepository = configRepository;
            _scheduledTaskRepository = scheduledTaskRepository;
        }
        private async Task AddPingTask()
        {
            if (!await _scheduledTaskRepository.ExistsPingTask())
            {
                var task = new ScheduledTask
                {
                    TaskType = TaskType.Ping,
                    TaskInterval = TaskInterval.EveryMinute,
                    Every = 1,
                    StartDate = DateTime.Now,
                    Timeout = 60 * 24,
                    PingHost = PageUtils.GetHost(Request)
                };

                await _scheduledTaskRepository.InsertAsync(task);
            }
        }

        public class TableInfo
        {
            public List<TableColumn> Columns { get; set; }
            public int TotalCount { get; set; }
            public List<string> RowFiles { get; set; }
        }
        public class GetRequest
        {
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
            public string Token { get; set; }
            public bool IsFileServer { get; set; }
        }
        public class GetResult
        {
            public bool DbBackupAuto { get; set; }
            public bool ExistsBackup { get; set; }
            public int Total { get; set; }
            public List<DbBackup> List { get; set; }
        }
        public class GetRecoverResult
        {
            public int Total { get; set; }
            public List<DbRecover> List { get; set; }

        }

        public class GetRecoverRequest
        {
            public string SecurityKey { get; set; }
            public int Id { get; set; }
        }
        public class GetDownloadRequest
        {
            public string SecurityKey { get; set; }
            public int Id { get; set; }
        }
        public class GetUploadFormRequest
        {
            public string SecurityKey { get; set; }
            public string Name { get; set; }
            public int Chunk { get; set; }
            public long MaxChunk { get; set; }
            public string Guid { get; set; }
        }
        public class GetSetConfigRequest
        {
            public bool DbBackupAuto { get; set; }
        }
    }
}
