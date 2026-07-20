import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { getAppointments } from "@/api/appointments";
import { useAuth } from "@/auth/AuthContext";
import type { AppointmentModel } from "@/api/types";

export function DashboardPage() {
  const { t } = useTranslation();
  const { role, doctorId } = useAuth();
  const [appointments, setAppointments] = useState<AppointmentModel[]>([]);

  useEffect(() => {
    const today = new Date().toISOString().split("T")[0];
    getAppointments({
      date: today,
      doctorId: role === "Doctor" ? doctorId : undefined,
      pageSize: 20,
    }).then((r) => setAppointments(r.items)).catch(() => {});
  }, [role, doctorId]);

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold">{t("nav.dashboard")}</h1>
      <p className="text-muted-foreground">
        {t("appointments.title")} — {new Intl.DateTimeFormat(undefined, { dateStyle: "full" }).format(new Date())}
      </p>
      <div className="grid gap-2">
        {appointments.length === 0 && (
          <p className="text-sm text-muted-foreground">{t("common.noResults")}</p>
        )}
        {appointments.map((a) => (
          <div key={a.id} className="flex items-center justify-between rounded-md border p-3 text-sm">
            <div>
              <p className="font-medium">{a.patientName}</p>
              <p className="text-muted-foreground">{a.doctorName}</p>
            </div>
            <div className="text-right">
              <p>{new Intl.DateTimeFormat(undefined, { timeStyle: "short" }).format(new Date(a.startsAt))}</p>
              <p className="text-muted-foreground">{a.durationMinutes} min</p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
