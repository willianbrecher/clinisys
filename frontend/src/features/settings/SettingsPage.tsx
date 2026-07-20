import { useEffect, useRef, useState } from "react";
import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { getClinicSettings, updateClinicSettings } from "@/api/clinicSettings";
import { settingsSchema, type SettingsFormData } from "./settings.schema";

const DAYS = [
  { value: 1, label: "Monday" },
  { value: 2, label: "Tuesday" },
  { value: 3, label: "Wednesday" },
  { value: 4, label: "Thursday" },
  { value: 5, label: "Friday" },
  { value: 6, label: "Saturday" },
  { value: 0, label: "Sunday" },
];

const MAX_LOGO_BYTES = 512 * 1024;

export function SettingsPage() {
  const { t } = useTranslation();
  const [openDays, setOpenDays] = useState<number[]>([1, 2, 3, 4, 5]);
  const [logoPreview, setLogoPreview] = useState<string | null>(null);
  const [logoBase64, setLogoBase64] = useState<string | undefined>();
  const fileRef = useRef<HTMLInputElement>(null);

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<SettingsFormData>({
    resolver: yupResolver(settingsSchema) as unknown as Resolver<SettingsFormData>,
    defaultValues: { openTime: "08:00", closeTime: "18:00", openDays: "1,2,3,4,5" },
  });

  useEffect(() => {
    getClinicSettings().then((s) => {
      reset({ openTime: s.openTime, closeTime: s.closeTime, openDays: s.openDays });
      setOpenDays(s.openDays.split(",").map(Number));
      setLogoPreview(s.logoBase64 ?? null);
      setLogoBase64(s.logoBase64 ?? undefined);
    }).catch(() => {});
  }, [reset]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > MAX_LOGO_BYTES) { toast.error("Logo must be under 512 KB."); return; }
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      setLogoPreview(result);
      setLogoBase64(result);
    };
    reader.readAsDataURL(file);
  };

  const toggleDay = (day: number) => {
    setOpenDays((prev) => prev.includes(day) ? prev.filter((d) => d !== day) : [...prev, day].sort());
  };

  const onSubmit = async (_data: SettingsFormData) => {
    try {
      await updateClinicSettings({
        openTime: _data.openTime,
        closeTime: _data.closeTime,
        openDays: openDays.join(","),
        logoBase64: logoBase64 ?? null,
      });
      toast.success("Settings saved.");
    } catch {
      toast.error("Failed to save settings.");
    }
  };

  return (
    <div className="max-w-lg space-y-6">
      <h1 className="text-2xl font-semibold">{t("settings.title")}</h1>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
        <div className="flex flex-col gap-2">
          <Label>{t("settings.logo")}</Label>
          <div className="flex items-center gap-4">
            {logoPreview
              ? <img src={logoPreview} alt="logo preview" className="h-16 w-16 object-contain rounded border" />
              : <div className="h-16 w-16 rounded border flex items-center justify-center text-muted-foreground text-xs">{t("settings.noLogo")}</div>}
            <div className="flex flex-col gap-1">
              <Button type="button" variant="outline" size="sm" onClick={() => fileRef.current?.click()}>
                {t("settings.uploadLogo")}
              </Button>
              {logoPreview && (
                <Button type="button" variant="ghost" size="sm" className="text-destructive"
                  onClick={() => { setLogoPreview(null); setLogoBase64(undefined); }}>
                  {t("settings.removeLogo")}
                </Button>
              )}
              <input ref={fileRef} type="file" accept="image/*" className="hidden" onChange={handleFileChange} />
            </div>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-1.5">
            <Label>{t("settings.openTime")}</Label>
            <Input type="time" {...register("openTime")} />
            {errors.openTime && <p className="text-xs text-destructive">{errors.openTime.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>{t("settings.closeTime")}</Label>
            <Input type="time" {...register("closeTime")} />
            {errors.closeTime && <p className="text-xs text-destructive">{errors.closeTime.message}</p>}
          </div>
        </div>

        <div className="flex flex-col gap-2">
          <Label>{t("settings.openDays")}</Label>
          <div className="flex flex-wrap gap-3">
            {DAYS.map((d) => (
              <div key={d.value} className="flex items-center gap-1.5">
                <Checkbox
                  id={`day-${d.value}`}
                  checked={openDays.includes(d.value)}
                  onCheckedChange={() => toggleDay(d.value)}
                />
                <label htmlFor={`day-${d.value}`} className="text-sm cursor-pointer">{d.label}</label>
              </div>
            ))}
          </div>
        </div>

        <Button type="submit" disabled={isSubmitting}>{t("common.save")}</Button>
      </form>
    </div>
  );
}
