import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMetricSummary, type MetricSummary } from "./metricsService";

const emptySummary: MetricSummary = {
  totalRequests: 0,
  ragRequests: 0,
  toolRequests: 0,
  errorCount: 0,
  averageResponseTimeMs: 0,
  totalApproxTokens: 0,
  totalChunksUsed: 0,
  routes: [],
  recent: [],
};

function formatDate(value: string) {
  return new Intl.DateTimeFormat("es-AR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}

export default function MetricsPage() {
  const [summary, setSummary] = useState<MetricSummary>(emptySummary);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getMetricSummary()
      .then((data) => {
        setSummary(data);
        setError(null);
      })
      .catch((requestError) => {
        console.error("Error cargando métricas:", requestError);
        setError("No se pudieron cargar las métricas.");
      })
      .finally(() => setIsLoading(false));
  }, []);

  const cards = [
    { label: "Requests", value: summary.totalRequests },
    { label: "Usaron RAG", value: summary.ragRequests },
    { label: "Usaron tool", value: summary.toolRequests },
    { label: "Errores", value: summary.errorCount },
    { label: "Promedio ms", value: summary.averageResponseTimeMs },
    { label: "Tokens aprox.", value: summary.totalApproxTokens },
    { label: "Chunks usados", value: summary.totalChunksUsed },
  ];

  return (
    <div className="min-h-screen bg-slate-950 p-6 text-white">
      <div className="mx-auto max-w-6xl space-y-6">
        <header className="flex flex-col gap-4 rounded-2xl border border-slate-800 bg-slate-900 p-6 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-sm uppercase tracking-[0.3em] text-blue-300">
              LocalMind AI
            </p>
            <h1 className="mt-2 text-3xl font-bold">Panel de métricas</h1>
            <p className="mt-2 text-sm text-slate-400">
              Seguimiento simple de modelo, latencia, tokens aproximados, RAG,
              tools, chunks y errores.
            </p>
          </div>

          <Link
            to="/chat"
            className="rounded-xl bg-blue-600 px-4 py-2 text-center font-semibold hover:bg-blue-700"
          >
            Volver al chat
          </Link>
        </header>

        {error && (
          <div className="rounded-xl border border-red-500/40 bg-red-500/10 p-4 text-red-200">
            {error}
          </div>
        )}

        {isLoading ? (
          <div className="rounded-xl border border-slate-800 bg-slate-900 p-6 text-slate-400">
            Cargando métricas...
          </div>
        ) : (
          <>
            <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              {cards.map((card) => (
                <article
                  key={card.label}
                  className="rounded-2xl border border-slate-800 bg-slate-900 p-5"
                >
                  <p className="text-sm text-slate-400">{card.label}</p>
                  <p className="mt-2 text-3xl font-bold">{card.value}</p>
                </article>
              ))}
            </section>

            <section className="grid gap-6 lg:grid-cols-[1fr_2fr]">
              <article className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
                <h2 className="text-lg font-semibold">Rutas usadas</h2>
                <div className="mt-4 space-y-3">
                  {summary.routes.length === 0 ? (
                    <p className="text-sm text-slate-500">Sin datos aún.</p>
                  ) : (
                    summary.routes.map((route) => (
                      <div
                        key={route.route}
                        className="flex items-center justify-between rounded-xl bg-slate-950 p-3"
                      >
                        <span className="font-medium">{route.route}</span>
                        <span className="rounded-full bg-blue-500/10 px-3 py-1 text-sm text-blue-200">
                          {route.count}
                        </span>
                      </div>
                    ))
                  )}
                </div>
              </article>

              <article className="overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
                <div className="border-b border-slate-800 p-5">
                  <h2 className="text-lg font-semibold">Últimas ejecuciones</h2>
                </div>

                <div className="overflow-x-auto">
                  <table className="w-full min-w-[760px] text-left text-sm">
                    <thead className="bg-slate-950 text-slate-400">
                      <tr>
                        <th className="p-3">Fecha</th>
                        <th className="p-3">Modelo</th>
                        <th className="p-3">Ruta</th>
                        <th className="p-3">ms</th>
                        <th className="p-3">Tokens</th>
                        <th className="p-3">RAG</th>
                        <th className="p-3">Tool</th>
                        <th className="p-3">Chunks</th>
                        <th className="p-3">Error</th>
                      </tr>
                    </thead>
                    <tbody>
                      {summary.recent.length === 0 ? (
                        <tr>
                          <td className="p-4 text-slate-500" colSpan={9}>
                            Sin métricas registradas todavía.
                          </td>
                        </tr>
                      ) : (
                        summary.recent.map((metric) => (
                          <tr key={metric.id} className="border-t border-slate-800">
                            <td className="p-3 text-slate-300">
                              {formatDate(metric.createdAt)}
                            </td>
                            <td className="p-3 text-slate-300">{metric.modelUsed}</td>
                            <td className="p-3">
                              <span className="rounded-full bg-indigo-500/10 px-2 py-1 text-indigo-200">
                                {metric.route}
                              </span>
                            </td>
                            <td className="p-3">{metric.responseTimeMs}</td>
                            <td className="p-3">{metric.approxTokens}</td>
                            <td className="p-3">{metric.usedRag ? "sí" : "no"}</td>
                            <td className="p-3">
                              {metric.usedTool ? metric.toolName ?? "sí" : "no"}
                            </td>
                            <td className="p-3">{metric.chunksUsed}</td>
                            <td className="max-w-xs truncate p-3 text-red-300">
                              {metric.error ?? "-"}
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </article>
            </section>
          </>
        )}
      </div>
    </div>
  );
}
