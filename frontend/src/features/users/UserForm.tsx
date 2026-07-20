import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createUserSchema, type CreateUserFormData } from "./user.schema";

interface Props {
  onSubmit: (data: CreateUserFormData) => Promise<void>;
  onCancel: () => void;
}

export function UserForm({ onSubmit, onCancel }: Props) {
  const { t } = useTranslation();
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<CreateUserFormData>({
    resolver: yupResolver(createUserSchema) as unknown as Resolver<CreateUserFormData>,
  });
  const role = watch("role");

  return (
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
        <Button type="button" variant="outline" onClick={onCancel}>{t("common.cancel")}</Button>
      </div>
    </form>
  );
}
