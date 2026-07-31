import type { ReactNode } from "react";
import "./PageHeader.css";

interface PageHeaderProps {
  title: string;
  description?: string;
  breadcrumbs?: ReactNode;
  actions?: ReactNode;
  children?: ReactNode;
}

export function PageHeader({
  title,
  description,
  breadcrumbs,
  actions,
  children,
}: PageHeaderProps) {
  return (
    <header className="page-header">

      {breadcrumbs && (
        <div className="page-header__breadcrumbs">
          {breadcrumbs}
        </div>
      )}

      <div className="page-header__top">

        <div className="page-header__content">
          <h1 className="page-header__title">
            {title}
          </h1>

          {description && (
            <p className="page-header__description">
              {description}
            </p>
          )}
        </div>

        {actions && (
          <div className="page-header__actions">
            {actions}
          </div>
        )}

      </div>

      {children && (
        <div className="page-header__bottom">
          {children}
        </div>
      )}

    </header>
  );
}