import * as yup from "yup";

export function buildAppointmentSchema() {
  return yup.object({
    patientId: yup.string().uuid().required("Patient is required"),
    doctorId: yup.string().uuid().required("Doctor is required"),
    startsAt: yup.string().required("Start date/time is required")
      .test("future", "Start date/time must be in the future",
        (value) => !value || new Date(value) > new Date()),
    durationMinutes: yup.number().min(5).max(480).required("Duration is required"),
    notes: yup.string().optional(),
  });
}

export const statusSchema = yup.object({
  status: yup.string().oneOf(["Scheduled","Confirmed","Completed","Cancelled","NoShow"]).required(),
});

export type AppointmentFormData = yup.InferType<ReturnType<typeof buildAppointmentSchema>>;
export type StatusFormData = yup.InferType<typeof statusSchema>;
