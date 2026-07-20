import * as yup from "yup";

export const appointmentSchema = yup.object({
  patientId: yup.string().uuid().required("Patient is required"),
  doctorId: yup.string().uuid().required("Doctor is required"),
  startsAt: yup.string().required("Start date/time is required"),
  durationMinutes: yup.number().min(5).max(480).required("Duration is required"),
  notes: yup.string().optional(),
});

export const statusSchema = yup.object({
  status: yup.string().oneOf(["Scheduled","Confirmed","Completed","Cancelled","NoShow"]).required(),
});

export type AppointmentFormData = yup.InferType<typeof appointmentSchema>;
export type StatusFormData = yup.InferType<typeof statusSchema>;
