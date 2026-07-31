import type { ReactNode } from "react";
import "./StatCard.css";

interface StatCardProps {
  title: string;
  value: ReactNode;
  subtitle?: string;
  trend?: ReactNode;
  icon?: ReactNode;
}

export function StatCard({
  title,
  value,
  subtitle,
  trend,
  icon,
}: StatCardProps) {
  return (
    <section className="stat-card">
      <header className="stat-card__header">
        <span className="stat-card__title">
          {title}
        </span>

        {icon && (
          <span className="stat-card__icon">
            {icon}
          </span>
        )}
      </header>

      <div className="stat-card__value">
        {value}
      </div>

      {subtitle && (
        <div className="stat-card__subtitle">
          {subtitle}
        </div>
      )}

      {trend && (
        <footer className="stat-card__footer">
          {trend}
        </footer>
      )}
    </section>
  );
}