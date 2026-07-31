import { Link } from "react-router";

export function NotFoundPage() {
  return (
    <section>
      <h1>Seite nicht gefunden</h1>
      <p>Die angeforderte Seite existiert nicht.</p>
      <Link to="/">Zum Dashboard</Link>
    </section>
  );
}