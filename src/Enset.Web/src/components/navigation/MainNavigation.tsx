import { NavLink } from "react-router";
import "./MainNavigation.css";
import { navigationVisibility } from "../../auth/navigationVisibility";

interface NavigationItem {
  path: string;
  label: string;
  end?: boolean;
  internalCustomerAdministration?: boolean;
}

const navigationItems: NavigationItem[] = [
  {
    path: "/",
    label: "Dashboard",
    end: true,
  },
  {
    path: "/imports",
    label: "Import",
  },
  {
    path: "/customers",
    label: "Kunden",
    internalCustomerAdministration: true,
  },
  {
    path: "/buildings",
    label: "Gebäude",
  },
  {
    path: "/meters",
    label: "Zähler",
  },
  {
    path: "/analytics",
    label: "Analysen",
  },
  {
    path: "/data-products",
    label: "Data Products",
  },
  {
    path: "/settings",
    label: "Einstellungen",
  },
];

export function MainNavigation() {
  return (
    <nav
      className="main-navigation"
      aria-label="Hauptnavigation"
    >
      <ul className="main-navigation__list">
        {navigationItems.filter(item => !item.internalCustomerAdministration || navigationVisibility.showInternalCustomerAdministration).map((item) => (
          <li
            key={item.path}
            className="main-navigation__item"
          >
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
          </li>
        ))}
      </ul>
    </nav>
  );
}
