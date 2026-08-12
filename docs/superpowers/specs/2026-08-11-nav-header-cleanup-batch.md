# CliniSys — Side Menu & Header Cleanup Batch Spec

Date: 2026-08-11
Status: Draft
Issues: [#4](https://github.com/willianbrecher/clinisys/issues/4), [#6](https://github.com/willianbrecher/clinisys/issues/6), [#7](https://github.com/willianbrecher/clinisys/issues/7)

## 1. Goal

Three navigation/header cleanup items, all touching `frontend/src/components/AppLayout.tsx`:

1. Remove "My Account" from the side menu (#4).
2. Group "Users" and "Settings" under a single "Administration" side menu item (#6).
3. Show the logged-in user's name in the header next to the avatar, removing it from the avatar
   dropdown (#7).

Bundled into one spec because they share a file, but each still gets its own branch/PR/commit per
root `CLAUDE.md` (one issue per branch). Implementation order matters — see plan.

## 2. Current behavior — confirmed

Full relevant source: `frontend/src/components/AppLayout.tsx`.

**Side menu** (`SidebarContent`, lines 32-55) is a flat `NavItem[]` array, filtered by role:

```tsx
const links: NavItem[] = [
  { to: "/", ..., roles: ["Admin","Staff","Doctor"] },
  { to: "/patients", ..., roles: ["Admin","Staff"] },
  { to: "/doctors", ..., roles: ["Admin","Staff"] },
  { to: "/appointments", ..., roles: ["Admin","Staff","Doctor"] },
  { to: "/users", ..., roles: ["Admin"] },
  { to: "/settings", ..., roles: ["Admin"] },
  { to: "/account", ..., roles: ["Admin","Staff","Doctor"] },
];
```

**Header/avatar** (lines 119-137): only the avatar (initials fallback) is shown in the header —
no name. The dropdown content shows the name as a plain div, then a separator, then the account
items:

```tsx
<DropdownMenuContent align="end">
  <div className="px-2 py-1.5 text-sm font-medium">{fullName}</div>
  <Separator />
  <DropdownMenuItem onClick={() => navigate("/account")}>
    <User className="mr-2 h-4 w-4" />{t("nav.account")}
  </DropdownMenuItem>
  <DropdownMenuItem onClick={handleLogout} className="text-destructive">
    <LogOut className="mr-2 h-4 w-4" />{t("nav.logout")}
  </DropdownMenuItem>
</DropdownMenuContent>
```

`fullName` is already destructured from `useAuth()` at `AppLayout.tsx:59` and used to compute
`initials` — no new plumbing needed for #7.

**"My Account" has two independent entry points today**, both navigating to `/account`
(`AccountPage.tsx`, routed at `App.tsx:48`, open to all three roles): the side-menu item
(line 43) and the avatar dropdown item (lines 130-132). Removing the side-menu one (#4) does
**not** orphan the page — the dropdown item stays. #7 only removes the plain-text name div
(line 128), not the dropdown's "Account" `DropdownMenuItem` — no conflict between the two issues.

No Accordion/Collapsible primitive is installed (`frontend/src/components/ui` has no
`accordion.tsx`/`collapsible.tsx`, and none of `@radix-ui/react-accordion` /
`@radix-ui/react-collapsible` are in `package.json`). Confirmed with the user: build the
"Administration" group as a plain `useState` expand/collapse toggle in `SidebarContent`, no new
dependency — not a new `/administration` route/page.

## 3. #4 — Remove "My Account" from the side menu

Delete the `{ to: "/account", ... }` entry (line 43) from the `links` array. `User` (the icon it
used) stays imported — still used by the dropdown's "Account" item (line 131).

## 4. #6 — Group Users + Settings under "Administration"

Split the flat `links` array into `topLinks` (dashboard/patients/doctors/appointments) and
`adminLinks` (users/settings), and render `adminLinks` inside a collapsible group gated on
`role === "Admin"` (both children are already Admin-only, so this preserves current visibility —
no non-Admin ever saw Users/Settings, and none will see "Administration" either):

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

Needs two new `lucide-react` icon imports (`Shield`, `ChevronDown`) and one new locale key,
`nav.administration`, in all three bundles (`en-US`: "Administration", `es-ES`: "Administración",
`pt-BR`: "Administração").

No routing change — `/users` and `/settings` stay independent top-level routes in `App.tsx`; this
is menu-UI-only grouping, per the confirmed approach.

## 5. #7 — User's name in the header, removed from dropdown

Add the name next to the avatar in the header (before the `DropdownMenu`, inside the same flex
row):

```tsx
<span className="text-sm font-medium">{fullName}</span>
<DropdownMenu>
  ...
```

Remove the name div and its trailing separator from the dropdown content:

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

No new locale key — reuses `fullName`, already fetched.

On narrow/mobile widths the header already hides the sidebar and swaps in a hamburger + mobile
logo (lines 90-110); the name span sits in the same flexible row as `LanguageSwitcher`/
`ThemeToggle`/avatar, to the left of the vertical separator before the avatar block — verify at
mobile width during manual testing that it doesn't crowd out the theme/language controls (may
need `hidden sm:inline` if it does — left as a manual-testing call, not pre-emptively added).

## 6. Non-goals

- No new `/administration` route or landing page (#6) — menu-grouping only, per confirmed scope.
- No change to `/account`'s route, role-gating, or page content (#4) — only its side-menu entry
  point is removed; the page and its dropdown entry point are untouched.
- No avatar image/photo support — `AvatarFallback` initials stay as-is (#7).
