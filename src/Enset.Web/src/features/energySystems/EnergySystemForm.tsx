import { useState, type FormEvent } from "react";
import { energySystemService } from "../../services/energySystemService";
import type { EnergySystemWriteModel } from "./types";
import { energySystemTypeOptions } from "../../components/ui/enumOptions";
import { Dialog, FormField, fieldErrors, formError, useUnsavedChanges } from "../crud/crudUi";
export function EnergySystemForm({ initial, entityId, onClose, onSaved }: { initial: EnergySystemWriteModel; entityId?: string; onClose: () => void; onSaved: () => void }) {
  const [model, setModel] = useState(initial); const [error, setError] = useState(""); const [errors, setErrors] = useState<Record<string,string>>({}); const [busy,setBusy]=useState(false);
  const dirty=JSON.stringify(model)!==JSON.stringify(initial); useUnsavedChanges(dirty); const close=()=>{if(!dirty||window.confirm("Ungespeicherte Änderungen verwerfen?"))onClose();};
  const set=(key:keyof EnergySystemWriteModel,value:string|number|null)=>setModel(x=>({...x,[key]:value}));
  const submit=async(e:FormEvent)=>{e.preventDefault();setBusy(true);setError("");setErrors({});try{if(entityId)await energySystemService.update(entityId,model);else await energySystemService.create(model);onSaved();}catch(value){setError(formError(value));setErrors(fieldErrors(value));}finally{setBusy(false);}};
  return <Dialog title={entityId?"Anlage bearbeiten":"Anlage hinzufügen"} onClose={close}><form className="crud-form" onSubmit={submit}>{error&&<p className="form-error">{error}</p>}
    <FormField label="Anlagennummer" error={errors.energySystemNumber}><input required value={model.energySystemNumber} onChange={e=>set("energySystemNumber",e.target.value)}/></FormField>
    <FormField label="Bezeichnung" error={errors.name}><input required value={model.name} onChange={e=>set("name",e.target.value)}/></FormField>
    <FormField label="Anlagentyp" error={errors.type}><select required value={model.type} onChange={e=>set("type",e.target.value)}>
      <option value="">Auswählen</option>
      {energySystemTypeOptions.map(option =>
        <option key={option.value} value={option.value}>{option.label}</option>)}
    </select></FormField>
    <FormField label="Leistung (kW)" error={errors.ratedPowerKw}><input type="number" step="any" value={model.ratedPowerKw??""} onChange={e=>set("ratedPowerKw",e.target.value===""?null:Number(e.target.value))}/></FormField>
    <FormField label="Inbetriebnahme" error={errors.commissionedAt}><input type="date" value={model.commissionedAt?.slice(0,16)??""} onChange={e=>set("commissionedAt",e.target.value?new Date(e.target.value).toISOString():null)}/></FormField>
    <FormField label="Stilllegung" error={errors.decommissionedAt}><input type="date" value={model.decommissionedAt?.slice(0,16)??""} onChange={e=>set("decommissionedAt",e.target.value?new Date(e.target.value).toISOString():null)}/></FormField>
    <footer className="form-actions"><button type="button" onClick={close}>Abbrechen</button><button className="primary-button" disabled={busy}>{busy?"Speichert …":"Speichern"}</button></footer>
  </form></Dialog>;
}
