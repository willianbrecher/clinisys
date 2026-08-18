import client from "./client";
import type { PagedResult, HealthPlanModel } from "./types";

export const getHealthPlans = (params: { search?: string; page?: number; pageSize?: number }) =>
  client.get<PagedResult<HealthPlanModel>>("/api/health-plans", { params }).then((r) => r.data);

export const getHealthPlanById = (id: string) =>
  client.get<HealthPlanModel>(`/api/health-plans/${id}`).then((r) => r.data);

export const createHealthPlan = (data: { name: string; notes?: string }) =>
  client.post<{ id: string }>("/api/health-plans", data).then((r) => r.data.id);

export const updateHealthPlan = (id: string, data: { name: string; notes?: string }) =>
  client.put(`/api/health-plans/${id}`, data);
