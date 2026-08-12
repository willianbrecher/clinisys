# Side Menu & Header Cleanup Batch Implementation Plan

> Implement task-by-task, in order. Steps use checkbox (`- [ ]`) syntax for tracking. Each task ends with its own commit and PR, per the branch/PR split defined below. **Merge each PR to `master` before starting the next task** — all three touch `AppLayout.tsx`, and #4/#6 touch the same `links` array, so branching each task off the latest `master` avoids conflicts instead of resolving them later.

**Goal:** Remove "My Account" from the side menu (#4), group Users/Settings under a collapsible
"Administration" item (#6), and move the user's name from the dropdown into the header (#7).
Spec: `docs/superpowers/specs/2026-08-11-nav-header-cleanup-batch.md`.

**Tech Stack:** React 18, TypeScript, Tailwind, `lucide-react` icons, react-i18next (frontend
only — no backend changes).

## Global Constraints

- Follow root `CLAUDE.md`: one branch (`feature/<slug>` or `fix/<slug>`) per issue.
- All three are single-layer (frontend-only) → each PR uses `Closes #N`.
- Implementation order: **#4 → #6 → #7**. #4 and #6 both edit `SidebarContent`'s `links` array
  (#4 deletes a line from it, #6 splits it in two), so doing #4 first means #6 starts from the
  already-shrunk array instead of deleting-then-splitting in the same diff. #7 only touches the
  header/dropdown region (lines 119-137), so it doesn't conflict with either — going last is just
  to keep a single clean sequence, not a hard dependency.
- New locale key `nav.administration` (task 2) must land in all three bundles (`en-US`, `es-ES`,
  `pt-BR`) in the same commit, per root `CLAUDE.md`'s locale-sync rule.

---

### Task 1: Remove "My Account" from the side menu (#4)

**Branch:** `feature/remove-my-account-side-menu` → PR `Closes #4`

**Files:**
- Modify: `frontend/src/components/AppLayout.tsx`

**Interfaces:** none.

- [ ] **Step 1: Delete the `/account` entry from `SidebarContent`'s `links` array**

In `frontend/src/components/AppLayout.tsx`, remove this line from `links` (currently line 43):

```tsx
    { to: "/account", icon: <User className="h-4 w-4" />, label: t("nav.account"), roles: ["Admin","Staff","Doctor"] },
```

Leave the `User` icon import as-is — still used by the dropdown's "Account" item.

- [ ] **Step 2: Manually verify via the `run` skill**

- Side menu no longer shows "Account" for any role (Admin/Staff/Doctor).
- Avatar dropdown still shows "Account", and clicking it still navigates to `/account` and loads
  `AccountPage` correctly — confirms the page isn't orphaned.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/AppLayout.tsx
git commit -m "fix: remove My Account from the side menu"
```

- [ ] **Step 4: Open PR**

```bash
gh pr create --title "fix: remove My Account from the side menu" \
  --body "Closes #4

Removes the \"Account\" entry from the side navigation. The page stays reachable via the avatar dropdown's \"Account\" item, which is untouched.

Spec: \`docs/superpowers/specs/2026-08-11-nav-header-cleanup-batch.md\`" \
  --label enhancement --assignee willianbrecher
```

(Repo has no `feature` label — use `enhancement`, matching the issue's own label, per precedent
from PR #24.)

---

### Task 2: Group Users + Settings under "Administration" (#6)

**Branch:** `feature/administration-side-menu-group` → PR `Closes #6`. Branch from `master`
*after* task 1's PR merges.

**Files:**
- Modify: `frontend/src/components/AppLayout.tsx`
- Modify: `frontend/src/locales/en-US/translation.json`
- Modify: `frontend/src/locales/es-ES/translation.json`
- Modify: `frontend/src/locales/pt-BR/translation.json`

**Interfaces:** none — local component state (`adminOpen`), no new exports.

- [ ] **Step 1: Add locale key `nav.administration` to all three bundles**

`frontend/src/locales/en-US/translation.json` — inside the `nav` block, after `"settings"`:

```json
"administration": "Administration",
```

`frontend/src/locales/es-ES/translation.json`:

```json
"administration": "Administración",
```

`frontend/src/locales/pt-BR/translation.json`:

```json
"administration": "Administração",
```

- [ ] **Step 2: Split `links` into `topLinks`/`adminLinks` and add the collapsible group**

In `frontend/src/components/AppLayout.tsx`:

1. Add `Shield` and `ChevronDown` to the `lucide-react` import list (alongside the existing
   `LayoutDashboard, Users, UserRound, CalendarDays, Settings, User, LogOut, Menu`).
2. In `SidebarContent`, replace the single `links` array and its render with:

```tsx
function SidebarContent({ onNav }: { onNav?: () => void }) {
  const { t } = useTranslation();
  const { role } = useAuth();
  const [adminOpen, setAdminOpen] = useState(false);

  const topLinks: NavItem[] = [
    { to: "/", icon: <LayoutDashboard className="h-4 w-4" />, label: t("nav.dashboard"), roles: ["Admin","Staff","Doctor"] },
    { to: "/patients", icon: <UserRound className="h-4 w-4" />, label: t("nav.patients"), roles: ["Admin","Staff"] },
    { to: "/doctors", icon: <Users className="h-4 w-4" />, label: t("nav.doctors"), roles: ["Admin","Staff"] },
    { to: "/appointments", icon: <CalendarDays className="h-4 w-4" />, label: t("nav.appointments"), roles: ["Admin","Staff","Doctor"] },
  ];

  const adminLinks: NavItem[] = [
    { to: "/users", icon: <Users className="h-4 w-4" />, label: t("nav.users"), roles: ["Admin"] },
    { to: "/settings", icon: <Settings className="h-4 w-4" />, label: t("nav.settings"), roles: ["Admin"] },
  ];

  return (
    <nav className="flex flex-col gap-1 p-4">
      {topLinks.filter((l) => l.roles.includes(role)).map((l) => (
        <NavLink key={l.to} to={l.to} end={l.to === "/"} className={navLinkClass} onClick={onNav}>
          {l.icon}{l.label}
        </NavLink>
      ))}
      {role === "Admin" && (
        <div>
          <button
            type="button"
            onClick={() => setAdminOpen((o) => !o)}
            className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm font-medium hover:bg-accent hover:text-accent-foreground"
          >
            <Shield className="h-4 w-4" />
            {t("nav.administration")}
            <ChevronDown className={`ml-auto h-4 w-4 transition-transform ${adminOpen ? "rotate-180" : ""}`} />
          </button>
          {adminOpen && (
            <div className="ml-4 flex flex-col gap-1">
              {adminLinks.map((l) => (
                <NavLink key={l.to} to={l.to} className={navLinkClass} onClick={onNav}>
                  {l.icon}{l.label}
                </NavLink>
              ))}
            </div>
          )}
        </div>
      )}
    </nav>
  );
}
```

- [ ] **Step 3: Manually verify via the `run` skill**

- As Admin: "Administration" appears once, collapsed by default; clicking toggles it open/closed
  (chevron rotates), revealing "Users" and "Settings" indented underneath; clicking either
  navigates correctly and highlights as active.
- As Staff or Doctor: "Administration" does not appear at all (matches today's behavior where
  Users/Settings were already hidden for those roles).
- Mobile sheet (`SheetContent`) renders the same collapsible correctly and `onNav` still closes
  the sheet on navigating to Users or Settings.
- Switch locale (en-US / es-ES / pt-BR) and confirm "Administration" translates correctly.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/AppLayout.tsx frontend/src/locales/en-US/translation.json frontend/src/locales/es-ES/translation.json frontend/src/locales/pt-BR/translation.json
git commit -m "feat: group Users and Settings under Administration in the side menu"
```

- [ ] **Step 5: Open PR**

```bash
gh pr create --title "feat: group Users and Settings under Administration in the side menu" \
  --body "Closes #6

Consolidates the \"Users\" and \"Settings\" side-menu items (both Admin-only) under a single collapsible \"Administration\" group, expanded on click. No routing changes — /users and /settings stay as-is; this is menu-UI grouping only, via local component state (no new dependency added).

Spec: \`docs/superpowers/specs/2026-08-11-nav-header-cleanup-batch.md\`" \
  --label enhancement --assignee willianbrecher
```

---

### Task 3: User's name in the header, removed from dropdown (#7)

**Branch:** `feature/header-user-name` → PR `Closes #7`. Branch from `master` *after* task 2's PR
merges.

**Files:**
- Modify: `frontend/src/components/AppLayout.tsx`

**Interfaces:** none — reuses `fullName`, already destructured from `useAuth()`.

- [ ] **Step 1: Add the name span to the header, before the `DropdownMenu`**

In `frontend/src/components/AppLayout.tsx`, immediately before the `<DropdownMenu>` block
(after the vertical `<Separator />`):

```tsx
<Separator orientation="vertical" className="h-6" />

<span className="text-sm font-medium">{fullName}</span>

<DropdownMenu>
```

- [ ] **Step 2: Remove the name line and its separator from the dropdown content**

```tsx
<DropdownMenuContent align="end">
  <DropdownMenuItem onClick={() => navigate("/account")}>
    <User className="mr-2 h-4 w-4" />{t("nav.account")}
  </DropdownMenuItem>
  <DropdownMenuItem onClick={handleLogout} className="text-destructive">
    <LogOut className="mr-2 h-4 w-4" />{t("nav.logout")}
  </DropdownMenuItem>
</DropdownMenuContent>
```

- [ ] **Step 3: Manually verify via the `run` skill**

- Desktop width: name shows in the header next to the avatar; dropdown no longer shows the name,
  just "Account" and "Logout".
- Narrow/mobile width: confirm the name span doesn't crowd out `LanguageSwitcher`/`ThemeToggle`/
  the hamburger — if it does, add `hidden sm:inline` to the span (not pre-added; only if the
  manual check shows crowding).
- Long names (e.g. three+ words) don't visibly break the header layout.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/AppLayout.tsx
git commit -m "feat: show user's name in the header, remove from dropdown"
```

- [ ] **Step 5: Open PR**

```bash
gh pr create --title "feat: show user's name in the header, remove from dropdown" \
  --body "Closes #7

Shows the logged-in user's full name in the header next to the avatar. Removes the redundant name line from the avatar dropdown, which now only has \"Account\" and \"Logout\".

Spec: \`docs/superpowers/specs/2026-08-11-nav-header-cleanup-batch.md\`" \
  --label enhancement --assignee willianbrecher
```
