import { useEffect } from "react";
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
import { getHealthPlanById, createHealthPlan, updateHealthPlan } from "@/api/healthPlans";
import { healthPlanSchema, type HealthPlanFormData } from "./healthPlan.schema";
import type { ModalContext } from "@/types/modal";

export function HealthPlanFormContent() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { onClose, onSaved } = useOutletContext<ModalContext>();
  const isEdit = !!id;

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<HealthPlanFormData>({
    resolver: yupResolver(healthPlanSchema) as unknown as Resolver<HealthPlanFormData>,
  });

  useEffect(() => {
    if (id) {
      getHealthPlanById(id).then((p) => reset({
        name: p.name, notes: p.notes ?? "",
      })).catch(() => toast.error("Failed to load health plan."));
    }
  }, [id, reset]);

  const onSubmit = async (data: HealthPlanFormData) => {
    try {
      if (isEdit) {
        await updateHealthPlan(id!, data);
        toast.success("Health plan updated.");
      } else {
        await createHealthPlan(data);
        toast.success("Health plan created.");
      }
      onSaved();
      onClose();
    } catch {
      toast.error("Failed to save health plan.");
    }
  };

  return (
    <>
      <DialogHeader>
        <DialogTitle>{isEdit ? t("common.edit") : t("healthPlans.new")}</DialogTitle>
      </DialogHeader>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label>{t("healthPlans.name")}</Label>
          <Input {...register("name")} />
          {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("healthPlans.notes")}</Label>
          <Textarea {...register("notes")} rows={3} />
        </div>

        <div className="flex gap-2">
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
