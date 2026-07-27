import { NavLink } from "react-router";
import { navigationVisibility } from "../../auth/navigationVisibility";
import "./MainNavigation.css";

interface NavigationItem {
  path?: string;
  label: string;
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
        label: "Zählpunkte",
      },
      {
        path: "/metering-points",
        label: "Zähler",
        disabled: true, //TODO: which information should be displayed here? This is a new concept in the energy sector, but not yet implemented in the system.
      },
      {
        path: "/documents",
        label: "Dokumente", //TODO: which documents, just Dokumentenliste?
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
        label: "Reports",
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
        label: "Data Products",
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
        path: "/tools/curation",
        label: "Datenkurationscenter",
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
      },
      {
        path: "/settings",
        label: "Einstellungen",
      },
      {
        path: "/admin/system",
        label: "System",
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
                      {item.label}
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
