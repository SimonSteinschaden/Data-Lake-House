public class DataSource : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public DataSourceType Type { get; set; }
}