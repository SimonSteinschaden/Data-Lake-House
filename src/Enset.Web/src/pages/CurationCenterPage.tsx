import { useCallback, useEffect, useState } from "react";
import { AdminPageHeader, PageState } from "../components/admin/AdminUi";
import { errorMessage } from "../components/admin/adminFormat";
import { curationService } from "../services/curationService";
import type { CurationStatistics, CurationTask, CurationTaskDetail } from "../features/curation/types";
import "./CurationCenterPage.css";

const fieldLabels: Record<string,string> = {
  BuildingCategory:"Gebäudetyp", PrimaryUseType:"Nutzungsart",
  Medium:"Energieträger", CustomerId:"Kundenzuordnung", BuildingId:"Objektzuordnung",
  PostalCode:"Postleitzahl", Address:"Adresse",
};
const entityLabels: Record<string,string> = {
  Building:"Gebäude", MeteringPoint:"Zählpunkt", EnergySystem:"Anlage", Customer:"Kunde",
};
export function CurationCenterPage() {
  const[tasks,setTasks]=useState<CurationTask[]>();const[statistics,setStatistics]=useState<CurationStatistics>();const[selected,setSelected]=useState<CurationTaskDetail>();const[error,setError]=useState("");const[busy,setBusy]=useState(false);const[customizing,setCustomizing]=useState(false);const[value,setValue]=useState("");const[reason,setReason]=useState("");
  const load=useCallback(async()=>{try{const[taskItems,stats]=await Promise.all([curationService.tasks(),curationService.statistics()]);setTasks(taskItems);setStatistics(stats);}catch(e){setError(errorMessage(e));}},[]);
  useEffect(()=>{const c=new AbortController();Promise.all([curationService.tasks(c.signal),curationService.statistics(c.signal)]).then(([items,stats])=>{setTasks(items);setStatistics(stats);}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e));});return()=>c.abort();},[]);
  const open=async(task:CurationTask)=>{setError("");try{setSelected(await curationService.task(task.id));setValue(task.suggestedValue);setReason("");setCustomizing(false);}catch(e){setError(errorMessage(e));}};
  const decide=async(action:"accept"|"reject"|"customize")=>{if(!selected)return;setBusy(true);setError("");try{if(action==="accept")await curationService.accept(selected.task.id);else if(action==="reject")await curationService.reject(selected.task.id,reason);else await curationService.customize(selected.task.id,value,reason);setSelected(undefined);await load();}catch(e){setError(errorMessage(e));}finally{setBusy(false);}};
  const openTasks=tasks?.filter(x=>x.status==="Open")??[];
  return <section className="admin-page curation-page"><AdminPageHeader title="Datenkurationscenter" description="Technisch gültige Daten transparent fachlich kuratieren"/>{error&&<p className="form-error" role="alert">{error}</p>}
    <section className="maturity-summary" aria-label="Datenreife">{(["bronze","silver","gold"] as const).map(level=><article key={level} className={`maturity maturity--${level}`}><span>{level}</span><strong>{statistics?.[level]??"–"}</strong><small>{level==="bronze"?"Rohdaten":level==="silver"?"Technisch geprüft":"Fachlich kuratiert"}</small></article>)}</section>
    <div className="curation-layout"><section><div className="section-heading"><h2>Offene Aufgaben</h2><span>{statistics?.openTasks??0}</span></div>{!tasks?<PageState>Aufgaben werden geladen …</PageState>:openTasks.length===0?<PageState>Keine offenen Kurationsaufgaben.</PageState>:<ul className="curation-task-list">{openTasks.map(task=><li key={task.id}><button onClick={()=>void open(task)} className={selected?.task.id===task.id?"is-selected":""}><span><strong>{task.entityDisplayName}</strong><small>{fieldLabels[task.fieldName]??task.fieldName} · {entityLabels[task.entityType]??task.entityType}</small></span><b>{task.confidencePercent} %</b></button></li>)}</ul>}</section>
      <section className="curation-detail"><h2>Einzelprüfung</h2>{!selected?<PageState>Eine offene Aufgabe auswählen.</PageState>:<><div className="comparison"><article><small>Originaldaten</small><strong>{selected.task.originalValue??"Nicht vorhanden"}</strong><span>Unverändert gespeichert</span></article><article><small>ENSET Vorschlag</small><strong>{selected.task.suggestedValue}</strong><span className="confidence">{selected.task.confidencePercent} % Konfidenz</span></article></div><div className="reasoning"><h3>Begründung</h3><p>{selected.task.reasoning}</p></div>{customizing&&<div className="customize-fields"><label>Kuratierter Wert<input value={value} onChange={e=>setValue(e.target.value)}/></label><label>Begründung<textarea value={reason} onChange={e=>setReason(e.target.value)}/></label></div>}<div className="curation-actions">{customizing?<><button onClick={()=>setCustomizing(false)} disabled={busy}>Abbrechen</button><button className="primary-button" onClick={()=>void decide("customize")} disabled={busy||!value.trim()}>Individuellen Wert übernehmen</button></>:<><button className="primary-button" onClick={()=>void decide("accept")} disabled={busy}>Übernehmen</button><button onClick={()=>setCustomizing(true)} disabled={busy}>Bearbeiten</button><button className="danger-button" onClick={()=>void decide("reject")} disabled={busy}>Ablehnen</button></>}</div></>}</section></div>
    {statistics&&statistics.taskGroups.length>0&&<section className="detail-section"><h2>Aufgabenübersicht</h2><div className="task-groups">{statistics.taskGroups.map(group=><p key={`${group.entityType}-${group.fieldName}`}><strong>{group.count}</strong> {entityLabels[group.entityType]??group.entityType} ohne {fieldLabels[group.fieldName]??group.fieldName}</p>)}</div></section>}
  </section>;
}
