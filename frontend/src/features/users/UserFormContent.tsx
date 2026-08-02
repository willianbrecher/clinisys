import { useOutletContext } from "react-router-dom";
import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import axios from "axios";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { createUser } from "@/api/users";
import { createUserSchema, type CreateUserFormData } from "./user.schema";
import type { ModalContext } from "@/types/modal";

export function UserFormContent() {
  const { t } = useTranslation();
  const { onClose, onSaved } = useOutletContext<ModalContext>();
  const { register, handleSubmit, watch, setError, formState: { errors, isSubmitting } } = useForm<CreateUserFormData>({
    resolver: yupResolver(createUserSchema) as unknown as Resolver<CreateUserFormData>,
  });
  const role = watch("role");

  const onSubmit = async (formData: CreateUserFormData) => {
    try {
      await createUser({
        email: formData.email,
        fullName: formData.fullName,
        password: formData.password,
        role: formData.role as "Admin" | "Staff" | "Doctor",
        specialty: formData.specialty,
      });
      toast.success("User created.");
      onSaved();
      onClose();
    } catch (err) {
      const apiErrors = axios.isAxiosError(err)
        ? (err.response?.data?.errors as string[] | undefined)
        : undefined;
      const passwordError = apiErrors?.find((m) => m.toLowerCase().includes("password"));
      if (passwordError) {
        setError("password", { message: passwordError });
      } else {
        toast.error(apiErrors?.[0] ?? "Failed to create user.");
      }
    }
  };

  return (
    <>
      <DialogHeader>
        <DialogTitle>{t("users.new")}</DialogTitle>
      </DialogHeader>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
        <div className="flex flex-col gap-1.5">
          <Label>{t("users.fullName")}</Label>
          <Input {...register("fullName")} />
          {errors.fullName && <p className="text-xs text-destructive">{errors.fullName.message}</p>}
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>{t("users.email")}</Label>
          <Input type="email" {...register("email")} />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>{t("users.password")}</Label>
          <Input type="password" {...register("password")} />
          {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>{t("users.role")}</Label>
          <select className="border rounded px-3 py-2 text-sm bg-background" {...register("role")}>
            <option value="Admin">{t("users.role_Admin")}</option>
            <option value="Staff">{t("users.role_Staff")}</option>
            <option value="Doctor">{t("users.role_Doctor")}</option>
          </select>
          {errors.role && <p className="text-xs text-destructive">{errors.role.message}</p>}
        </div>
        {role === "Doctor" && (
          <div className="flex flex-col gap-1.5">
            <Label>{t("users.specialty")}</Label>
            <Input {...register("specialty")} />
            {errors.specialty && <p className="text-xs text-destructive">{errors.specialty.message}</p>}
          </div>
        )}
        <div className="flex gap-2 pt-1">
          <Button type="submit" disabled={isSubmitting}>{t("common.create")}</Button>
          <Button type="button" variant="outline" onClick={onClose}>{t("common.cancel")}</Button>
        </div>
      </form>
    </>
  );
}
