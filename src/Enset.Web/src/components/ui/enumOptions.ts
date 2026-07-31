export interface EnumOption<T extends string = string> {
  value: T;
  label: string;
}

export const buildingCategoryOptions: EnumOption[] = [
  { value: "Apartment", label: "Mehrfamilienhaus" },
  { value: "House", label: "Haus" },
  { value: "Office", label: "Büro" },
  { value: "Hall", label: "Halle" },
  { value: "School", label: "Schule" },
  { value: "Retail", label: "Handel" },
  { value: "Industry", label: "Industrie" },
  { value: "Other", label: "Sonstiges" },
];

export const primaryUseTypeOptions: EnumOption[] = [
  { value: "Residential", label: "Wohnen" },
  { value: "Commercial", label: "Gewerbe" },
  { value: "Public", label: "Öffentlich" },
  { value: "Mixed", label: "Mischnutzung" },
];

export const buildingStateOptions: EnumOption[] = [
  { value: "Existing", label: "Bestand" },
  { value: "Improved", label: "Saniert/Verbessert" },
  { value: "Target", label: "Zielzustand" },
];

export const meterMediumOptions: EnumOption[] = [
  { value: "Electricity", label: "Strom" },
  { value: "Gas", label: "Gas" },
  { value: "Heat", label: "Wärme" },
  { value: "Cooling", label: "Kälte" },
  { value: "Water", label: "Wasser" },
  { value: "Steam", label: "Dampf" },
  { value: "Hydrogen", label: "Wasserstoff" },
  { value: "Other", label: "Sonstiges" },
];

export const meterUnitOptions: EnumOption[] = [
  { value: "Wh", label: "Wh" },
  { value: "KWh", label: "kWh" },
  { value: "MWh", label: "MWh" },
  { value: "W", label: "W" },
  { value: "KW", label: "kW" },
  { value: "MW", label: "MW" },
  { value: "CubicMeter", label: "m³" },
  { value: "CubicMeterPerHour", label: "m³/h" },
  { value: "Liter", label: "Liter" },
  { value: "LiterPerSecond", label: "Liter/s" },
  { value: "Celsius", label: "°C" },
  { value: "Kelvin", label: "K" },
  { value: "Other", label: "Sonstiges" },
];

export const meterDirectionOptions: EnumOption[] = [
  { value: "Consumption", label: "Verbrauch" },
  { value: "Production", label: "Erzeugung" },
  { value: "Bidirectional", label: "Bidirektional" },
];

export const energySystemTypeOptions: EnumOption[] = [
  { value: "Photovoltaic", label: "Photovoltaik" },
  { value: "HeatPump", label: "Wärmepumpe" },
  { value: "Boiler", label: "Kessel" },
  { value: "DistrictHeating", label: "Fernwärme" },
  { value: "BatteryStorage", label: "Batteriespeicher" },
  { value: "Ventilation", label: "Lüftung" },
  { value: "Cooling", label: "Kälteanlage" },
  { value: "ChargingInfrastructure", label: "Ladeinfrastruktur" },
  { value: "Other", label: "Sonstiges" },
];

export const meterReadingTypeOptions: EnumOption[] = [
  { value: "Instantaneous", label: "Momentanwert" },
  { value: "IntervalValue", label: "Intervallwert" },
  { value: "CumulativeValue", label: "Zählerstand" },
  { value: "Calculated", label: "Berechneter Wert" },
];

export const dataQualityOptions: EnumOption[] = [
  { value: "Measured", label: "Gemessen" },
  { value: "Validated", label: "Validiert" },
  { value: "Estimated", label: "Geschätzt" },
  { value: "Interpolated", label: "Interpoliert" },
  { value: "Calculated", label: "Berechnet" },
  { value: "Invalid", label: "Ungültig" },
  { value: "Missing", label: "Fehlend" },
];
