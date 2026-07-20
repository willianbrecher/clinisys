import client from "./client";
import type { PagedResult, DoctorModel } from "./types";

export const getDoctors = (params?: { page?: number; pageSize?: number }) =>
  client.get<PagedResult<DoctorModel>>("/api/doctors", { params }).then((r) => r.data);

export const getDoctorById = (id: string) =>
  client.get<DoctorModel>(`/api/doctors/${id}`).then((r) => r.data);

export const updateDoctor = (id: string, specialty: string) =>
  client.patch(`/api/doctors/${id}`, { specialty });
