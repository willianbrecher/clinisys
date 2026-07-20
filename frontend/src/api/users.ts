import client from "./client";
import type { PagedResult, UserModel, Role } from "./types";

export const getUsers = (params?: { page?: number; pageSize?: number }) =>
  client.get<PagedResult<UserModel>>("/api/users", { params }).then((r) => r.data);

export const createUser = (data: {
  email: string; fullName: string; password: string; role: Role; specialty?: string;
}) => client.post<{ id: string }>("/api/users", data).then((r) => r.data.id);

export const deactivateUser = (id: string) =>
  client.patch(`/api/users/${id}/deactivate`);

export const resetPassword = (id: string, newPassword: string) =>
  client.post(`/api/users/${id}/reset-password`, { newPassword });
