import * as yup from "yup";

export const patientSchema = yup.object({
  fullName: yup.string().required("Full name is required").max(200),
  dateOfBirth: yup.string().required("Date of birth is required"),
  phone: yup.string().required("Phone is required").max(30),
  email: yup.string().email("Invalid email").optional(),
  notes: yup.string().optional(),
});

export type PatientFormData = yup.InferType<typeof patientSchema>;
