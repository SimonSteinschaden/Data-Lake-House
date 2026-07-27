import { useState,type FormEvent } from "react";
import { meterService } from "../../services/meterService";
import type { MeterWriteModel } from "./types";
import { Dialog,FormField,fieldErrors,formError,useUnsavedChanges } from "../crud/crudUi";
export function MeterForm({initial,entityId,onClose,onSaved}:{initial:MeterWriteModel;entityId?:string;onClose:()=>void;onSaved:(id:string)=>void}){
 const [model,setModel]=useState(initial),[busy,setBusy]=useState(false),[error,setError]=useState("");const [errors,setErrors]=useState<Record<string,string>>({});const dirty=JSON.stringify(model)!==JSON.stringify(initial);useUnsavedChanges(dirty);const close=()=>{if(!dirty||window.confirm("Ungespeicherte Änderungen verwerfen?"))onClose();};const set=(key:keyof MeterWriteModel,value:string|null)=>setModel(x=>({...x,[key]:value}));
 const submit=async(e:FormEvent)=>{e.preventDefault();setBusy(true);setError("");setErrors({});try{const result=entityId?await meterService.update(entityId,model):await meterService.create(model);onSaved(result.id);}catch(value){setError(formError(value));setErrors(fieldErrors(value));}finally{setBusy(false);}};
 return <Dialog title={entityId?"Zählpunkt bearbeiten":"Zählpunkt anlegen"} onClose={close}><form className="crud-form" onSubmit={submit}>{error&&<p className="form-error">{error}</p>}
 {([["meterNumber","Zählpunktnummer"],["name","Name"],["buildingId","Gebäude-ID"],["medium","Energieträger"],["quantity","Messgröße"],["unit","Einheit"],["direction","Richtung"],["type","Typ"]] as [keyof MeterWriteModel,string][]).map(([key,label])=><FormField key={key} label={label} error={errors[key]}><input required value={String(model[key]??"")} onChange={e=>set(key,e.target.value)}/></FormField>)}
 <FormField label="Verknüpfte Anlage" error={errors.energySystemId}><input value={model.energySystemId??""} onChange={e=>set("energySystemId",e.target.value||null)}/></FormField>
 <footer className="form-actions"><button type="button" onClick={close}>Abbrechen</button><button className="primary-button" disabled={busy}>{busy?"Speichert …":"Speichern"}</button></footer></form></Dialog>;
}
