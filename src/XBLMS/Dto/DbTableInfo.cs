using Datory;
using System.Collections.Generic;

namespace XBLMS.Dto
{
    public class DbTableInfo
    {
        public List<TableColumn> Columns { get; set; }
        public int TotalCount { get; set; }
        public List<string> RowFiles { get; set; }
    }
}
