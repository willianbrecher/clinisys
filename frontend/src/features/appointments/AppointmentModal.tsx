import { useEffect, useState } from "react";
import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { getPatients } from "@/api/patients";
import { getDoctors } from "@/api/doctors";
import { createAppointment, rescheduleAppointment, updateAppointmentStatus } from "@/api/appointments";
import { appointmentSchema, statusSchema, type AppointmentFormData, type StatusFormData } from "./appointment.schema";
import type { AppointmentModel, PatientModel, DoctorModel, AppointmentStatus } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";

interface Props {
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
  appointment?: AppointmentModel;
  defaultStartsAt?: string;
}

export function AppointmentModal({ open, onClose, onSaved, appointment, defaultStartsAt }: Props) {
  const { t } = useTranslation();
  const { role, doctorId } = useAuth();
  const isEdit = !!appointment;
  const [patients, setPatients] = useState<PatientModel[]>([]);
  const [doctors, setDoctors] = useState<DoctorModel[]>([]);

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<AppointmentFormData>({
    resolver: yupResolver(appointmentSchema) as unknown as Resolver<AppointmentFormData>,
  });

  const { register: registerStatus, handleSubmit: handleStatusSubmit, formState: { isSubmitting: isStatusSubmitting } } = useForm<StatusFormData>({
    resolver: yupResolver(statusSchema),
    defaultValues: { status: appointment?.status },
  });

  useEffect(() => {
    getPatients({ pageSize: 100 }).then((r) => setPatients(r.items)).catch(() => {});
    getDoctors({ pageSize: 100 }).then((r) => setDoctors(r.items)).catch(() => {});
  }, []);

  useEffect(() => {
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
  }, [appointment, defaultStartsAt, reset]);

  const onSubmit = async (data: AppointmentFormData) => {
    try {
      if (isEdit) {
        await rescheduleAppointment(appointment!.id, { startsAt: data.startsAt, durationMinutes: data.durationMinutes, notes: data.notes });
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
    } catch {
      toast.error("Failed to update status.");
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose(); }}>
      <DialogContent className="max-w-md max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{isEdit ? t("appointments.reschedule") : t("appointments.new")}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <Label>{t("appointments.patient")}</Label>
            <select className="border rounded px-3 py-2 text-sm bg-background" {...register("patientId")} disabled={isEdit}>
              <option value="">{t("common.select")}</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
            {errors.patientId && <p className="text-xs text-destructive">{errors.patientId.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label>{t("appointments.doctor")}</Label>
            <select className="border rounded px-3 py-2 text-sm bg-background" {...register("doctorId")} disabled={isEdit || role === "Doctor"}>
              <option value="">{t("common.select")}</option>
              {(role === "Doctor" ? doctors.filter((d) => d.id === doctorId) : doctors).map((d) => (
                <option key={d.id} value={d.id}>{d.fullName}</option>
              ))}
            </select>
            {errors.doctorId && <p className="text-xs text-destructive">{errors.doctorId.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label>{t("appointments.startsAt")}</Label>
            <Input type="datetime-local" {...register("startsAt")} />
            {errors.startsAt && <p className="text-xs text-destructive">{errors.startsAt.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label>{t("appointments.durationMinutes")}</Label>
            <Input type="number" min={5} max={480} step={5} {...register("durationMinutes")} />
            {errors.durationMinutes && <p className="text-xs text-destructive">{errors.durationMinutes.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label>{t("appointments.notes")}</Label>
            <Textarea rows={2} {...register("notes")} />
          </div>

          <div className="flex gap-2">
            <Button type="submit" disabled={isSubmitting}>{t("common.save")}</Button>
            <Button type="button" variant="outline" onClick={onClose}>{t("common.cancel")}</Button>
          </div>
        </form>

        {isEdit && (
          <>
            <hr className="my-2" />
            <form onSubmit={handleStatusSubmit(onStatusSubmit)} className="flex gap-2 items-end">
              <div className="flex flex-col gap-1.5 flex-1">
                <Label>{t("appointments.status")}</Label>
                <select className="border rounded px-3 py-2 text-sm bg-background" {...registerStatus("status")}>
                  <option value="Scheduled">Scheduled</option>
                  <option value="Confirmed">Confirmed</option>
                  <option value="Completed">Completed</option>
                  <option value="Cancelled">Cancelled</option>
                  <option value="NoShow">NoShow</option>
                </select>
              </div>
              <Button type="submit" variant="outline" disabled={isStatusSubmitting}>{t("appointments.updateStatus")}</Button>
            </form>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
