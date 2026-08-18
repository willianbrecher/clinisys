import client from "./client";
import type { PagedResult, PatientModel } from "./types";

export const getPatients = (params: { search?: string; page?: number; pageSize?: number }) =>
  client.get<PagedResult<PatientModel>>("/api/patients", { params }).then((r) => r.data);

export const getPatientById = (id: string) =>
  client.get<PatientModel>(`/api/patients/${id}`).then((r) => r.data);

export const createPatient = (data: {
  fullName: string; dateOfBirth: string; phone: string; email?: string; notes?: string;
  healthPlanId?: string; healthPlanNumber?: string;
}) => client.post<{ id: string }>("/api/patients", data).then((r) => r.data.id);

export const updatePatient = (id: string, data: {
  fullName: string; dateOfBirth: string; phone: string; email?: string; notes?: string;
  healthPlanId?: string; healthPlanNumber?: string;
}) => client.put(`/api/patients/${id}`, data);
