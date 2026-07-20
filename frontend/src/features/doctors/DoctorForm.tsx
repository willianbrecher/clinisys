import { useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getDoctorById, updateDoctor } from "@/api/doctors";

export function DoctorForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm<{ specialty: string }>();

  useEffect(() => {
    if (id) getDoctorById(id).then((d) => reset({ specialty: d.specialty })).catch(() => {});
  }, [id, reset]);

  const onSubmit = async (data: { specialty: string }) => {
    try { await updateDoctor(id!, data.specialty); toast.success("Specialty updated."); navigate("/doctors"); }
    catch { toast.error("Failed to update specialty."); }
  };

  return (
    <div className="max-w-sm space-y-4">
      <h1 className="text-2xl font-semibold">{t("doctors.editSpecialty")}</h1>
      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label>{t("doctors.specialty")}</Label>
          <Input {...register("specialty", { required: true })} />
        </div>
        <div className="flex gap-2">
          <Button type="submit" disabled={isSubmitting}>{t("common.save")}</Button>
          <Button type="button" variant="outline" onClick={() => navigate("/doctors")}>{t("common.cancel")}</Button>
        </div>
      </form>
    </div>
  );
}
