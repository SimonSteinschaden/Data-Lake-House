import type { HTMLAttributes, ReactNode } from "react";
import "./Card.css";

interface CardProps extends HTMLAttributes<HTMLElement> {
  title?: string;
  description?: string;
  actions?: ReactNode;
  footer?: ReactNode;
}

export function Card({
  title,
  description,
  actions,
  footer,
  className,
  children,
  ...props
}: CardProps) {
  const classes = ["card", className ?? ""]
    .filter(Boolean)
    .join(" ");

  return (
    <section className={classes} {...props}>
      {(title || description || actions) && (
        <header className="card__header">
          <div className="card__heading">
            {title && (
              <h2 className="card__title">{title}</h2>
            )}

            {description && (
              <p className="card__description">
                {description}
              </p>
            )}
          </div>

          {actions && (
            <div className="card__actions">
              {actions}
            </div>
          )}
        </header>
      )}

      <div className="card__content">
        {children}
      </div>

      {footer && (
        <footer className="card__footer">
          {footer}
        </footer>
      )}
    </section>
  );
}