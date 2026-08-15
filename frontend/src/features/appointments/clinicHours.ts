import type { ClinicSettingsModel } from "@/api/types";

export function deriveOpenDays(settings: ClinicSettingsModel | null): number[] {
  return settings?.openDays ? settings.openDays.split(",").map(Number) : [1, 2, 3, 4, 5];
}

/** True if `startsAt` (a "YYYY-MM-DDTHH:mm" local string) plus `durationMinutes` fits inside
 * `settings`'s open days/hours for that calendar day. Mirrors the backend's ValidateOpenHours. */
export function isWithinOpenHours(
  startsAt: string, durationMinutes: number, settings: ClinicSettingsModel | null,
): boolean {
  if (!settings || !startsAt) return true;
  const start = new Date(startsAt);
  if (!deriveOpenDays(settings).includes(start.getDay())) return false;

  const end = new Date(start.getTime() + durationMinutes * 60000);
  if (end.getDate() !== start.getDate()) return false;

  const pad = (n: number) => String(n).padStart(2, "0");
  const timeOf = (d: Date) => `${pad(d.getHours())}:${pad(d.getMinutes())}`;
  return timeOf(start) >= settings.openTime && timeOf(end) <= settings.closeTime;
}
