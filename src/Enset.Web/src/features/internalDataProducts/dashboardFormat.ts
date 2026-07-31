export const number = (value: number) => value.toLocaleString("de-AT",
  { maximumFractionDigits: 2 });

export const carrier = (value: string) => ({
  Electricity: "Strom", Heat: "Wärme", Gas: "Gas", DistrictHeating: "Fernwärme",
  Water: "Wasser", Cooling: "Kälte", Steam: "Dampf", Hydrogen: "Wasserstoff",
}[value] ?? value);

export const direction = (value: string) => ({
  Consumption: "Verbrauch", Production: "Erzeugung",
  Bidirectional: "Bezug und Einspeisung",
}[value] ?? value);
