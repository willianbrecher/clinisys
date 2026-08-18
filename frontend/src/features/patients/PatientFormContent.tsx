import { useEffect, useState } from "react";
import { useParams, useOutletContext } from "react-router-dom";
import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { getPatientById, createPatient, updatePatient } from "@/api/patients";
import { getHealthPlans } from "@/api/healthPlans";
import { patientSchema, type PatientFormData } from "./patient.schema";
import type { HealthPlanModel } from "@/api/types";
import type { ModalContext } from "@/types/modal";

export function PatientFormContent() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { onClose, onSaved } = useOutletContext<ModalContext>();
  const isEdit = !!id;

  const [healthPlans, setHealthPlans] = useState<HealthPlanModel[]>([]);
  const [optionsLoaded, setOptionsLoaded] = useState(false);

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<PatientFormData>({
    resolver: yupResolver(patientSchema) as unknown as Resolver<PatientFormData>,
  });

  useEffect(() => {
    getHealthPlans({ pageSize: 100 }).then((r) => setHealthPlans(r.items)).catch(() => {})
      .finally(() => setOptionsLoaded(true));
  }, []);

  useEffect(() => {
    if (!optionsLoaded) return;
    if (id) {
      getPatientById(id).then((p) => reset({
        fullName: p.fullName, dateOfBirth: p.dateOfBirth,
        phone: p.phone, email: p.email ?? "", notes: p.notes ?? "",
        healthPlanId: p.healthPlanId ?? "", healthPlanNumber: p.healthPlanNumber ?? "",
      })).catch(() => toast.error("Failed to load patient."));
    }
  }, [optionsLoaded, id, reset]);

  const onSubmit = async (data: PatientFormData) => {
    try {
      if (isEdit) {
        await updatePatient(id!, data);
        toast.success("Patient updated.");
      } else {
        await createPatient(data);
        toast.success("Patient created.");
      }
      onSaved();
      onClose();
    } catch {
      toast.error("Failed to save patient.");
    }
  };

  return (
    <>
      <DialogHeader>
        <DialogTitle>{isEdit ? t("common.edit") : t("patients.new")}</DialogTitle>
      </DialogHeader>

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

        <div className="flex flex-col gap-1.5">
          <Label>{t("patients.healthPlan")}</Label>
          <select className="border rounded px-3 py-2 text-sm bg-background" {...register("healthPlanId")}>
            <option value="">{t("common.select")}</option>
            {healthPlans.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
          {errors.healthPlanId && <p className="text-xs text-destructive">{errors.healthPlanId.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("patients.healthPlanNumber")}</Label>
          <Input {...register("healthPlanNumber")} />
          {errors.healthPlanNumber && <p className="text-xs text-destructive">{errors.healthPlanNumber.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label>{t("patients.notes")}</Label>
          <Textarea {...register("notes")} rows={3} />
        </div>

        <div className="flex gap-2 sm:col-span-2">
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? t("common.loading") : t("common.save")}
          </Button>
          <Button type="button" variant="outline" onClick={onClose}>
            {t("common.cancel")}
          </Button>
        </div>
      </form>
    </>
  );
}
