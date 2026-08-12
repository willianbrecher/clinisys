# CliniSys — Remember User (Login Screen) Spec

Date: 2026-08-11
Status: Draft
Issue: [#3](https://github.com/willianbrecher/clinisys/issues/3)

## 1. Goal

Add a "Remember me" checkbox to the login screen. When checked at a successful login, the email
address is pre-filled the next time the login screen is shown, so a returning user only has to
type their password.

Scoped to email-only persistence (confirmed with the user) — this does not change session length,
token lifetime, or add a "stay signed in" concept. That would require backend changes (OpenIddict
refresh tokens or a configurable access-token lifetime), which don't exist today and are out of
scope here.

## 2. Current behavior — confirmed

`frontend/src/features/auth/LoginPage.tsx` (68 lines, full file) is a React Hook Form + Yup form
with two fields — `email` and `password` — validated by `loginSchema`
(`frontend/src/features/auth/login.schema.ts:3-6`, both plain required fields, no persistence
concept). `onSubmit` (`LoginPage.tsx:30-37`) calls `login(data.email, data.password)` from
`useAuth()` and navigates to `/` on success, or toasts `auth.loginError` on failure.

No "remember" functionality exists anywhere in the repo — `grep -rn -i "remember"` across
`frontend/src` and `backend/src` returns zero hits. No checkbox, no persisted default for the
email field, no related locale key in any of the three `auth.*` translation blocks
(`frontend/src/locales/{en-US,es-ES,pt-BR}/translation.json`).

Auth state (`frontend/src/auth/AuthContext.tsx`) stores only the JWT, under localStorage key
`"clinisys_token"` (`AuthContext.tsx:51`). Login itself
(`frontend/src/api/auth.ts:3-14`) is a single POST to `/connect/token`; nothing here changes.

The repo already has a Shadcn `Checkbox` component (`frontend/src/components/ui/checkbox.tsx`),
used elsewhere as a controlled component via `checked`/`onCheckedChange`
(`frontend/src/features/settings/SettingsPage.tsx:121-125`), not wired through RHF's `register()`
(Radix's `Checkbox` isn't a native `<input>`, so it can't take a spread `register()` prop).
`LoginPage` follows the same controlled pattern for the new checkbox rather than adding it to
`login.schema.ts`.

## 3. Proposed fix

Frontend-only change, contained to `LoginPage.tsx`.

**Storage**: a new localStorage key, `clinisys_remembered_email`, sitting alongside the existing
`clinisys_token` key — read once to seed initial form/checkbox state, written (or removed) only on
successful login.

```tsx
const REMEMBERED_EMAIL_KEY = "clinisys_remembered_email";

export function LoginPage() {
  const rememberedEmail = localStorage.getItem(REMEMBERED_EMAIL_KEY) ?? "";
  const [rememberMe, setRememberMe] = useState(rememberedEmail !== "");

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<LoginFormData>({
    resolver: yupResolver(loginSchema),
    defaultValues: { email: rememberedEmail },
  });

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

  // ...
}
```

Checkbox markup, placed between the password field and the submit button:

```tsx
<div className="flex items-center gap-1.5">
  <Checkbox id="rememberMe" checked={rememberMe} onCheckedChange={(v) => setRememberMe(v === true)} />
  <label htmlFor="rememberMe" className="text-sm cursor-pointer">{t("auth.rememberMe")}</label>
</div>
```

Notes:
- Reading `localStorage.getItem` directly in the component body (not in a `useEffect`) is safe
  here — unlike the async-fetch race hypothesized for issue #1, this is a synchronous read used
  only to compute the initial render's `defaultValues`/state, so there's no staleness window.
- The write only happens after a successful `login()` call, so a failed login attempt never
  clobbers a previously remembered email with a typo'd one.
- Unchecking the box and logging in successfully clears any previously remembered email — that's
  the expected "forget me" path, not just "don't update."
- New locale key `auth.rememberMe` needed in all three bundles (`en-US`, `es-ES`, `pt-BR`), per
  root `CLAUDE.md`'s locale-sync rule.

## 4. Non-goals

- No change to session length, token lifetime, or "stay signed in" behavior — password is never
  remembered and the access token's lifetime is untouched.
- No backend changes — no new endpoint, no OpenIddict config change.
- No change to `login.schema.ts` — the checkbox is local component state, not a validated form
  field.
