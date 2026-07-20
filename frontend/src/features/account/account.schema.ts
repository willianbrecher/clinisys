import * as yup from "yup";

export const changePasswordSchema = yup.object({
  currentPassword: yup.string().required("Current password is required"),
  newPassword: yup.string().min(8, "At least 8 characters").required(),
  confirmPassword: yup.string()
    .oneOf([yup.ref("newPassword")], "Passwords do not match")
    .required(),
});

export type ChangePasswordFormData = yup.InferType<typeof changePasswordSchema>;
