import { api } from "../../services/api";

export interface RouteMetric {
  route: string;
  count: number;
}

export interface RecentMetric {
  id: number;
  modelUsed: string;
  responseTimeMs: number;
  approxTokens: number;
  usedRag: boolean;
  usedTool: boolean;
  toolName: string | null;
  chunksUsed: number;
  route: string;
  error: string | null;
  createdAt: string;
}

export interface MetricSummary {
  totalRequests: number;
  ragRequests: number;
  toolRequests: number;
  errorCount: number;
  averageResponseTimeMs: number;
  totalApproxTokens: number;
  totalChunksUsed: number;
  routes: RouteMetric[];
  recent: RecentMetric[];
}

export const getMetricSummary = async (): Promise<MetricSummary> => {
  const response = await api.get<MetricSummary>("/metrics/summary");
  return response.data;
};
