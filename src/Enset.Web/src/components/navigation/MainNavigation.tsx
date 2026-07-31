import { NavLink } from "react-router";
import { navigationVisibility } from "../../auth/navigationVisibility";
import "./MainNavigation.css";

interface NavigationItem {
  path?: string;
  label: string;
  icon?: "export";
  end?: boolean;
  disabled?: boolean;
  internalCustomerAdministration?: boolean;
  adminOnly?: boolean;
}

interface NavigationGroup {
  label?: string;
  items: NavigationItem[];
  adminOnly?: boolean;
}

const isAdmin = true;

const navigationGroups: NavigationGroup[] = [
  {
    items: [
      {
        path: "/",
        label: "Dashboard",
        end: true,
      },
    ],
  },
  {
    label: "Daten",
    items: [
      {
        path: "/imports",
        label: "Importe",
      },
      {
        path: "/exports",
        label: "Exporte",
        icon: "export",
      },
      {
        path: "/customers",
        label: "Kunden",
        //TODO: internalCustomerAdministration: true, Customers can only see their own products.
      },
      {
        path: "/buildings",
        label: "Gebäude",
        //TODO: internalCustomerAdministration: true, Customers can only see their own products.
      },
      {
        path: "/meters",
        label: "Zähler",
      },
      {
        path: "/metering-points",
        label: "Messwerte",
        disabled: true, //TODO: which information should be displayed here, without overwhelming loadprofiles?
      },
      {
        path: "/documents",
        label: "Dokumente", //TODO: which documents, just document list?
        disabled: true, //TODO:  not yet implemented in the system.
      },
    ],
  },
  {
    label: "Analyse",
    items: [
      {
        path: "/analysis/object",
        label: "Objektanalyse",
      },
      {
        path: "/reports",
        label: "Berichte",
      },
      {
        path: "/financials",
        label: "Wirtschaftlichkeitsanalyse",
        disabled: true,
      },
      {
        path: "/emissions",
        label: "Emissionsanalyse",
        disabled: true,
      },
      {
        path: "/data-products",
        label: "Datenprodukte",
      },
    ],
  },
  {
    label: "Werkzeuge",
    items: [
      {
        path: "/tools/data-quality",
        label: "Datenqualität",
      },
      {
        path: "/tools/data-review",
        label: "Datenprüfung",
      },
      {
        path: "/tools/assignments",
        label: "Zuordnungen",
      },
    ],
  },
  {
    label: "Administration",
    adminOnly: true,
    items: [
      {
        path: "/admin/users",
        label: "Benutzer",
        disabled: true,
      },
      {
        path: "/settings",
        label: "Einstellungen",
        disabled: true,
      },
      {
        path: "/admin/system",
        label: "System",
        disabled: true,
      },
    ],
  },
];

function isItemVisible(item: NavigationItem) {
  if (
    item.internalCustomerAdministration &&
    !navigationVisibility.showInternalCustomerAdministration
  ) {
    return false;
  }

  if (item.adminOnly && !isAdmin) {
    return false;
  }

  return true;
}

export function MainNavigation() {
  const visibleGroups = navigationGroups.filter(
    (group) => !group.adminOnly || isAdmin,
  );

  return (
    <nav
      className="main-navigation"
      aria-label="Hauptnavigation"
    >
      {visibleGroups.map((group, groupIndex) => {
        const visibleItems = group.items.filter(isItemVisible);

        if (visibleItems.length === 0) {
          return null;
        }

        return (
          <section
            key={group.label ?? `navigation-group-${groupIndex}`}
            className="main-navigation__group"
          >
            {group.label && (
              <h2 className="main-navigation__group-title">
                {group.label}
              </h2>
            )}

            <ul className="main-navigation__list">
              {visibleItems.map((item) => (
                <li
                  key={item.path ?? item.label}
                  className="main-navigation__item"
                >
                  {item.disabled || !item.path ? (
                    <span
                      className="main-navigation__link main-navigation__link--disabled"
                      aria-disabled="true"
                    >
                      <span>{item.label}</span>

                      <span className="main-navigation__status">
                        Bald
                      </span>
                    </span>
                  ) : (
                    <NavLink
                      to={item.path}
                      end={item.end}
                      className={({ isActive }) =>
                        isActive
                          ? "main-navigation__link main-navigation__link--active"
                          : "main-navigation__link"
                      }
                    >
                      <span className="main-navigation__link-label">
                        {item.icon === "export" && <ExportIcon />}
                        <span>{item.label}</span>
                      </span>
                    </NavLink>
                  )}
                </li>
              ))}
            </ul>
          </section>
        );
      })}
    </nav>
  );
}

function ExportIcon() {
  return (
    <svg
      className="main-navigation__icon"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      aria-hidden="true"
    >
      <path d="M12 3v11m0 0 4-4m-4 4-4-4" />
      <path d="M5 14v5h14v-5" />
    </svg>
  );
}
