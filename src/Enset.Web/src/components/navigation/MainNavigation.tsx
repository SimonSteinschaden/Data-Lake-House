import { NavLink } from "react-router";
import "./MainNavigation.css";

interface NavigationItem {
  path: string;
  label: string;
  end?: boolean;
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
  },
  {
    path: "/buildings",
    label: "Gebäude",
  },
  {
    path: "/analytics",
    label: "Analysen",
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
        {navigationItems.map((item) => (
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