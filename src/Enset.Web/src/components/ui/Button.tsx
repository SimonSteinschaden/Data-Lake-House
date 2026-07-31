import type {
  ButtonHTMLAttributes,
  ReactNode,
} from "react";
import "./Button.css";

export type ButtonVariant =
  | "primary"
  | "secondary"
  | "ghost"
  | "danger";

interface ButtonProps
  extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  icon?: ReactNode;
  loading?: boolean;
  fullWidth?: boolean;
}

export function Button({
  variant = "primary",
  icon,
  loading = false,
  fullWidth = false,
  disabled,
  className,
  children,
  type = "button",
  ...props
}: ButtonProps) {
  const classes = [
    "button",
    `button--${variant}`,
    fullWidth ? "button--full-width" : "",
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  const isDisabled = disabled || loading;

  return (
    <button
      {...props}
      type={type}
      className={classes}
      disabled={isDisabled}
      aria-busy={loading || undefined}
    >
      {loading && (
        <span
          className="button__spinner"
          aria-hidden="true"
        />
      )}

      {!loading && icon && (
        <span
          className="button__icon"
          aria-hidden="true"
        >
          {icon}
        </span>
      )}

      <span className="button__label">
        {loading ? "Wird verarbeitet …" : children}
      </span>
    </button>
  );
}