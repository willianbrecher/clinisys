import * as yup from "yup";

export const healthPlanSchema = yup.object({
  name: yup.string().required("Name is required").max(200),
  notes: yup.string().optional(),
});

export type HealthPlanFormData = yup.InferType<typeof healthPlanSchema>;
