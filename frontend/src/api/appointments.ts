import client from "./client";
import type { PagedResult, AppointmentModel, AppointmentStatus } from "./types";

export interface AppointmentFilters {
  doctorId?: string;
  patientId?: string;
  date?: string;
  startDate?: string;
  endDate?: string;
  status?: AppointmentStatus;
  page?: number;
  pageSize?: number;
}

export const getAppointments = (params: AppointmentFilters) =>
  client.get<PagedResult<AppointmentModel>>("/api/appointments", { params }).then((r) => r.data);

export const createAppointment = (data: {
  patientId: string; doctorId: string; startsAt: string;
  durationMinutes: number; notes?: string;
}) => client.post<{ id: string }>("/api/appointments", data).then((r) => r.data.id);

export const rescheduleAppointment = (id: string, data: {
  startsAt: string; durationMinutes: number; notes?: string;
}) => client.put(`/api/appointments/${id}`, data);

export const updateAppointmentStatus = (id: string, status: AppointmentStatus) =>
  client.patch(`/api/appointments/${id}/status`, { status });
