using Datory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XBLMS.Dto;
using XBLMS.Utils;

namespace XBLMS.Core.Services
{
    public partial class DatabaseManager
    {
        public async Task ExecuteBackupAsync()
        {
            var backupinfo = await DbBackupRepository.GetBackupingAsync();

            var backupPath = PathUtils.Combine(DirectoryUtils.SiteFiles.DirectoryName, DirectoryUtils.SiteFiles.DbBackupFiles, $"{DateTime.Now:yyyy-MM-dd-hh-mm-ss}-{StringUtils.GetShortGuid()}");
            var directory = PathUtils.Combine(_settingsManager.WebRootPath, backupPath);
            var tableNames = new List<string>();

            var repositories = GetAllRepositories();
            foreach (var repository in repositories)
            {
                tableNames.Add(repository.TableName);
            }

            var tablesFilePath = PathUtils.Combine(directory, "_tables.json");
            await FileUtils.WriteTextAsync(tablesFilePath, TranslateUtils.JsonSerialize(tableNames));

            var successTables = new List<string>();
            var errorTables = new List<string>();
            var errorLog = "";

            foreach (var tableName in tableNames)
            {
                try
                {
                    await BackupTable(tableName, directory);
                    successTables.Add(tableName);

                    if (StringUtils.EqualsIgnoreCase(ExamPaperRepository.TableName, tableName))
                    {
                        var tableIdList = await ExamPaperRepository.Select_GetSeparateStorageIdList();
                        if (tableIdList != null && tableIdList.Count > 0)
                        {
                            foreach (var tableId in tableIdList)
                            {
                                var ExamPaperAnswer_TableName = ExamPaperAnswerRepository.GetNewTableNameAsync(tableId);
                                await BackupTable(ExamPaperAnswer_TableName, directory);
                                var ExamPaperRandomConfig_TableName = ExamPaperRandomConfigRepository.GetNewTableNameAsync(tableId);
                                await BackupTable(ExamPaperRandomConfig_TableName, directory);
                                var ExamPaperRandom_TableName = ExamPaperRandomRepository.GetNewTableNameAsync(tableId);
                                await BackupTable(ExamPaperRandom_TableName, directory);
                                var ExamPaperRandomTm_TableName = ExamPaperRandomTmRepository.GetNewTableNameAsync(tableId);
                                await BackupTable(ExamPaperRandomTm_TableName, directory);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorTables.Add(tableName);
                    errorLog += $"{tableName}:{ex};";
                }
            }

            long totalSize = DirectoryUtils.GetTotalSize(directory);
            var paths = DirectoryUtils.GetDirectoryPaths(directory);
            foreach (var path in paths)
            {
                totalSize += DirectoryUtils.GetTotalSize(path);
            }

            backupinfo.EndTime = DateTime.Now;
            backupinfo.Status = errorTables.Count > 0 ? 2 : 1;
            backupinfo.ErrorLog = errorLog.ToString();
            backupinfo.SuccessTables = successTables;
            backupinfo.ErrorTables = errorTables;
            backupinfo.FilePath = backupPath;
            backupinfo.DataSize = FileUtils.GetFileSizeByFileLength(totalSize);
            await DbBackupRepository.UpdateAsync(backupinfo);

        }
        private async Task BackupTable(string tableName, string directory)
        {
            var columns = await _settingsManager.Database.GetTableColumnsAsync(tableName);
            var repository = new Repository(_settingsManager.Database, tableName, columns);

            var tableInfo = new DbTableInfo
            {
                Columns = repository.TableColumns,
                TotalCount = await repository.CountAsync(),
                RowFiles = []
            };

            if (tableInfo.TotalCount > 0)
            {
                var fileName = $"{tableName}.json";
                tableInfo.RowFiles.Add(fileName);
                var rows = await GetObjectsAsync(tableName);

                var filepath = PathUtils.Combine(directory, fileName);
                await FileUtils.WriteTextAsync(filepath, TranslateUtils.JsonSerialize(rows));
            }
            var metapath = PathUtils.Combine(directory, tableName, "_metadata.json");
            await FileUtils.WriteTextAsync(metapath, TranslateUtils.JsonSerialize(tableInfo));
        }
    }
}
