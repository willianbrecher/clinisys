import { useEffect, useMemo, useState } from "react";
import { useParams, useLocation, useOutletContext } from "react-router-dom";
import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { getPatients } from "@/api/patients";
import { getDoctors } from "@/api/doctors";
import { createAppointment, rescheduleAppointment, updateAppointmentStatus } from "@/api/appointments";
import { buildAppointmentSchema, statusSchema, type AppointmentFormData, type StatusFormData } from "./appointment.schema";
import type { AppointmentModel, PatientModel, DoctorModel, AppointmentStatus } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import type { ModalContext } from "@/types/modal";
import { getApiErrorMessage } from "@/lib/apiError";

export function AppointmentFormContent() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { pathname, state } = useLocation();
  const { role, doctorId } = useAuth();
  const { onClose, onSaved } = useOutletContext<ModalContext>();

  const isDetail = pathname.endsWith("/detail");
  const appointment: AppointmentModel | undefined = state?.appointment;
  const defaultStartsAt: string | undefined = state?.defaultStartsAt;
  const isEdit = !!id;

  const [patients, setPatients] = useState<PatientModel[]>([]);
  const [doctors, setDoctors] = useState<DoctorModel[]>([]);
  const [optionsLoaded, setOptionsLoaded] = useState(false);

  const appointmentSchema = useMemo(() => buildAppointmentSchema(), []);

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<AppointmentFormData>({
    resolver: yupResolver(appointmentSchema) as unknown as Resolver<AppointmentFormData>,
  });

  const pad = (n: number) => String(n).padStart(2, "0");
  const now = new Date();
  const minStartsAt = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(now.getHours())}:${pad(now.getMinutes())}`;

  const { register: registerStatus, handleSubmit: handleStatusSubmit, formState: { isSubmitting: isStatusSubmitting } } = useForm<StatusFormData>({
    resolver: yupResolver(statusSchema),
    defaultValues: { status: appointment?.status },
  });

  useEffect(() => {
    Promise.all([
      getPatients({ pageSize: 100 }).then((r) => setPatients(r.items)).catch(() => {}),
      getDoctors({ pageSize: 100 }).then((r) => setDoctors(r.items)).catch(() => {}),
    ]).finally(() => setOptionsLoaded(true));
  }, []);

  useEffect(() => {
    if (!optionsLoaded) return;
    if (appointment) {
      reset({
        patientId: appointment.patientId,
        doctorId: appointment.doctorId,
        startsAt: appointment.startsAt.slice(0, 16),
        durationMinutes: appointment.durationMinutes,
        notes: appointment.notes ?? "",
      });
    } else {
      reset({ startsAt: defaultStartsAt?.slice(0, 16) ?? "", durationMinutes: 30 });
    }
  }, [optionsLoaded, appointment, defaultStartsAt, reset]);

  const onSubmit = async (data: AppointmentFormData) => {
    try {
      if (isEdit) {
        await rescheduleAppointment(appointment!.id, {
          startsAt: data.startsAt,
          durationMinutes: data.durationMinutes,
          notes: data.notes,
        });
        toast.success("Appointment rescheduled.");
      } else {
        await createAppointment(data);
        toast.success("Appointment created.");
      }
      onSaved();
      onClose();
    } catch {
      toast.error("Failed to save appointment.");
    }
  };

  const onStatusSubmit = async (data: StatusFormData) => {
    try {
      await updateAppointmentStatus(appointment!.id, data.status as AppointmentStatus);
      toast.success("Status updated.");
      onSaved();
      onClose();
    } catch (err) {
      toast.error(getApiErrorMessage(err, "Failed to update status."));
    }
  };

  return (
    <>
      <DialogHeader>
        <DialogTitle>
          {isDetail
            ? t("appointments.title")
            : isEdit
              ? t("appointments.reschedule")
              : t("appointments.new")}
        </DialogTitle>
      </DialogHeader>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
        <div className="flex flex-col gap-1.5">
          <Label>{t("appointments.patient")}</Label>
          <select
            className="border rounded px-3 py-2 text-sm bg-background"
            {...register("patientId")}
            disabled={isEdit || isDetail}
          >
            <option value="">{t("common.select")}</option>
            {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
          </select>
          {errors.patientId && <p className="text-xs text-destructive">{errors.patientId.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("appointments.doctor")}</Label>
          <select
            className="border rounded px-3 py-2 text-sm bg-background"
            {...register("doctorId")}
            disabled={isEdit || isDetail || role === "Doctor"}
          >
            <option value="">{t("common.select")}</option>
            {(role === "Doctor" ? doctors.filter((d) => d.id === doctorId) : doctors).map((d) => (
              <option key={d.id} value={d.id}>{d.fullName}</option>
            ))}
          </select>
          {errors.doctorId && <p className="text-xs text-destructive">{errors.doctorId.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("appointments.startsAt")}</Label>
          <Input type="datetime-local" min={minStartsAt} {...register("startsAt")} disabled={isDetail} readOnly={isDetail} />
          {errors.startsAt && <p className="text-xs text-destructive">{errors.startsAt.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("appointments.durationMinutes")}</Label>
          <Input type="number" min={5} max={480} step={5} {...register("durationMinutes")} disabled={isDetail} readOnly={isDetail} />
          {errors.durationMinutes && <p className="text-xs text-destructive">{errors.durationMinutes.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("appointments.notes")}</Label>
          <Textarea rows={2} {...register("notes")} disabled={isDetail} readOnly={isDetail} />
        </div>

        <div className="flex gap-2">
          {!isDetail && (
            <Button type="submit" disabled={isSubmitting}>{t("common.save")}</Button>
          )}
          <Button type="button" variant="outline" onClick={onClose}>{t("common.cancel")}</Button>
        </div>
      </form>

      {isEdit && !isDetail && (
        <>
          <hr className="my-2" />
          <form onSubmit={handleStatusSubmit(onStatusSubmit)} className="flex gap-2 items-end">
            <div className="flex flex-col gap-1.5 flex-1">
              <Label>{t("appointments.status")}</Label>
              <select className="border rounded px-3 py-2 text-sm bg-background" {...registerStatus("status")}>
                <option value="Scheduled">{t("appointments.status_Scheduled")}</option>
                <option value="Confirmed">{t("appointments.status_Confirmed")}</option>
                <option value="Completed">{t("appointments.status_Completed")}</option>
                <option value="Cancelled">{t("appointments.status_Cancelled")}</option>
                <option value="NoShow">{t("appointments.status_NoShow")}</option>
              </select>
            </div>
            <Button type="submit" variant="outline" disabled={isStatusSubmitting}>
              {t("appointments.updateStatus")}
            </Button>
          </form>
        </>
      )}
    </>
  );
}
