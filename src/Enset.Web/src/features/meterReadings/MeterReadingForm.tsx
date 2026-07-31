import { useState,type FormEvent } from "react";
import { meterReadingService } from "../../services/meterReadingService";
import type { MeterReadingWriteModel } from "./types";
import { dataQualityOptions,meterReadingTypeOptions } from "../../components/ui/enumOptions";
import { Dialog,FormField,fieldErrors,formError,useUnsavedChanges } from "../crud/crudUi";
export function MeterReadingForm({initial,entityId,unit,onClose,onSaved}:{initial:MeterReadingWriteModel;entityId?:string;unit:string;onClose:()=>void;onSaved:()=>void}){
 const [model,setModel]=useState(initial),[busy,setBusy]=useState(false),[error,setError]=useState("");const [errors,setErrors]=useState<Record<string,string>>({});const dirty=JSON.stringify(model)!==JSON.stringify(initial);useUnsavedChanges(dirty);const close=()=>{if(!dirty||window.confirm("Ungespeicherte Änderungen verwerfen?"))onClose();};const set=(key:keyof MeterReadingWriteModel,value:string|number|null)=>setModel(x=>({...x,[key]:value}));
 const submit=async(e:FormEvent)=>{e.preventDefault();setBusy(true);setError("");setErrors({});try{if(entityId)await meterReadingService.update(entityId,model);else await meterReadingService.create(model);onSaved();}catch(value){setError(formError(value));setErrors(fieldErrors(value));}finally{setBusy(false);}};
 return <Dialog title={entityId?"Messwert bearbeiten":"Messwert hinzufügen"} onClose={close}><form className="crud-form" onSubmit={submit}>{error&&<p className="form-error">{error}</p>}
 <FormField label="Zeitpunkt" error={errors.timestamp}><input type="datetime-local" required value={model.timestamp.slice(0,16)} onChange={e=>set("timestamp",new Date(e.target.value).toISOString())}/></FormField>
 <FormField label={`Wert (${unit})`} error={errors.value}><input type="number" step="any" required value={model.value} onChange={e=>set("value",Number(e.target.value))}/></FormField>
 <FormField label="Messwerttyp" error={errors.readingType}><select required value={model.readingType} onChange={e=>set("readingType",e.target.value)}>
  {meterReadingTypeOptions.map(option=><option key={option.value} value={option.value}>{option.label}</option>)}
 </select></FormField>
 <FormField label="Qualität" error={errors.qualityFlag}><select required value={model.qualityFlag} onChange={e=>set("qualityFlag",e.target.value)}>
  {dataQualityOptions.map(option=><option key={option.value} value={option.value}>{option.label}</option>)}
 </select></FormField>
 <FormField label="Intervall (Sekunden)" error={errors.intervalSeconds}><input type="number" value={model.intervalSeconds??""} onChange={e=>set("intervalSeconds",e.target.value===""?null:Number(e.target.value))}/></FormField>
 <FormField label="Korrekturgrund" error={errors.reason}><input value={model.reason??""} onChange={e=>set("reason",e.target.value||null)}/></FormField>
 <footer className="form-actions"><button type="button" onClick={close}>Abbrechen</button><button className="primary-button" disabled={busy}>{busy?"Speichert …":"Speichern"}</button></footer></form></Dialog>;
}
