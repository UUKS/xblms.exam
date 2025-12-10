using Datory;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XBLMS.Dto;
using XBLMS.Enums;
using XBLMS.Models;
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


            var job = await _dbBackupRepository.GetAsync(request.Id);
            var jobRecover = new DbRecover
            {
                JobId = job.Id,
                BeginTime = DateTime.Now,
            };
            if (job == null || job.Status == 0 || job.Status == 2 || string.IsNullOrWhiteSpace(job.FilePath))
            {
                jobRecover.EndTime = DateTime.Now;
                jobRecover.Status = 2;
                jobRecover.ErrorLog = "备份异常，无法恢复。";
                await _dbRecoverRepository.InsertAsync(jobRecover);
                return new BoolResult
                {
                    Value = false
                };

            }
            else
            {
                var directionryPath = PathUtils.Combine(_settingsManager.WebRootPath, job.FilePath);

                if (!DirectoryUtils.IsDirectoryExists(directionryPath))
                {
                    jobRecover.EndTime = DateTime.Now;
                    jobRecover.Status = 2;
                    jobRecover.ErrorLog = "恢复文件路径不存在。";
                    await _dbRecoverRepository.InsertAsync(jobRecover);
                    return new BoolResult
                    {
                        Value = false
                    };
                }
                var tablesFilePath = PathUtils.Combine(directionryPath, "_tables.json");
                if (!FileUtils.IsFileExists(tablesFilePath))
                {
                    jobRecover.EndTime = DateTime.Now;
                    jobRecover.Status = 2;
                    jobRecover.ErrorLog = "恢复数据结构不存在。";
                    await _dbRecoverRepository.InsertAsync(jobRecover);
                    return new BoolResult
                    {
                        Value = false
                    };
                }

                var allRepositorys = _databaseManager.GetAllRepositories();

                var tableNames = TranslateUtils.JsonDeserialize<List<string>>(await FileUtils.ReadTextAsync(tablesFilePath, Encoding.UTF8));
                var errorTableNames = new List<string>();
                var errorLogs = "";
                foreach (var tableName in tableNames)
                {
                    var includes = new List<string>
                    {
                        _dbRecoverRepository.TableName,
                        _dbBackupRepository.TableName,
                        _databaseManager.ScheduledTaskRepository.TableName
                    };
                    if (ListUtils.ContainsIgnoreCase(includes, tableName)) continue;

                    var extendExamPaperTableNames = new List<string>();
                    var tableIdList = await _databaseManager.ExamPaperRepository.Select_GetSeparateStorageIdList();
                    if (tableIdList != null && tableIdList.Count > 0)
                    {
                        foreach (var tableId in tableIdList)
                        {
                            extendExamPaperTableNames.Add(_databaseManager.ExamPaperAnswerRepository.GetNewTableNameAsync(tableId));
                            extendExamPaperTableNames.Add(_databaseManager.ExamPaperRandomConfigRepository.GetNewTableNameAsync(tableId));
                            extendExamPaperTableNames.Add(_databaseManager.ExamPaperRandomRepository.GetNewTableNameAsync(tableId));
                            extendExamPaperTableNames.Add(_databaseManager.ExamPaperRandomTmRepository.GetNewTableNameAsync(tableId));
                        }
                    }
                    if (ListUtils.ContainsIgnoreCase(extendExamPaperTableNames, tableName))
                    {
                        Repository dbRepository = null;
                        foreach (var tableId in tableIdList)
                        {
                            var ExamPaperAnswer_TableName = _databaseManager.ExamPaperAnswerRepository.GetNewTableNameAsync(tableId);
                            var ExamPaperRandomConfig_TableName = _databaseManager.ExamPaperRandomConfigRepository.GetNewTableNameAsync(tableId);
                            var ExamPaperRandom_TableName = _databaseManager.ExamPaperRandomRepository.GetNewTableNameAsync(tableId);
                            var ExamPaperRandomTm_TableName = _databaseManager.ExamPaperRandomTmRepository.GetNewTableNameAsync(tableId);

                            if (StringUtils.EqualsIgnoreCase(tableName, ExamPaperAnswer_TableName))
                            {
                                dbRepository = new Repository(_settingsManager.Database, tableName,
                                _databaseManager.ExamPaperAnswerRepository.TableColumns);
                            }
                            if (StringUtils.EqualsIgnoreCase(tableName, ExamPaperRandomConfig_TableName))
                            {
                                dbRepository = new Repository(_settingsManager.Database, tableName,
                               _databaseManager.ExamPaperRandomConfigRepository.TableColumns);
                            }
                            if (StringUtils.EqualsIgnoreCase(tableName, ExamPaperRandom_TableName))
                            {
                                dbRepository = new Repository(_settingsManager.Database, tableName,
                                _databaseManager.ExamPaperRandomRepository.TableColumns);
                            }
                            if (StringUtils.EqualsIgnoreCase(tableName, ExamPaperRandomTm_TableName))
                            {
                                dbRepository = new Repository(_settingsManager.Database, tableName,
                                _databaseManager.ExamPaperRandomTmRepository.TableColumns);
                            }
                            if (dbRepository != null)
                            {
                                await RecoverTable(dbRepository, directionryPath, errorTableNames);
                            }
                        }
                    }
                    else
                    {
                        var repository = allRepositorys.SingleOrDefault(r => r.TableName == tableName);
                        if (repository != null)
                        {
                            var dbRepository = new Repository(_settingsManager.Database, tableName,
                              repository.TableColumns);
                            if (dbRepository != null)
                            {
                                await RecoverTable(dbRepository, directionryPath, errorTableNames);
                            }
                        }
                    }
                }

                jobRecover.EndTime = DateTime.Now;
                jobRecover.ErrorTables = errorTableNames;
                jobRecover.ErrorLog = errorLogs;
                jobRecover.Status = 1;
                if (!string.IsNullOrEmpty(errorLogs) || errorTableNames.Count > 0)
                {
                    jobRecover.Status = 2;
                }
                await _authManager.AddAdminLogAsync("恢复数据库", $"备份任务id：{job.Id}");
                await _dbRecoverRepository.InsertAsync(jobRecover);
                return new BoolResult
                {
                    Value = jobRecover.Status != 2
                };
            }
        }
        private async Task RecoverTable(Repository repository, string directionryPath, List<string> errorTableNames)
        {
            var metadataFilePath = PathUtils.Combine(directionryPath, repository.TableName, "_metadata.json");

            if (FileUtils.IsFileExists(metadataFilePath))
            {
                var tableInfo = TranslateUtils.JsonDeserialize<TableInfo>(await FileUtils.ReadTextAsync(metadataFilePath, Encoding.UTF8));

                if (await _settingsManager.Database.IsTableExistsAsync(repository.TableName))
                {
                    await _settingsManager.Database.DropTableAsync(repository.TableName);
                }

                await _settingsManager.Database.CreateTableAsync(repository.TableName, repository.TableColumns);

                if (tableInfo.RowFiles.Count > 0)
                {
                    for (var i = 0; i < tableInfo.RowFiles.Count; i++)
                    {
                        var fileName = tableInfo.RowFiles[i];
                        var filepath = PathUtils.Combine(directionryPath, fileName);
                        var objects = TranslateUtils.JsonDeserialize<List<JObject>>(
                            await FileUtils.ReadTextAsync(filepath, Encoding.UTF8));

                        await repository.BulkInsertAsync(objects);
                    }
                }
            }
        }
    }
}
