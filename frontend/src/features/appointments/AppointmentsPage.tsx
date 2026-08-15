import { useEffect, useState, useCallback } from "react";
import { useNavigate, useOutlet, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import FullCalendar from "@fullcalendar/react";
import type { EventSourceFuncArg, EventInput } from "@fullcalendar/core";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin from "@fullcalendar/interaction";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { Plus, List, CalendarDays } from "lucide-react";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { getAppointments } from "@/api/appointments";
import { getClinicSettings } from "@/api/clinicSettings";
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
  const navigate = useNavigate();
  const outlet = useOutlet();
  const { role, doctorId } = useAuth();
  const [tab, setTab] = useState<Tab>("calendar");
  const [data, setData] = useState<AppointmentModel[]>([]);
  const [listPage, setListPage] = useState(1);
  const [listTotal, setListTotal] = useState(0);
  const [settings, setSettings] = useState<ClinicSettingsModel | null>(null);
  const [calendarKey, setCalendarKey] = useState(0);

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
      startDate: start, endDate: end, pageSize: 200,
    }).then((r) => r.items).catch(() => [] as AppointmentModel[]);
  }, [role, doctorId]);

  const handleCalendarEvents = useCallback(
    (info: EventSourceFuncArg, successCb: (events: EventInput[]) => void) => {
      loadCalendar(info.startStr, info.endStr).then((items) => {
        successCb(items.map((a) => ({
          id: a.id,
          title: `${a.patientName} (${a.doctorName})`,
          start: a.startsAt,
          end: new Date(new Date(a.startsAt).getTime() + a.durationMinutes * 60000).toISOString(),
          backgroundColor: STATUS_COLORS[a.status],
          borderColor: STATUS_COLORS[a.status],
          extendedProps: { appointment: a },
        })));
      });
    },
    [loadCalendar],
  );

  useEffect(() => { if (tab === "list") loadList(); }, [tab, loadList]);

  const close = () => navigate("/appointments");

  const onSaved = () => {
    if (tab === "list") loadList();
    else setCalendarKey((k) => k + 1);
  };

  const openAppointment = (a: AppointmentModel) => {
    if (role === "Doctor") navigate(`/appointments/${a.id}/detail`, { state: { appointment: a } });
    else navigate(`/appointments/${a.id}/edit`, { state: { appointment: a } });
  };

  const openDays = settings?.openDays
    ? settings.openDays.split(",").map(Number)
    : [1, 2, 3, 4, 5];

  const slotMinTime = settings ? settings.openTime : "08:00:00";
  const slotMaxTime = settings ? settings.closeTime : "18:00:00";

  const pad = (n: number) => String(n).padStart(2, "0");
  const now = new Date();
  const todayStr = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;

  const isPastClick = (date: Date, allDay: boolean) => {
    if (allDay) {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      return date < today;
    }
    return date < new Date();
  };

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
          <Button size="sm" onClick={() => navigate("/appointments/new")}>
            <Plus className="mr-1 h-4 w-4" />{t("appointments.new")}
          </Button>
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
                      <Button size="sm" variant="outline" onClick={() => openAppointment(a)}>
                        {role === "Doctor" ? t("common.view") : t("common.edit")}
                      </Button>
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
                <Button size="sm" variant="outline" className="w-full" onClick={() => openAppointment(a)}>
                  {role === "Doctor" ? t("common.view") : t("common.edit")}
                </Button>
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
            key={calendarKey}
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin] as any[]}
            initialView="timeGridWeek"
            headerToolbar={{
              left: "prev,next today",
              center: "title",
              right: "dayGridMonth,timeGridWeek,timeGridDay",
            }}
            validRange={{ start: todayStr }}
            slotMinTime={slotMinTime}
            slotMaxTime={slotMaxTime}
            hiddenDays={[0, 1, 2, 3, 4, 5, 6].filter((d) => !openDays.includes(d))}
            selectable
            selectConstraint={{ daysOfWeek: openDays, startTime: slotMinTime, endTime: slotMaxTime }}
            dateClick={(info: { dateStr: string; date: Date; allDay: boolean }) => {
              if (isPastClick(info.date, info.allDay)) return;
              navigate("/appointments/new", { state: { defaultStartsAt: info.dateStr } });
            }}
            eventClick={(info) => {
              const appt = info.event.extendedProps.appointment as AppointmentModel;
              openAppointment(appt);
            }}
            events={handleCalendarEvents}
            height="auto"
          />
        </div>
      )}

      <Dialog open={!!outlet} onOpenChange={(open) => { if (!open) close(); }}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <Outlet context={{ onClose: close, onSaved }} />
        </DialogContent>
      </Dialog>
    </div>
  );
}
