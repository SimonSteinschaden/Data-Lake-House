using Enset.Domain.Common;

namespace Enset.Domain.Data;

public class DataSource : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public DataSourceType Type { get; set; }
}