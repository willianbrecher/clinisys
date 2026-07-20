import client from "./client";
import type { ClinicSettingsModel } from "./types";

export const getClinicSettings = () =>
  client.get<ClinicSettingsModel>("/api/clinic-settings").then((r) => r.data);

export const updateClinicSettings = (data: {
  openTime: string; closeTime: string; openDays: string; logoBase64?: string | null;
}) => client.put("/api/clinic-settings", data);
