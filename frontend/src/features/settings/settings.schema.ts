import * as yup from "yup";

export const settingsSchema = yup.object({
  openTime: yup.string().required("Open time is required"),
  closeTime: yup.string().required("Close time is required"),
  openDays: yup.string().required(),
  logoBase64: yup.string().optional(),
});

export type SettingsFormData = yup.InferType<typeof settingsSchema>;
