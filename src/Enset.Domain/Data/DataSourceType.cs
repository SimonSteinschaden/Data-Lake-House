public class DataSource : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public DataSourceType Type { get; set; }

    public string? ExternalIdentifier { get; set; }

    public string? Provider { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }
} //für spätere Imports: Smart Meter Portal, Fronius API, CSV-Import Gemeinde Gars, MQTT-Broker,Modbus Gateway,etc.