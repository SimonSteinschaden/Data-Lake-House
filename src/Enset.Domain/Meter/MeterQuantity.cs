namespace Enset.Domain.Energy;

public enum MeterQuantity
{
    Unknown = 0,

    Energy = 1,
    Power = 2,

    Volume = 3,
    Flow = 4,

    Temperature = 5,
    Pressure = 6,

    Voltage = 7,
    Current = 8,
    Frequency = 9,

    Irradiance = 20,
    WindSpeed = 21,
    Humidity = 22,

    Other = 99
}
