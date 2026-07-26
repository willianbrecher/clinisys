import client from "./client";
import type { Role, ThemePreference } from "./types";

export interface MeResponse {
  userId: string;
  role: Role;
  fullName: string;
  theme: ThemePreference;
  language: string;
  doctorId?: string | null;
}

export const getMe = () =>
  client.get<MeResponse>("/api/account/me").then(r => r.data);

export const updateProfilePicture = (profilePictureBase64: string | null) =>
  client.patch("/api/account/profile-picture", { profilePictureBase64 });

export const updatePreferences = (theme: ThemePreference, language: string) =>
  client.patch("/api/account/preferences", { theme, language });
