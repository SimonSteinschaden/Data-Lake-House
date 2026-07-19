namespace Enset.Domain.Energy;

public enum MeterUnit
{
    Unknown = 0,

    Wh = 1,
    KWh = 2,
    MWh = 3,

    W = 10,
    KW = 11,
    MW = 12,

    CubicMeter = 20,
    CubicMeterPerHour = 21,

    Liter = 22,
    LiterPerSecond = 23,

    Celsius = 30,
    Kelvin = 31,

    Pascal = 40,
    Bar = 41,

    Volt = 50,
    Ampere = 51,
    Hertz = 52,

    WattPerSquareMeter = 60,
    MeterPerSecond = 61,
    Percent = 62,

    Other = 99
}