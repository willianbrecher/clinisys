# CLAUDE.md

This file provides repository-wide guidance to Claude Code. CliniSys is a monorepo with two
independently built projects:

| Folder | Stack | Read its CLAUDE.md when you |
|---|---|---|
| `backend/` | .NET 8 / C# 12 / Clean Architecture / MediatR / EF Core / PostgreSQL | touch backend code, write an EF migration, add an endpoint, or run/build the API |
| `frontend/` | React 18 / TypeScript / Vite / Tailwind / Shadcn UI | touch frontend code, add a component/feature, or run/build the app |

Each subfolder's `CLAUDE.md` is the source of truth for that stack's commands, coding
conventions, and architecture — read it before making changes there. This root file only holds
conventions that apply across the whole repo, regardless of which folder you're working in.

## Pull request conventions

**Always split a piece of work into separate PRs by layer — never bundle them:**

- **Backend PR** — any change under `backend/`.
- **Frontend PR** — any change under `frontend/`.
- **Docs/spec PR** — any change outside both (`docs/`, `README.md`, `.env.example`, root-level
  config, etc.), when the work also touches backend and/or frontend.

A single feature almost always spans layers; each layer still gets its own branch and its own PR.
Mixing backend and frontend changes in one PR makes review painful — a reviewer looking at
EF Core/Identity config doesn't want to scroll past React components, and vice versa.

When a feature is developed on a long-lived feature branch (e.g. `feature/appointment-reminders`),
the per-layer PRs target that feature branch, not `main`; the feature branch itself gets one PR
into `main` once all its layer PRs have landed.

**Every PR must, at creation time:**
- Get the `feature` label (or whatever label matches the work — `bug`, `documentation`, etc. —
  `feature` is the default for anything adding new functionality).
- Be assigned to `willianbrecher`.
- Reference its tracking issue in the body, using the issue number from the phase/feature this
  work belongs to.
  - **One PR closes one issue:** use `Closes #N` — GitHub auto-closes the issue when this PR
    merges.
  - **Multiple PRs address the same issue** (the normal case, since backend/frontend/docs are
    always split — see above): every one of those PRs uses `Refs #N`, never `Closes #N`. Using
    `Closes #N` on more than one PR auto-closes the issue the moment the *first* of them merges,
    even though the others (and the rest of the phase) are still open. Close the issue manually
    once every PR for that phase has actually merged.

Use `gh pr edit <N> --add-label feature --add-assignee willianbrecher` right after `gh pr create`
if the label/assignee weren't set at creation time — don't leave a PR unlabeled or unassigned.

## Branch naming

Every branch Claude creates must be `feature/<slug>` or `fix/<slug>` and reference an issue —
no exceptions. Only the user may bypass this rule themselves.

## Comments

**No comments that just restate the spec or business logic**, in backend or frontend code — only
comment a non-obvious WHY (a framework quirk, a workaround, a hidden constraint). Applies
everywhere in the repo, not just the folder currently being edited.
