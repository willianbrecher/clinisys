import { useEffect, useState } from "react";
import { getClinicSettings } from "@/api/clinicSettings";
import type { ClinicSettingsModel } from "@/api/types";

export function useClinicSettings() {
  const [settings, setSettings] = useState<ClinicSettingsModel | null>(null);

  useEffect(() => {
    getClinicSettings().then(setSettings).catch(() => {});
  }, []);

  return { settings, setSettings };
}
