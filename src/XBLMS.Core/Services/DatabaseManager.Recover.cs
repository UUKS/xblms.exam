using Datory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XBLMS.Dto;
using XBLMS.Models;
using XBLMS.Utils;

namespace XBLMS.Core.Services
{
    public partial class DatabaseManager
    {
        public async Task<bool> ExecuteRecoverAsync(int jobId)
        {
            var job = await DbBackupRepository.GetAsync(jobId);
            var jobRecover = new DbRecover
            {
                JobId = job.Id,
                BeginTime = DateTime.Now
            };

            if (job == null || job.Status == 0 || job.Status == 2 || string.IsNullOrWhiteSpace(job.FilePath))
            {
                jobRecover.EndTime = DateTime.Now;
                jobRecover.Status = 2;
                jobRecover.ErrorLog = "备份异常，无法恢复。";
                await DbRecoverRepository.InsertAsync(jobRecover);
                return false;
            }
            else
            {
                var directionryPath = PathUtils.Combine(_settingsManager.WebRootPath, job.FilePath);

                if (!DirectoryUtils.IsDirectoryExists(directionryPath))
                {
                    jobRecover.EndTime = DateTime.Now;
                    jobRecover.Status = 2;
                    jobRecover.ErrorLog = "恢复文件路径不存在。";
                    await DbRecoverRepository.InsertAsync(jobRecover);
                    return false;
                }
                var tablesFilePath = PathUtils.Combine(directionryPath, "_tables.json");
                if (!FileUtils.IsFileExists(tablesFilePath))
                {
                    jobRecover.EndTime = DateTime.Now;
                    jobRecover.Status = 2;
                    jobRecover.ErrorLog = "恢复数据结构不存在。";
                    await DbRecoverRepository.InsertAsync(jobRecover);
                    return false;
                }

                var allRepositorys = GetAllRepositories();

                var tableNamesContent = await FileUtils.ReadTextAsync(tablesFilePath, Encoding.UTF8);
                var tableNames = TranslateUtils.JsonDeserialize<List<string>>(tableNamesContent);
                var errorTableNames = new List<string>();
                var errorLogs = "";

                var extendExamPaperTableNames = new List<string>()
                {
                    $"{ExamPaperAnswerRepository.TableName}_",
                    $"{ExamPaperRandomConfigRepository.TableName}_",
                    $"{ExamPaperRandomRepository.TableName}_",
                    $"{ExamPaperRandomTmRepository.TableName}_"
                };

                foreach (var tableName in tableNames)
                {
                    var includes = new List<string>
                    {
                        DbRecoverRepository.TableName,
                        DbBackupRepository.TableName,
                        ScheduledTaskRepository.TableName
                    };
                    if (ListUtils.ContainsIgnoreCase(includes, tableName)) continue;

                    if (extendExamPaperTableNames.Exists(tName => StringUtils.ContainsIgnoreCase(tableName, tName)))
                    {
                        var examPaper_TableName_List = ListUtils.GetStringList(tableName, "_");
                        if (examPaper_TableName_List != null && examPaper_TableName_List.Count == 3)
                        {
                            Repository dbRepository = null;
                            var tbName = string.Empty;
                            var tbNameCount = examPaper_TableName_List[2];
                            if (StringUtils.ContainsIgnoreCase(tableName, $"{ExamPaperAnswerRepository.TableName}_"))
                            {
                                tbName = $"{ExamPaperAnswerRepository.TableName}_{tbNameCount}";
                                dbRepository = new Repository(_settingsManager.Database, tbName, ExamPaperAnswerRepository.TableColumns);
                            }
                            if (StringUtils.ContainsIgnoreCase(tableName, $"{ExamPaperRandomConfigRepository.TableName}_"))
                            {
                                tbName = $"{ExamPaperRandomConfigRepository.TableName}_{tbNameCount}";
                                dbRepository = new Repository(_settingsManager.Database, tbName, ExamPaperRandomConfigRepository.TableColumns);
                            }
                            if (StringUtils.ContainsIgnoreCase(tableName, $"{ExamPaperRandomRepository.TableName}_"))
                            {
                                tbName = $"{ExamPaperRandomRepository.TableName}_{tbNameCount}";
                                dbRepository = new Repository(_settingsManager.Database, tbName, ExamPaperRandomRepository.TableColumns);
                            }
                            if (StringUtils.ContainsIgnoreCase(tableName, $"{ExamPaperRandomTmRepository.TableName}_"))
                            {
                                tbName = $"{ExamPaperRandomTmRepository.TableName}_{tbNameCount}";
                                dbRepository = new Repository(_settingsManager.Database, tbName, ExamPaperRandomTmRepository.TableColumns);
                            }
                            if (dbRepository != null)
                            {
                                await RecoverTable(dbRepository, directionryPath, errorTableNames);
                            }
                        }
                    }
                    else
                    {
                        var repository = allRepositorys.SingleOrDefault(r => StringUtils.EqualsIgnoreCase(r.TableName, tableName));
                        if (repository != null)
                        {
                            var dbRepository = new Repository(_settingsManager.Database, repository.TableName,
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

                await DbRecoverRepository.InsertAsync(jobRecover);
                return jobRecover.Status != 2;
            }
        }
        private async Task RecoverTable(Repository repository, string directionryPath, List<string> errorTableNames)
        {
            var tableName = repository.TableName;
            var metadataFilePath = PathUtils.Combine(directionryPath, tableName, "_metadata.json");
            var tableInfoContent = await FileUtils.ReadTextAsync(metadataFilePath, Encoding.UTF8);

            if (FileUtils.IsFileExists(metadataFilePath) && !string.IsNullOrWhiteSpace(tableInfoContent))
            {
                var tableInfo = TranslateUtils.JsonDeserialize<DbTableInfo>(tableInfoContent);

                if (!await _settingsManager.Database.IsTableExistsAsync(tableName))
                {
                    await CreateTableAsync(tableName, repository.TableColumns);
                }
                else
                {
                    await _settingsManager.Database.TruncateTableAsync(tableName);
                }

                if (tableInfo.RowFiles.Count > 0)
                {
                    for (var i = 0; i < tableInfo.RowFiles.Count; i++)
                    {
                        var fileName = tableInfo.RowFiles[i];
                        var filepath = PathUtils.Combine(directionryPath, fileName);

                        var objectContents = await FileUtils.ReadTextAsync(filepath, Encoding.UTF8);

                        if (!string.IsNullOrWhiteSpace(objectContents))
                        {
                            var objects = TranslateUtils.JsonDeserialize<List<JObject>>(objectContents);
                            try
                            {
                                await repository.BulkInsertAsync(objects);
                            }
                            catch
                            {
                                errorTableNames.Add(tableName);
                            }
                        }
                    }
                }
            }
        }
    }
}
