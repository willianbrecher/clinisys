import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useAuth } from "@/auth/AuthContext";
import { getClinicSettings } from "@/api/clinicSettings";
import { loginSchema, type LoginFormData } from "./login.schema";

const REMEMBERED_EMAIL_KEY = "clinisys_remembered_email";

export function LoginPage() {
  const { t } = useTranslation();
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [logoBase64, setLogoBase64] = useState<string | null>(null);
  const rememberedEmail = localStorage.getItem(REMEMBERED_EMAIL_KEY) ?? "";
  const [rememberMe, setRememberMe] = useState(rememberedEmail !== "");

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<LoginFormData>({
    resolver: yupResolver(loginSchema),
    defaultValues: { email: rememberedEmail },
  });

  useEffect(() => {
    if (isAuthenticated) { navigate("/"); return; }
    getClinicSettings().then((s) => setLogoBase64(s.logoBase64 ?? null)).catch(() => {});
  }, [isAuthenticated, navigate]);

  const onSubmit = async (data: LoginFormData) => {
    try {
      await login(data.email, data.password);
      if (rememberMe) {
        localStorage.setItem(REMEMBERED_EMAIL_KEY, data.email);
      } else {
        localStorage.removeItem(REMEMBERED_EMAIL_KEY);
      }
      navigate("/");
    } catch {
      toast.error(t("auth.loginError"));
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="flex flex-col items-center gap-3 pb-2">
          {logoBase64
            ? <img src={logoBase64} alt="Clinic logo" className="h-16 w-16 object-contain" />
            : <div className="h-16 w-16 rounded-full bg-primary flex items-center justify-center text-primary-foreground text-2xl font-bold">C</div>}
          <CardTitle className="text-xl">CliniSys</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="email">{t("auth.email")}</Label>
              <Input id="email" type="email" autoComplete="email" {...register("email")} />
              {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="password">{t("auth.password")}</Label>
              <Input id="password" type="password" autoComplete="current-password" {...register("password")} />
              {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
            </div>
            <div className="flex items-center gap-1.5">
              <Checkbox id="rememberMe" checked={rememberMe} onCheckedChange={(v) => setRememberMe(v === true)} />
              <label htmlFor="rememberMe" className="text-sm cursor-pointer">{t("auth.rememberMe")}</label>
            </div>
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? t("common.loading") : t("auth.login")}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
