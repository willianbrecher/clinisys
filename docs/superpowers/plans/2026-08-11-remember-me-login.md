# Remember User (Login Screen) Implementation Plan

> Implement task-by-task, in order. Steps use checkbox (`- [ ]`) syntax for tracking. Each task ends with its own commit, per the branch/PR split defined below.

**Goal:** Add a "Remember me" checkbox to the login screen that pre-fills the email field on
return visits (#3). Spec: `docs/superpowers/specs/2026-08-11-remember-me-login.md`.

**Tech Stack:** React 18, TypeScript, React Hook Form + Yup, Shadcn/UI `Checkbox`, react-i18next
(frontend only — no backend changes).

## Global Constraints

- Follow root `CLAUDE.md`: one branch (`feature/<slug>`), referencing its issue.
- Single-layer (frontend-only) fix → PR uses `Closes #3`.
- Email persistence only — no password, no session-length/token changes (see spec §4 non-goals).
- New `auth.rememberMe` locale key must land in all three bundles (`en-US`, `es-ES`, `pt-BR`) in
  the same commit, per root `CLAUDE.md`'s locale-sync rule.

---

### Task 1: Add "Remember me" checkbox to login screen (#3)

**Branch:** `feature/remember-me-login` → PR `Closes #3`

**Files:**
- Modify: `frontend/src/features/auth/LoginPage.tsx`
- Modify: `frontend/src/locales/en-US/translation.json`
- Modify: `frontend/src/locales/es-ES/translation.json`
- Modify: `frontend/src/locales/pt-BR/translation.json`

**Interfaces:** none — self-contained change to an existing page component; no new exports, no
`login.schema.ts` change (checkbox is local component state, not a validated field).

- [ ] **Step 1: Add locale key `auth.rememberMe` to all three bundles**

`frontend/src/locales/en-US/translation.json` — inside the existing `auth` block:

```json
"auth": {
  "login": "Sign In",
  "email": "Email",
  "password": "Password",
  "rememberMe": "Remember me",
  "changePassword": "Change Password",
  ...
```

`frontend/src/locales/es-ES/translation.json`:

```json
"rememberMe": "Recordarme",
```

`frontend/src/locales/pt-BR/translation.json`:

```json
"rememberMe": "Lembrar de mim",
```

(Insert right after `"password"` in each file's `auth` block, matching the position shown for
en-US above.)

- [ ] **Step 2: Wire up remembered-email storage and the checkbox in `LoginPage.tsx`**

```tsx
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
```

- [ ] **Step 3: Manually verify via the `run` skill**

- Log in with "Remember me" checked → reload the login screen (e.g. log out) → email field should
  be pre-filled, checkbox should default to checked.
- Log in with "Remember me" unchecked → reload the login screen → email field should be empty,
  checkbox unchecked.
- Previously remembered email, then log in again with the box unchecked → the remembered email
  should be cleared (next visit shows an empty field), confirming the "forget me" path.
- A failed login attempt (wrong password) must not write/clear `clinisys_remembered_email` —
  check DevTools → Application → Local Storage before and after the failed attempt.
- Switch locale (en-US / es-ES / pt-BR) and confirm the checkbox label translates correctly.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/auth/LoginPage.tsx frontend/src/locales/en-US/translation.json frontend/src/locales/es-ES/translation.json frontend/src/locales/pt-BR/translation.json
git commit -m "feat: add remember me checkbox to login screen"
```

- [ ] **Step 5: Open PR**

```bash
gh pr create --title "feat: add remember me checkbox to login screen" \
  --body "Closes #3

Adds a \"Remember me\" checkbox to the login screen. When checked at a successful login, the email address is saved to localStorage (\`clinisys_remembered_email\`) and pre-filled next time; unchecking and logging in again clears it. Frontend-only — no session length or token lifetime changes (password is never remembered)." \
  --label feature --assignee willianbrecher
```
