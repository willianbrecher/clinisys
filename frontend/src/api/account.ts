import client from "./client";
import type { ThemePreference } from "./types";

export const updateProfilePicture = (profilePictureBase64: string | null) =>
  client.patch("/api/account/profile-picture", { profilePictureBase64 });

export const updatePreferences = (theme: ThemePreference, language: string) =>
  client.patch("/api/account/preferences", { theme, language });
