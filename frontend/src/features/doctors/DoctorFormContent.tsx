import { useEffect } from "react";
import { useParams, useLocation, useOutletContext } from "react-router-dom";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { getDoctorById, updateDoctor } from "@/api/doctors";
import { getApiErrorMessage } from "@/lib/apiError";
import type { ModalContext } from "@/types/modal";

export function DoctorFormContent() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { pathname } = useLocation();
  const isDetail = pathname.endsWith("/detail");
  const { onClose, onSaved } = useOutletContext<ModalContext>();

  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm<{ specialty: string }>();

  useEffect(() => {
    if (!id) return;
    let active = true;
    getDoctorById(id)
      .then((d) => { if (active) reset({ specialty: d.specialty }); })
      .catch((err) => { if (active) toast.error(getApiErrorMessage(err, "Failed to load doctor.")); });
    return () => { active = false; };
  }, [id, reset]);

  const onSubmit = async (data: { specialty: string }) => {
    try {
      await updateDoctor(id!, data.specialty);
      toast.success("Specialty updated.");
      onSaved();
      onClose();
    } catch {
      toast.error("Failed to update specialty.");
    }
  };

  return (
    <>
      <DialogHeader>
        <DialogTitle>
          {isDetail ? t("doctors.specialty") : t("doctors.editSpecialty")}
        </DialogTitle>
      </DialogHeader>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label>{t("doctors.specialty")}</Label>
          <Input
            {...register("specialty", { required: !isDetail })}
            disabled={isDetail}
            readOnly={isDetail}
          />
        </div>
        <div className="flex gap-2">
          {!isDetail && (
            <Button type="submit" disabled={isSubmitting}>{t("common.save")}</Button>
          )}
          <Button type="button" variant="outline" onClick={onClose}>{t("common.cancel")}</Button>
        </div>
      </form>
    </>
  );
}
