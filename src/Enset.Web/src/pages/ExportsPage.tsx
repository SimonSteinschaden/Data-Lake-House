import { useState } from "react";
import { Button } from "../components/ui/Button";
import { Card } from "../components/ui/Card";
import { PageHeader } from "../layouts/PageHeader";
import {
  LebExportValidationError,
  lebExportService,
  type LebExportValidationResult,
} from "../services/lebExportService";
import "./ExportsPage.css";

type ExportAction = "validate" | "csv" | "excel";

const plannedExports = [
  "Siemens Navigator (Direktübertragung)",
  "Data Space Connector",
  "Data Marketplace",
  "Benchmark Export",
  "CO₂-Bericht",
];

const exportDefinitions = [
  {
    format: "csv" as const,
    title: "LEB Export (CSV)",
    description:
      "Exportiert die Landesenergiebuchhaltung im CSV-Format.",
    output: "ZIP-Archiv",
    contents: [
      "Municipalities.csv",
      "Objects.csv",
      "Meters.csv",
      "Readings.csv",
      "EnergySystems.csv",
    ],
    downloadLabel: "CSV herunterladen",
  },
  {
    format: "excel" as const,
    title: "LEB Export (Excel)",
    description:
      "Exportiert die Landesenergiebuchhaltung als Excel-Arbeitsmappe.",
    output: "XLSX-Arbeitsmappe",
    contents: [
      "Municipalities",
      "Objects",
      "Meters",
      "Readings",
      "EnergySystems",
    ],
    downloadLabel: "Excel herunterladen",
  },
];

export function ExportsPage() {
  const [validation, setValidation] =
    useState<LebExportValidationResult | null>(null);
  const [activeAction, setActiveAction] =
    useState<ExportAction | null>(null);
  const [requestError, setRequestError] = useState<string | null>(null);

  const exportBlocked = validation?.canExport === false;

  async function validate() {
    setActiveAction("validate");
    setRequestError(null);
    try {
      setValidation(await lebExportService.validate());
    } catch (error) {
      setRequestError(getErrorMessage(error));
    } finally {
      setActiveAction(null);
    }
  }

  async function download(format: "csv" | "excel") {
    setActiveAction(format);
    setRequestError(null);
    try {
      if (format === "csv") {
        await lebExportService.downloadCsv();
      } else {
        await lebExportService.downloadExcel();
      }
    } catch (error) {
      if (error instanceof LebExportValidationError) {
        setValidation(error.validation);
      } else {
        setRequestError(getErrorMessage(error));
      }
    } finally {
      setActiveAction(null);
    }
  }

  return (
    <section className="exports-page">
      <PageHeader
        title="Exporte"
        description="Exportieren Sie qualitätsgesicherte Daten für externe Systeme."
      />

      {requestError && (
        <div className="exports-page__request-error" role="alert">
          {requestError}
        </div>
      )}

      <div className="exports-page__grid">
        {exportDefinitions.map((definition) => (
          <Card
            key={definition.format}
            title={definition.title}
            description={definition.description}
            className="export-card"
            footer={
              <>
                <Button
                  variant="secondary"
                  onClick={validate}
                  loading={activeAction === "validate"}
                  disabled={activeAction !== null}
                >
                  Validieren
                </Button>
                <Button
                  onClick={() => download(definition.format)}
                  loading={activeAction === definition.format}
                  disabled={activeAction !== null || exportBlocked}
                >
                  {definition.downloadLabel}
                </Button>
              </>
            }
          >
            <dl className="export-card__metadata">
              <div>
                <dt>Ausgabeformat</dt>
                <dd>{definition.output}</dd>
              </div>
              <div>
                <dt>Zielsystem</dt>
                <dd>Landesenergiebuchhaltung Niederösterreich</dd>
              </div>
              <div>
                <dt>Exportvertrag</dt>
                <dd>NoeLebExportContractV1</dd>
              </div>
            </dl>

            <div className="export-card__contents">
              <h3>
                {definition.format === "csv"
                  ? "Enthaltene Dateien"
                  : "Arbeitsblätter"}
              </h3>
              <ul>
                {definition.contents.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>

            <ValidationStatus validation={validation} />
          </Card>
        ))}
      </div>

      <section
        className="exports-page__planned"
        aria-labelledby="planned-exports-title"
      >
        <div className="exports-page__section-heading">
          <h2 id="planned-exports-title">Geplante Exportformate</h2>
          <p>
            Diese Formate sind vorgemerkt und derzeit noch nicht verfügbar.
          </p>
        </div>
        <div className="exports-page__planned-grid">
          {plannedExports.map((title) => (
            <article
              className="planned-export-card"
              aria-disabled="true"
              key={title}
            >
              <h3>{title}</h3>
              <span className="planned-export-card__badge">Geplant</span>
            </article>
          ))}
        </div>
      </section>
    </section>
  );
}

function ValidationStatus({
  validation,
}: {
  validation: LebExportValidationResult | null;
}) {
  if (!validation) {
    return (
      <div className="export-validation export-validation--unchecked">
        <strong>Validierungsstatus</strong>
        <span>Nicht geprüft</span>
      </div>
    );
  }

  const hasWarnings = validation.warnings.length > 0;
  const tone = !validation.canExport
    ? "error"
    : hasWarnings
      ? "warning"
      : "ready";
  const label = !validation.canExport
    ? "Export derzeit nicht möglich"
    : hasWarnings
      ? "Export mit Warnungen möglich"
      : "✓ Export bereit";

  return (
    <div
      className={`export-validation export-validation--${tone}`}
      role={!validation.canExport ? "alert" : "status"}
    >
      <strong>{label}</strong>

      {validation.errors.length > 0 && (
        <ValidationList
          title="Blockierende Fehler"
          items={validation.errors}
        />
      )}
      {validation.warnings.length > 0 && (
        <ValidationList title="Warnungen" items={validation.warnings} />
      )}
    </div>
  );
}

function ValidationList({
  title,
  items,
}: {
  title: string;
  items: LebExportValidationResult["errors"];
}) {
  return (
    <div className="export-validation__details">
      <h3>{title}</h3>
      <ul>
        {items.map((item, index) => (
          <li key={`${item.code}-${item.rowId ?? "global"}-${index}`}>
            {item.message}
            <span>
              {item.table} · {item.field}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error
    ? error.message
    : "Die Exportanfrage ist fehlgeschlagen.";
}
