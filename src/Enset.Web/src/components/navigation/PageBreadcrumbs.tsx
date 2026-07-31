import { Link, useLocation } from "react-router";
import "./PageBreadcrumbs.css";

interface BreadcrumbItem {
  label: string;
  path?: string;
}

const routeLabels: Record<string, string> = {
  imports: "Importe",
  exports: "Exporte",
  customers: "Kunden",
  buildings: "Gebäude",
  meters: "Zähler",
  "metering-points": "Zählpunkte",
  documents: "Dokumente",
  analysis: "Analyse",
  object: "Objektanalyse",
  reports: "Berichte",
  "data-products": "Datenprodukte",
  tools: "Werkzeuge",
  "data-quality": "Datenqualität",
  assignments: "Zuordnungen",
  admin: "Administration",
  users: "Benutzer",
  system: "System",
  settings: "Einstellungen",
  energy: "Energie",
};

function isIdentifier(segment: string) {
  return (
    /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
      segment,
    ) || /^\d+$/.test(segment)
  );
}

function createBreadcrumbs(pathname: string): BreadcrumbItem[] {
  const segments = pathname
    .split("/")
    .filter(Boolean);

  if (segments.length === 0) {
    return [
      {
        label: "Management-Dashboard",
      },
    ];
  }

  const breadcrumbs: BreadcrumbItem[] = [
    {
      label: "Management-Dashboard",
      path: "/",
    },
  ];

  let currentPath = "";

  for (const segment of segments) {
    currentPath += `/${segment}`;

    breadcrumbs.push({
      label: isIdentifier(segment)
        ? "Details"
        : routeLabels[segment] ?? segment,
      path: currentPath,
    });
  }

  return breadcrumbs;
}

export function PageBreadcrumbs() {
  const location = useLocation();
  const breadcrumbs = createBreadcrumbs(location.pathname);

  return (
    <nav
      className="page-breadcrumbs"
      aria-label="Brotkrümelnavigation"
    >
      <ol className="page-breadcrumbs__list">
        {breadcrumbs.map((item, index) => {
          const isLast = index === breadcrumbs.length - 1;

          return (
            <li
              key={`${item.label}-${index}`}
              className="page-breadcrumbs__item"
            >
              {index > 0 && (
                <span
                  className="page-breadcrumbs__separator"
                  aria-hidden="true"
                >
                  /
                </span>
              )}

              {item.path && !isLast ? (
                <Link
                  to={item.path}
                  className="page-breadcrumbs__link"
                >
                  {item.label}
                </Link>
              ) : (
                <span
                  className={
                    isLast
                      ? "page-breadcrumbs__current"
                      : undefined
                  }
                  aria-current={isLast ? "page" : undefined}
                >
                  {item.label}
                </span>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
