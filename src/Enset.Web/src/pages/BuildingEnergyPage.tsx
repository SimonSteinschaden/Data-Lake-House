import { useEffect, useState } from "react";
import { useParams } from "react-router";
import { BuildingEnergyDashboard } from "../features/dataProducts/BuildingEnergyDashboard";
import { dataProductService } from "../features/dataProducts/dataProductService";
import type { ProductVersion } from "../features/dataProducts/types";
export function BuildingEnergyPage() { const { buildingId } = useParams(); const [version, setVersion] = useState<ProductVersion>(); const [error, setError] = useState(""); useEffect(() => { dataProductService.list().then(items => items.find(x => x.code === "BUILDING_ENERGY_PROFILE" && x.scopeId === buildingId)).then(item => item ? dataProductService.latest(item.id) : Promise.reject(new Error("Kein Building Energy Profile vorhanden."))).then(setVersion).catch(e => setError(e.message)); }, [buildingId]); return <main>{error && <p className="error">{error}</p>}{version && <BuildingEnergyDashboard version={version} />}</main>; }
