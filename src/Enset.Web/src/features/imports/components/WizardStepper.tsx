import type { ImportWizardStep } from "../types/ImportWizardStep";

interface WizardStepperProps {
  currentStep: ImportWizardStep;
}

interface StepDefinition {
  key: Exclude<ImportWizardStep, "completed">;
  label: string;
}

const steps: StepDefinition[] = [
  { key: "upload", label: "Datei" },
  { key: "analysis", label: "Analyse" },
  { key: "resolution", label: "Entscheidungen" },
  { key: "commit", label: "Commit" },
];

export function WizardStepper({
  currentStep,
}: WizardStepperProps) {
  const currentIndex =
    currentStep === "completed"
      ? steps.length
      : steps.findIndex((step) => step.key === currentStep);

  return (
    <ol
      className="import-wizard__steps"
      aria-label="Importfortschritt"
    >
      {steps.map((step, index) => {
        const isActive = step.key === currentStep;
        const isCompleted = index < currentIndex;

        const classNames = [
          "import-wizard__step",
          isActive ? "import-wizard__step--active" : "",
          isCompleted ? "import-wizard__step--completed" : "",
        ]
          .filter(Boolean)
          .join(" ");

        return (
          <li
            key={step.key}
            className={classNames}
            aria-current={isActive ? "step" : undefined}
          >
            <span className="import-wizard__step-marker">
              {isCompleted ? "✓" : index + 1}
            </span>

            <span>{step.label}</span>
          </li>
        );
      })}
    </ol>
  );
}