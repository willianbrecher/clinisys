import * as yup from "yup";

export const createUserSchema = yup.object({
  email: yup.string().email().required("Email is required"),
  fullName: yup.string().required("Full name is required"),
  password: yup.string().min(8, "At least 8 characters").required(),
  role: yup.string().oneOf(["Admin","Staff","Doctor"]).required("Role is required"),
  specialty: yup.string().when("role", {
    is: "Doctor",
    then: (s) => s.required("Specialty is required for Doctors"),
    otherwise: (s) => s.optional(),
  }),
});

export const resetPasswordSchema = yup.object({
  newPassword: yup.string().min(8, "At least 8 characters").required(),
});

export type CreateUserFormData = yup.InferType<typeof createUserSchema>;
export type ResetPasswordFormData = yup.InferType<typeof resetPasswordSchema>;
