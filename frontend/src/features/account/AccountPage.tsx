import { useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { useTheme } from "next-themes";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { updateProfilePicture, updatePreferences } from "@/api/account";
import { changePassword } from "@/api/auth";
import { changePasswordSchema, type ChangePasswordFormData } from "./account.schema";
import { useAuth } from "@/auth/AuthContext";
import type { ThemePreference } from "@/api/types";

const MAX_PIC_BYTES = 512 * 1024;

export function AccountPage() {
  const { t, i18n } = useTranslation();
  const { fullName } = useAuth();
  const { theme, setTheme } = useTheme();
  const [preview, setPreview] = useState<string | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  const initials = fullName.split(" ").slice(0, 2).map((n) => n[0]).join("").toUpperCase();

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<ChangePasswordFormData>({
    resolver: yupResolver(changePasswordSchema),
  });

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > MAX_PIC_BYTES) { toast.error("Image must be under 512 KB."); return; }
    const reader = new FileReader();
    reader.onload = async () => {
      const result = reader.result as string;
      setPreview(result);
      try { await updateProfilePicture(result); toast.success("Profile picture updated."); }
      catch { toast.error("Failed to update picture."); }
    };
    reader.readAsDataURL(file);
  };

  const handleThemeChange = async (value: string) => {
    setTheme(value);
    const pref: ThemePreference = value === "light" ? "Light" : value === "dark" ? "Dark" : "System";
    const lang = localStorage.getItem("clinisys_language") ?? "en-US";
    try { await updatePreferences(pref, lang); }
    catch { toast.error("Failed to save theme preference."); }
  };

  const handleLanguageChange = async (lang: string) => {
    await i18n.changeLanguage(lang);
    localStorage.setItem("clinisys_language", lang);
    const pref: ThemePreference = theme === "light" ? "Light" : theme === "dark" ? "Dark" : "System";
    try { await updatePreferences(pref, lang); }
    catch { toast.error("Failed to save language preference."); }
  };

  const onPasswordSubmit = async (data: ChangePasswordFormData) => {
    try {
      await changePassword(data.currentPassword, data.newPassword);
      toast.success("Password changed.");
      reset();
    } catch {
      toast.error("Failed to change password.");
    }
  };

  return (
    <div className="max-w-lg space-y-8">
      <h1 className="text-2xl font-semibold">{t("nav.account")}</h1>

      <section className="space-y-3">
        <h2 className="text-lg font-medium">{t("account.profilePicture")}</h2>
        <div className="flex items-center gap-4">
          <Avatar className="h-16 w-16">
            {preview && <AvatarImage src={preview} />}
            <AvatarFallback className="text-xl">{initials}</AvatarFallback>
          </Avatar>
          <div>
            <Button type="button" variant="outline" size="sm" onClick={() => fileRef.current?.click()}>
              {t("account.uploadPicture")}
            </Button>
            <p className="text-xs text-muted-foreground mt-1">{t("account.maxSize")}</p>
            <input ref={fileRef} type="file" accept="image/*" className="hidden" onChange={handleFileChange} />
          </div>
        </div>
      </section>

      <section className="space-y-3">
        <h2 className="text-lg font-medium">{t("account.preferences")}</h2>
        <div className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <Label>{t("account.theme")}</Label>
            <select className="border rounded px-3 py-2 text-sm bg-background max-w-xs"
              value={theme ?? "system"} onChange={(e) => handleThemeChange(e.target.value)}>
              <option value="light">{t("theme.light")}</option>
              <option value="dark">{t("theme.dark")}</option>
              <option value="system">{t("theme.system")}</option>
            </select>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>{t("account.language")}</Label>
            <select className="border rounded px-3 py-2 text-sm bg-background max-w-xs"
              value={i18n.language} onChange={(e) => handleLanguageChange(e.target.value)}>
              <option value="en-US">{t("language.en-US")}</option>
              <option value="pt-BR">{t("language.pt-BR")}</option>
              <option value="es-ES">{t("language.es-ES")}</option>
            </select>
          </div>
        </div>
      </section>

      <section className="space-y-3">
        <h2 className="text-lg font-medium">{t("account.changePassword")}</h2>
        <form onSubmit={handleSubmit(onPasswordSubmit)} className="flex flex-col gap-3 max-w-sm">
          <div className="flex flex-col gap-1.5">
            <Label>{t("account.currentPassword")}</Label>
            <Input type="password" {...register("currentPassword")} />
            {errors.currentPassword && <p className="text-xs text-destructive">{errors.currentPassword.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>{t("account.newPassword")}</Label>
            <Input type="password" {...register("newPassword")} />
            {errors.newPassword && <p className="text-xs text-destructive">{errors.newPassword.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>{t("account.confirmPassword")}</Label>
            <Input type="password" {...register("confirmPassword")} />
            {errors.confirmPassword && <p className="text-xs text-destructive">{errors.confirmPassword.message}</p>}
          </div>
          <Button type="submit" disabled={isSubmitting} className="self-start">
            {t("account.changePassword")}
          </Button>
        </form>
      </section>
    </div>
  );
}
