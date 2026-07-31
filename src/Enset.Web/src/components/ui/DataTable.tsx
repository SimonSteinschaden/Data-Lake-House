import {
  useMemo,
  useState,
} from "react";
import type {
  ReactNode,
} from "react";
import "./DataTable.css";

export type SortDirection = "asc" | "desc";

export interface DataTableColumn<T> {
  key: string;
  header: string;
  width?: string;
  sortable?: boolean;
  align?: "left" | "center" | "right";
  getValue?: (row: T) => unknown;
  render?: (row: T) => ReactNode;
}

interface DataTableProps<T> {
  rows: T[];
  columns: DataTableColumn<T>[];
  getRowId: (row: T) => string;
  loading?: boolean;
  searchable?: boolean;
  searchPlaceholder?: string;
  emptyTitle?: string;
  emptyDescription?: string;
  pageSize?: number;
  onRowClick?: (row: T) => void;
}

function normalizeValue(value: unknown): string {
  if (value === null || value === undefined) {
    return "";
  }

  if (value instanceof Date) {
    return value.toISOString();
  }

  return String(value);
}

export function DataTable<T>({
  rows,
  columns,
  getRowId,
  loading = false,
  searchable = true,
  searchPlaceholder = "Suchen …",
  emptyTitle = "Keine Einträge vorhanden",
  emptyDescription = "Für diese Ansicht sind derzeit keine Daten vorhanden.",
  pageSize = 10,
  onRowClick,
}: DataTableProps<T>) {
  const [searchTerm, setSearchTerm] = useState("");
  const [sortKey, setSortKey] = useState<string | null>(null);
  const [sortDirection, setSortDirection] =
    useState<SortDirection>("asc");
  const [page, setPage] = useState(1);

  const filteredRows = useMemo(() => {
    const normalizedSearch = searchTerm
      .trim()
      .toLocaleLowerCase();

    if (!normalizedSearch) {
      return rows;
    }

    return rows.filter((row) =>
      columns.some((column) => {
        const value = column.getValue
          ? column.getValue(row)
          : undefined;

        return normalizeValue(value)
          .toLocaleLowerCase()
          .includes(normalizedSearch);
      }),
    );
  }, [rows, columns, searchTerm]);

  const sortedRows = useMemo(() => {
    if (!sortKey) {
      return filteredRows;
    }

    const column = columns.find(
      (item) => item.key === sortKey,
    );

    if (!column?.getValue) {
      return filteredRows;
    }

    return [...filteredRows].sort((left, right) => {
      const leftValue = column.getValue?.(left);
      const rightValue = column.getValue?.(right);

      if (
        typeof leftValue === "number" &&
        typeof rightValue === "number"
      ) {
        return sortDirection === "asc"
          ? leftValue - rightValue
          : rightValue - leftValue;
      }

      const comparison = normalizeValue(leftValue).localeCompare(
        normalizeValue(rightValue),
        "de",
        {
          numeric: true,
          sensitivity: "base",
        },
      );

      return sortDirection === "asc"
        ? comparison
        : -comparison;
    });
  }, [
    filteredRows,
    columns,
    sortKey,
    sortDirection,
  ]);

  const totalPages = Math.max(
    1,
    Math.ceil(sortedRows.length / pageSize),
  );

  const currentPage = Math.min(page, totalPages);

  const visibleRows = useMemo(() => {
    const start = (currentPage - 1) * pageSize;

    return sortedRows.slice(
      start,
      start + pageSize,
    );
  }, [sortedRows, currentPage, pageSize]);

  function handleSort(column: DataTableColumn<T>) {
    if (!column.sortable || !column.getValue) {
      return;
    }

    if (sortKey === column.key) {
      setSortDirection((current) =>
        current === "asc" ? "desc" : "asc",
      );
    } else {
      setSortKey(column.key);
      setSortDirection("asc");
    }

    setPage(1);
  }

  function handleSearch(value: string) {
    setSearchTerm(value);
    setPage(1);
  }

  return (
    <section className="data-table">
      {searchable && (
        <div className="data-table__toolbar">
          <input
            type="search"
            value={searchTerm}
            onChange={(event) =>
              handleSearch(event.target.value)
            }
            className="data-table__search"
            placeholder={searchPlaceholder}
            aria-label={searchPlaceholder}
          />

          <span className="data-table__result-count">
            {filteredRows.length} Einträge
          </span>
        </div>
      )}

      <div className="data-table__container">
        <table className="data-table__table">
          <thead className="data-table__head">
            <tr>
              {columns.map((column) => {
                const isSorted =
                  sortKey === column.key;

                return (
                  <th
                    key={column.key}
                    scope="col"
                    className={`data-table__header data-table__header--${column.align ?? "left"}`}
                    style={{
                      width: column.width,
                    }}
                  >
                    {column.sortable ? (
                      <button
                        type="button"
                        className="data-table__sort-button"
                        onClick={() =>
                          handleSort(column)
                        }
                      >
                        <span>{column.header}</span>

                        <span
                          className="data-table__sort-indicator"
                          aria-hidden="true"
                        >
                          {isSorted
                            ? sortDirection === "asc"
                              ? "▲"
                              : "▼"
                            : "↕"}
                        </span>
                      </button>
                    ) : (
                      column.header
                    )}
                  </th>
                );
              })}
            </tr>
          </thead>

          <tbody className="data-table__body">
            {loading &&
              Array.from({ length: 5 }).map(
                (_, rowIndex) => (
                  <tr
                    key={`loading-${rowIndex}`}
                    className="data-table__row"
                  >
                    {columns.map((column) => (
                      <td
                        key={column.key}
                        className="data-table__cell"
                      >
                        <span className="data-table__skeleton" />
                      </td>
                    ))}
                  </tr>
                ),
              )}

            {!loading &&
              visibleRows.map((row) => (
                <tr
                  key={getRowId(row)}
                  className={[
                    "data-table__row",
                    onRowClick
                      ? "data-table__row--clickable"
                      : "",
                  ]
                    .filter(Boolean)
                    .join(" ")}
                  onClick={() =>
                    onRowClick?.(row)
                  }
                  tabIndex={
                    onRowClick ? 0 : undefined
                  }
                  onKeyDown={(event) => {
                    if (
                      onRowClick &&
                      (event.key === "Enter" ||
                        event.key === " ")
                    ) {
                      event.preventDefault();
                      onRowClick(row);
                    }
                  }}
                >
                  {columns.map((column) => (
                    <td
                      key={column.key}
                      className={`data-table__cell data-table__cell--${column.align ?? "left"}`}
                    >
                      {column.render
                        ? column.render(row)
                        : normalizeValue(
                            column.getValue?.(row),
                          )}
                    </td>
                  ))}
                </tr>
              ))}
          </tbody>
        </table>

        {!loading && visibleRows.length === 0 && (
          <div className="data-table__empty">
            <h3 className="data-table__empty-title">
              {emptyTitle}
            </h3>

            <p className="data-table__empty-description">
              {searchTerm
                ? "Für den aktuellen Suchbegriff wurden keine Treffer gefunden."
                : emptyDescription}
            </p>
          </div>
        )}
      </div>

      {!loading && sortedRows.length > 0 && (
        <footer className="data-table__pagination">
          <span className="data-table__pagination-info">
            Seite {currentPage} von {totalPages}
          </span>

          <div className="data-table__pagination-actions">
            <button
              type="button"
              className="data-table__pagination-button"
              disabled={currentPage === 1}
              onClick={() =>
                setPage((current) =>
                  Math.max(1, current - 1),
                )
              }
            >
              Zurück
            </button>

            <button
              type="button"
              className="data-table__pagination-button"
              disabled={
                currentPage === totalPages
              }
              onClick={() =>
                setPage((current) =>
                  Math.min(
                    totalPages,
                    current + 1,
                  ),
                )
              }
            >
              Weiter
            </button>
          </div>
        </footer>
      )}
    </section>
  );
}