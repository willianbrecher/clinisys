import { useEffect, useState, useCallback } from "react";
import { useTranslation } from "react-i18next";
import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin from "@fullcalendar/interaction";
import { Button } from "@/components/ui/button";
import { Plus, List, CalendarDays } from "lucide-react";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { getAppointments } from "@/api/appointments";
import { getClinicSettings } from "@/api/clinicSettings";
import { AppointmentModal } from "./AppointmentModal";
import type { AppointmentModel, ClinicSettingsModel } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";

type Tab = "list" | "calendar";

const STATUS_COLORS: Record<string, string> = {
  Scheduled: "#3b82f6",
  Confirmed: "#10b981",
  Completed: "#6b7280",
  Cancelled: "#ef4444",
  NoShow: "#f59e0b",
};

export function AppointmentsPage() {
  const { t } = useTranslation();
  const { role, doctorId } = useAuth();
  const [tab, setTab] = useState<Tab>("calendar");
  const [data, setData] = useState<AppointmentModel[]>([]);
  const [listPage, setListPage] = useState(1);
  const [listTotal, setListTotal] = useState(0);
  const [settings, setSettings] = useState<ClinicSettingsModel | null>(null);
  const [selected, setSelected] = useState<AppointmentModel | null>(null);
  const [defaultStartsAt, setDefaultStartsAt] = useState<string | undefined>();
  const [modalOpen, setModalOpen] = useState(false);

  useEffect(() => {
    getClinicSettings().then(setSettings).catch(() => {});
  }, []);

  const loadList = useCallback(() => {
    getAppointments({
      doctorId: role === "Doctor" ? doctorId : undefined,
      page: listPage, pageSize: 20,
    }).then((r) => { setData(r.items); setListTotal(r.totalPages); }).catch(() => {});
  }, [role, doctorId, listPage]);

  const loadCalendar = useCallback((start: string, end: string) => {
    return getAppointments({
      doctorId: role === "Doctor" ? doctorId : undefined,
      startDate: start, endDate: end,
      pageSize: 200,
    }).then((r) => r.items).catch(() => [] as AppointmentModel[]);
  }, [role, doctorId]);

  useEffect(() => { if (tab === "list") loadList(); }, [tab, loadList]);

  const openNew = (startsAt?: string) => {
    setSelected(null);
    setDefaultStartsAt(startsAt);
    setModalOpen(true);
  };

  const openEdit = (a: AppointmentModel) => {
    setSelected(a);
    setDefaultStartsAt(undefined);
    setModalOpen(true);
  };

  const openDays = settings?.openDays
    ? settings.openDays.split(",").map(Number)
    : [1, 2, 3, 4, 5];

  const slotMinTime = settings ? settings.openTime : "08:00:00";
  const slotMaxTime = settings ? settings.closeTime : "18:00:00";

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-2">
        <h1 className="text-2xl font-semibold">{t("appointments.title")}</h1>
        <div className="flex gap-2 flex-wrap">
          <Button variant={tab === "list" ? "default" : "outline"} size="sm" onClick={() => setTab("list")}>
            <List className="mr-1 h-4 w-4" />{t("appointments.listView")}
          </Button>
          <Button variant={tab === "calendar" ? "default" : "outline"} size="sm" onClick={() => setTab("calendar")}>
            <CalendarDays className="mr-1 h-4 w-4" />{t("appointments.calendarView")}
          </Button>
          <Button size="sm" onClick={() => openNew()}><Plus className="mr-1 h-4 w-4" />{t("appointments.new")}</Button>
        </div>
      </div>

      {tab === "list" && (
        <>
          <div className="hidden md:block rounded-md border overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t("appointments.patient")}</TableHead>
                  <TableHead>{t("appointments.doctor")}</TableHead>
                  <TableHead>{t("appointments.startsAt")}</TableHead>
                  <TableHead>{t("appointments.status")}</TableHead>
                  <TableHead>{t("common.actions")}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.map((a) => (
                  <TableRow key={a.id}>
                    <TableCell>{a.patientName}</TableCell>
                    <TableCell>{a.doctorName}</TableCell>
                    <TableCell>{new Intl.DateTimeFormat(undefined, { dateStyle: "short", timeStyle: "short" }).format(new Date(a.startsAt))}</TableCell>
                    <TableCell>
                      <span className="px-2 py-0.5 rounded-full text-xs text-white" style={{ background: STATUS_COLORS[a.status] }}>
                        {a.status}
                      </span>
                    </TableCell>
                    <TableCell>
                      <Button size="sm" variant="outline" onClick={() => openEdit(a)}>{t("common.edit")}</Button>
                    </TableCell>
                  </TableRow>
                ))}
                {data.length === 0 && (
                  <TableRow><TableCell colSpan={5} className="text-center text-muted-foreground py-8">{t("common.noResults")}</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </div>

          <div className="flex flex-col gap-2 md:hidden">
            {data.map((a) => (
              <div key={a.id} className="rounded-md border p-3 space-y-1">
                <div className="flex justify-between">
                  <p className="font-medium">{a.patientName}</p>
                  <span className="px-2 py-0.5 rounded-full text-xs text-white" style={{ background: STATUS_COLORS[a.status] }}>{a.status}</span>
                </div>
                <p className="text-sm text-muted-foreground">{a.doctorName}</p>
                <p className="text-sm">{new Intl.DateTimeFormat(undefined, { dateStyle: "short", timeStyle: "short" }).format(new Date(a.startsAt))}</p>
                <Button size="sm" variant="outline" className="w-full" onClick={() => openEdit(a)}>{t("common.edit")}</Button>
              </div>
            ))}
            {data.length === 0 && <p className="text-center text-muted-foreground py-8">{t("common.noResults")}</p>}
          </div>

          {listTotal > 1 && (
            <div className="flex items-center justify-center gap-2">
              <Button variant="outline" size="sm" disabled={listPage === 1} onClick={() => setListPage(p => p - 1)}>{t("common.previous")}</Button>
              <span className="text-sm">{t("common.page")} {listPage} {t("common.of")} {listTotal}</span>
              <Button variant="outline" size="sm" disabled={listPage === listTotal} onClick={() => setListPage(p => p + 1)}>{t("common.next")}</Button>
            </div>
          )}
        </>
      )}

      {tab === "calendar" && (
        <div className="[&_.fc]:text-sm">
          <FullCalendar
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin] as any[]}
            initialView="timeGridWeek"
            headerToolbar={{
              left: "prev,next today",
              center: "title",
              right: "dayGridMonth,timeGridWeek,timeGridDay",
            }}
            slotMinTime={slotMinTime}
            slotMaxTime={slotMaxTime}
            hiddenDays={[0, 1, 2, 3, 4, 5, 6].filter((d) => !openDays.includes(d))}
            selectable
            selectConstraint={{ daysOfWeek: openDays, startTime: slotMinTime, endTime: slotMaxTime }}
            dateClick={(info: { dateStr: string }) => openNew(info.dateStr)}
            eventClick={(info) => {
              const id = info.event.id;
              const appt = data.find((a) => a.id === id);
              if (appt) openEdit(appt);
            }}
            events={async (info, successCb) => {
              const items = await loadCalendar(info.startStr, info.endStr);
              setData(items);
              successCb(items.map((a) => ({
                id: a.id,
                title: `${a.patientName} (${a.doctorName})`,
                start: a.startsAt,
                end: new Date(new Date(a.startsAt).getTime() + a.durationMinutes * 60000).toISOString(),
                backgroundColor: STATUS_COLORS[a.status],
                borderColor: STATUS_COLORS[a.status],
              })));
            }}
            height="auto"
          />
        </div>
      )}

      <AppointmentModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        onSaved={() => { if (tab === "list") loadList(); }}
        appointment={selected ?? undefined}
        defaultStartsAt={defaultStartsAt}
      />
    </div>
  );
}
