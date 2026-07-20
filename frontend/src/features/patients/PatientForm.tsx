import { useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { getPatientById, createPatient, updatePatient } from "@/api/patients";
import { patientSchema, type PatientFormData } from "./patient.schema";

export function PatientForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<PatientFormData>({
    resolver: yupResolver(patientSchema) as unknown as Resolver<PatientFormData>,
  });

  useEffect(() => {
    if (id) {
      getPatientById(id).then((p) => reset({
        fullName: p.fullName, dateOfBirth: p.dateOfBirth,
        phone: p.phone, email: p.email ?? "", notes: p.notes ?? "",
      })).catch(() => toast.error("Failed to load patient."));
    }
  }, [id, reset]);

  const onSubmit = async (data: PatientFormData) => {
    try {
      if (isEdit) {
        await updatePatient(id!, data);
        toast.success("Patient updated.");
      } else {
        await createPatient(data);
        toast.success("Patient created.");
      }
      navigate("/patients");
    } catch {
      toast.error("Failed to save patient.");
    }
  };

  return (
    <div className="max-w-2xl space-y-4">
      <h1 className="text-2xl font-semibold">
        {isEdit ? t("common.edit") : t("patients.new")} {t("patients.title").toLowerCase()}
      </h1>

      <form onSubmit={handleSubmit(onSubmit)} className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label>{t("patients.fullName")}</Label>
          <Input {...register("fullName")} />
          {errors.fullName && <p className="text-xs text-destructive">{errors.fullName.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("patients.dateOfBirth")}</Label>
          <Input type="date" {...register("dateOfBirth")} />
          {errors.dateOfBirth && <p className="text-xs text-destructive">{errors.dateOfBirth.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("patients.phone")}</Label>
          <Input {...register("phone")} />
          {errors.phone && <p className="text-xs text-destructive">{errors.phone.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label>{t("patients.email")}</Label>
          <Input type="email" {...register("email")} />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label>{t("patients.notes")}</Label>
          <Textarea {...register("notes")} rows={3} />
        </div>

        <div className="flex gap-2 sm:col-span-2">
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? t("common.loading") : t("common.save")}
          </Button>
          <Button type="button" variant="outline" onClick={() => navigate("/patients")}>
            {t("common.cancel")}
          </Button>
        </div>
      </form>
    </div>
  );
}
