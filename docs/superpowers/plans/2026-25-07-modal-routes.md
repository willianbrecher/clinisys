# Modal Routes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert all create/edit/detail forms from full-page routes or state-driven dialogs into React Router v6 nested-route modals — the URL updates, the list stays mounted behind the dialog, and back-button/deep-link navigation work without extra code.

**Architecture:** Each list page wraps `<Outlet />` inside a Shadcn `<Dialog>`, using `useOutlet()` to drive `open`. Child routes render only form content (no Dialog wrapper). `onClose` navigates back to the list. `onSaved` refreshes list data. A shared `ModalContext` type is the contract between host and guest.

**Tech Stack:** React 18, React Router v6, Shadcn/UI Dialog, react-hook-form, react-i18next, TypeScript.

## Global Constraints

- React Router v6 — `useOutlet`, `useOutletContext`, `useNavigate`, `useParams`, `useLocation` all from `react-router-dom`
- Shadcn Dialog imports: `Dialog`, `DialogContent`, `DialogHeader`, `DialogTitle` from `@/components/ui/dialog`
- All new form content files live alongside their feature files (e.g., `frontend/src/features/patients/PatientFormContent.tsx`)
- `ModalContext` shared type lives at `frontend/src/types/modal.ts`
- No API modules changed — all `src/api/` files are untouched
- No validation schema changes — all `*.schema.ts` files unchanged
- Delete old form files only after verifying the new nested routes work

---

### Task 1: Shared type and i18n

**Files:**
- Create: `frontend/src/types/modal.ts`
- Modify: `frontend/src/locales/en-US/translation.json`
- Modify: `frontend/src/locales/pt-BR/translation.json`
- Modify: `frontend/src/locales/es-ES/translation.json`

**Interfaces:**
- Produces: `ModalContext` — `{ onClose: () => void; onSaved: () => void }` — consumed by all form content components in Tasks 2–5

- [ ] **Step 1: Create `frontend/src/types/modal.ts`**

```ts
export interface ModalContext {
  onClose: () => void;
  onSaved: () => void;
}
```

- [ ] **Step 2: Add `common.view` to en-US locale**

In `frontend/src/locales/en-US/translation.json`, add `"view": "View"` to the `"common"` object after `"select"`:

```json
"select": "Select...",
"view": "View"
```

- [ ] **Step 3: Add `common.view` to pt-BR locale**

In `frontend/src/locales/pt-BR/translation.json`, add after `"select"`:

```json
"select": "Selecionar...",
"view": "Visualizar"
```

- [ ] **Step 4: Add `common.view` to es-ES locale**

In `frontend/src/locales/es-ES/translation.json`, add after `"select"`:

```json
"select": "Seleccionar...",
"view": "Ver"
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/types/modal.ts frontend/src/locales
git commit -m "feat: add ModalContext type and common.view i18n key"
```

---

### Task 2: Patients modal route

**Files:**
- Create: `frontend/src/features/patients/PatientFormContent.tsx`
- Modify: `frontend/src/features/patients/PatientsPage.tsx`
- Modify: `frontend/src/App.tsx` (patients routes section)
- Delete: `frontend/src/features/patients/PatientForm.tsx`

**Interfaces:**
- Consumes: `ModalContext` from `@/types/modal`
- Produces: nested routes `patients/new` and `patients/:id/edit` rendering `PatientFormContent`

- [ ] **Step 1: Create `PatientFormContent.tsx`**

```tsx
import { useEffect } from "react";
import { useParams, useOutletContext } from "react-router-dom";
import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { getPatientById, createPatient, updatePatient } from "@/api/patients";
import { patientSchema, type PatientFormData } from "./patient.schema";
import type { ModalContext } from "@/types/modal";

export function PatientFormContent() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { onClose, onSaved } = useOutletContext<ModalContext>();
  const isEdit = !!id;

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<PatientFormData>({
    resolver: yupResolver(patientSchema) as unknown as Resolver<PatientFormData>,
  });

  useEffect(() => {
    if (id) {
      getPatientById(id).then((p) => reset({
        fullName: p.fullName, dateOfBirth: p.dateOfBirth,
        phone: p.phone, email: p.email ?? "", notes: p.notes ?? "",
      })).catch(() => toast.error("Failed to load patient."));
    }
  }, [id, reset]);

  const onSubmit = async (data: PatientFormData) => {
    try {
      if (isEdit) {
        await updatePatient(id!, data);
        toast.success("Patient updated.");
      } else {
        await createPatient(data);
        toast.success("Patient created.");
      }
      onSaved();
      onClose();
    } catch {
      toast.error("Failed to save patient.");
    }
  };

  return (
    <>
      <DialogHeader>
        <DialogTitle>{isEdit ? t("common.edit") : t("patients.new")}</DialogTitle>
      </DialogHeader>

      <form onSubmit={handleSubmit(onSubmit)} className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label>{t("patients.fullName")}</Label>
          <Input {...register("fullName")} />
          {errors.fullName && <p className="text-xs text-destructive">{errors.fullName.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("patients.dateOfBirth")}</Label>
          <Input type="date" {...register("dateOfBirth")} />
          {errors.dateOfBirth && <p className="text-xs text-destructive">{errors.dateOfBirth.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("patients.phone")}</Label>
          <Input {...register("phone")} />
          {errors.phone && <p className="text-xs text-destructive">{errors.phone.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label>{t("patients.email")}</Label>
          <Input type="email" {...register("email")} />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label>{t("patients.notes")}</Label>
          <Textarea {...register("notes")} rows={3} />
        </div>

        <div className="flex gap-2 sm:col-span-2">
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? t("common.loading") : t("common.save")}
          </Button>
          <Button type="button" variant="outline" onClick={onClose}>
            {t("common.cancel")}
          </Button>
        </div>
      </form>
    </>
  );
}
```

- [ ] **Step 2: Update `PatientsPage.tsx`**

Replace the full file content with:

```tsx
import { useEffect, useState, useCallback } from "react";
import { useNavigate, useOutlet, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Plus, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { getPatients, deactivatePatient } from "@/api/patients";
import type { PatientModel, PagedResult } from "@/api/types";

export function PatientsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const outlet = useOutlet();
  const [data, setData] = useState<PagedResult<PatientModel> | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);

  const load = useCallback(() => {
    getPatients({ search: search || undefined, page, pageSize: 20 })
      .then(setData)
      .catch(() => toast.error("Failed to load patients."));
  }, [search, page]);

  useEffect(() => { load(); }, [load]);

  const close = () => navigate("/patients");

  const handleDeactivate = async (id: string) => {
    try {
      await deactivatePatient(id);
      toast.success("Patient deactivated.");
      load();
    } catch {
      toast.error("Failed to deactivate patient.");
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2">
        <h1 className="text-2xl font-semibold">{t("patients.title")}</h1>
        <Button onClick={() => navigate("/patients/new")} size="sm">
          <Plus className="mr-1 h-4 w-4" />{t("patients.new")}
        </Button>
      </div>

      <div className="relative max-w-sm">
        <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
        <Input className="pl-8" placeholder={t("common.search")}
          value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
      </div>

      <div className="hidden md:block rounded-md border overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("patients.fullName")}</TableHead>
              <TableHead>{t("patients.phone")}</TableHead>
              <TableHead>{t("patients.email")}</TableHead>
              <TableHead>{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data?.items.map((p) => (
              <TableRow key={p.id}>
                <TableCell className="font-medium">{p.fullName}</TableCell>
                <TableCell>{p.phone}</TableCell>
                <TableCell>{p.email ?? "—"}</TableCell>
                <TableCell>
                  <div className="flex gap-2">
                    <Button size="sm" variant="outline" onClick={() => navigate(`/patients/${p.id}/edit`)}>
                      {t("common.edit")}
                    </Button>
                    <Button size="sm" variant="destructive" onClick={() => handleDeactivate(p.id)}>
                      {t("patients.deactivate")}
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {data?.items.length === 0 && (
              <TableRow><TableCell colSpan={4} className="text-center text-muted-foreground py-8">{t("common.noResults")}</TableCell></TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex flex-col gap-2 md:hidden">
        {data?.items.map((p) => (
          <div key={p.id} className="rounded-md border p-3 space-y-1">
            <p className="font-medium">{p.fullName}</p>
            <p className="text-sm text-muted-foreground">{p.phone}</p>
            {p.email && <p className="text-sm text-muted-foreground">{p.email}</p>}
            <div className="flex gap-2 pt-1">
              <Button size="sm" variant="outline" className="flex-1" onClick={() => navigate(`/patients/${p.id}/edit`)}>
                {t("common.edit")}
              </Button>
              <Button size="sm" variant="destructive" className="flex-1" onClick={() => handleDeactivate(p.id)}>
                {t("patients.deactivate")}
              </Button>
            </div>
          </div>
        ))}
        {data?.items.length === 0 && <p className="text-center text-muted-foreground py-8">{t("common.noResults")}</p>}
      </div>

      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button variant="outline" size="sm" disabled={page === 1} onClick={() => setPage(page - 1)}>
            {t("common.previous")}
          </Button>
          <span className="text-sm">{t("common.page")} {page} {t("common.of")} {data.totalPages}</span>
          <Button variant="outline" size="sm" disabled={page === data.totalPages} onClick={() => setPage(page + 1)}>
            {t("common.next")}
          </Button>
        </div>
      )}

      <Dialog open={!!outlet} onOpenChange={(open) => { if (!open) close(); }}>
        <DialogContent className="max-w-lg">
          <Outlet context={{ onClose: close, onSaved: load }} />
        </DialogContent>
      </Dialog>
    </div>
  );
}
```

- [ ] **Step 3: Update patients routes in `App.tsx`**

Replace the three flat patient routes:
```tsx
<Route path="patients" element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><PatientsPage /></ProtectedRoute>} />
<Route path="patients/new" element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><PatientForm /></ProtectedRoute>} />
<Route path="patients/:id" element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><PatientForm /></ProtectedRoute>} />
```

With nested routes:
```tsx
<Route path="patients"
  element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><PatientsPage /></ProtectedRoute>}>
  <Route path="new"      element={<PatientFormContent />} />
  <Route path=":id/edit" element={<PatientFormContent />} />
</Route>
```

Update the import section — remove `PatientForm`, add `PatientFormContent`:
```tsx
import { PatientFormContent } from "@/features/patients/PatientFormContent";
```

- [ ] **Step 4: Verify manually**

Run `npm run dev` in `frontend/`. Navigate to `/patients` — list renders normally. Click "New Patient" — URL changes to `/patients/new`, modal opens over the list. Fill form and save — modal closes, list refreshes. Click "Edit" on a patient — URL changes to `/patients/:id/edit`, modal opens with pre-filled data. Press browser back — modal closes without page reload.

- [ ] **Step 5: Delete `PatientForm.tsx`**

Delete `frontend/src/features/patients/PatientForm.tsx`.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/patients/ frontend/src/App.tsx
git commit -m "feat: convert patients form to modal route"
```

---

### Task 3: Doctors modal route

**Files:**
- Create: `frontend/src/features/doctors/DoctorFormContent.tsx`
- Modify: `frontend/src/features/doctors/DoctorsPage.tsx`
- Modify: `frontend/src/App.tsx` (doctors routes section)
- Delete: `frontend/src/features/doctors/DoctorForm.tsx`

**Interfaces:**
- Consumes: `ModalContext` from `@/types/modal`; `useAuth` for `role` check in `DoctorsPage`
- Produces: nested routes `doctors/:id/edit` (Admin) and `doctors/:id/detail` (Staff, read-only)

- [ ] **Step 1: Create `DoctorFormContent.tsx`**

```tsx
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
import type { ModalContext } from "@/types/modal";

export function DoctorFormContent() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { pathname } = useLocation();
  const isDetail = pathname.endsWith("/detail");
  const { onClose, onSaved } = useOutletContext<ModalContext>();

  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm<{ specialty: string }>();

  useEffect(() => {
    if (id) getDoctorById(id).then((d) => reset({ specialty: d.specialty })).catch(() => {});
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
```

- [ ] **Step 2: Update `DoctorsPage.tsx`**

Replace the full file content with:

```tsx
import { useEffect, useState, useCallback } from "react";
import { useNavigate, useOutlet, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { getDoctors } from "@/api/doctors";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useAuth } from "@/auth/AuthContext";
import type { DoctorModel, PagedResult } from "@/api/types";

export function DoctorsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const outlet = useOutlet();
  const { role } = useAuth();
  const [data, setData] = useState<PagedResult<DoctorModel> | null>(null);
  const [page, setPage] = useState(1);

  const load = useCallback(() => {
    getDoctors({ page, pageSize: 20 }).then(setData).catch(() => {});
  }, [page]);

  useEffect(() => { load(); }, [load]);

  const close = () => navigate("/doctors");

  const openDoctor = (id: string) => {
    if (role === "Admin") navigate(`/doctors/${id}/edit`);
    else navigate(`/doctors/${id}/detail`);
  };

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold">{t("doctors.title")}</h1>

      <div className="hidden md:block rounded-md border overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("patients.fullName")}</TableHead>
              <TableHead>{t("patients.email")}</TableHead>
              <TableHead>{t("doctors.specialty")}</TableHead>
              <TableHead>{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data?.items.map((d) => (
              <TableRow key={d.id}>
                <TableCell className="font-medium">{d.fullName}</TableCell>
                <TableCell>{d.email ?? "—"}</TableCell>
                <TableCell>{d.specialty}</TableCell>
                <TableCell>
                  <Button size="sm" variant="outline" onClick={() => openDoctor(d.id)}>
                    {role === "Admin" ? t("doctors.editSpecialty") : t("common.view")}
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <div className="flex flex-col gap-2 md:hidden">
        {data?.items.map((d) => (
          <div key={d.id} className="rounded-md border p-3 space-y-1">
            <p className="font-medium">{d.fullName}</p>
            <p className="text-sm text-muted-foreground">{d.specialty}</p>
            <Button size="sm" variant="outline" className="w-full mt-1" onClick={() => openDoctor(d.id)}>
              {role === "Admin" ? t("doctors.editSpecialty") : t("common.view")}
            </Button>
          </div>
        ))}
      </div>

      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button variant="outline" size="sm" disabled={page === 1} onClick={() => setPage(p => p - 1)}>{t("common.previous")}</Button>
          <span className="text-sm">{t("common.page")} {page} {t("common.of")} {data.totalPages}</span>
          <Button variant="outline" size="sm" disabled={page === data.totalPages} onClick={() => setPage(p => p + 1)}>{t("common.next")}</Button>
        </div>
      )}

      <Dialog open={!!outlet} onOpenChange={(open) => { if (!open) close(); }}>
        <DialogContent className="max-w-sm">
          <Outlet context={{ onClose: close, onSaved: load }} />
        </DialogContent>
      </Dialog>
    </div>
  );
}
```

- [ ] **Step 3: Update doctors routes in `App.tsx`**

Replace:
```tsx
<Route path="doctors" element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><DoctorsPage /></ProtectedRoute>} />
<Route path="doctors/:id" element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><DoctorForm /></ProtectedRoute>} />
```

With:
```tsx
<Route path="doctors"
  element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><DoctorsPage /></ProtectedRoute>}>
  <Route path=":id/edit"   element={<DoctorFormContent />} />
  <Route path=":id/detail" element={<DoctorFormContent />} />
</Route>
```

Update imports — remove `DoctorForm`, add `DoctorFormContent`:
```tsx
import { DoctorFormContent } from "@/features/doctors/DoctorFormContent";
```

- [ ] **Step 4: Verify manually**

As Admin: navigate to `/doctors`, click the button on a doctor — URL changes to `/doctors/:id/edit`, modal opens with specialty editable, save works. As Staff: same button shows "View", URL changes to `/doctors/:id/detail`, specialty input is disabled, no Save button.

- [ ] **Step 5: Delete `DoctorForm.tsx`**

Delete `frontend/src/features/doctors/DoctorForm.tsx`.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/doctors/ frontend/src/App.tsx
git commit -m "feat: convert doctors form to modal route with edit and detail modes"
```

---

### Task 4: Users modal route

**Files:**
- Create: `frontend/src/features/users/UserFormContent.tsx`
- Modify: `frontend/src/features/users/UsersPage.tsx`
- Modify: `frontend/src/App.tsx` (users routes section)
- Delete: `frontend/src/features/users/UserForm.tsx`

**Interfaces:**
- Consumes: `ModalContext` from `@/types/modal`; `createUser` from `@/api/users` (moved in from `UsersPage`)
- Produces: nested route `users/new` rendering `UserFormContent`

- [ ] **Step 1: Create `UserFormContent.tsx`**

```tsx
import { useOutletContext } from "react-router-dom";
import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { createUser } from "@/api/users";
import { createUserSchema, type CreateUserFormData } from "./user.schema";
import type { ModalContext } from "@/types/modal";

export function UserFormContent() {
  const { t } = useTranslation();
  const { onClose, onSaved } = useOutletContext<ModalContext>();
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<CreateUserFormData>({
    resolver: yupResolver(createUserSchema) as unknown as Resolver<CreateUserFormData>,
  });
  const role = watch("role");

  const onSubmit = async (formData: CreateUserFormData) => {
    try {
      await createUser({
        email: formData.email,
        fullName: formData.fullName,
        password: formData.password,
        role: formData.role as "Admin" | "Staff" | "Doctor",
        specialty: formData.specialty,
      });
      toast.success("User created.");
      onSaved();
      onClose();
    } catch {
      toast.error("Failed to create user.");
    }
  };

  return (
    <>
      <DialogHeader>
        <DialogTitle>{t("users.new")}</DialogTitle>
      </DialogHeader>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
        <div className="flex flex-col gap-1.5">
          <Label>{t("users.fullName")}</Label>
          <Input {...register("fullName")} />
          {errors.fullName && <p className="text-xs text-destructive">{errors.fullName.message}</p>}
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>{t("users.email")}</Label>
          <Input type="email" {...register("email")} />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>{t("users.password")}</Label>
          <Input type="password" {...register("password")} />
          {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>{t("users.role")}</Label>
          <select className="border rounded px-3 py-2 text-sm bg-background" {...register("role")}>
            <option value="Admin">{t("users.role_Admin")}</option>
            <option value="Staff">{t("users.role_Staff")}</option>
            <option value="Doctor">{t("users.role_Doctor")}</option>
          </select>
          {errors.role && <p className="text-xs text-destructive">{errors.role.message}</p>}
        </div>
        {role === "Doctor" && (
          <div className="flex flex-col gap-1.5">
            <Label>{t("users.specialty")}</Label>
            <Input {...register("specialty")} />
            {errors.specialty && <p className="text-xs text-destructive">{errors.specialty.message}</p>}
          </div>
        )}
        <div className="flex gap-2 pt-1">
          <Button type="submit" disabled={isSubmitting}>{t("common.create")}</Button>
          <Button type="button" variant="outline" onClick={onClose}>{t("common.cancel")}</Button>
        </div>
      </form>
    </>
  );
}
```

- [ ] **Step 2: Update `UsersPage.tsx`**

Replace the full file content with:

```tsx
import { useEffect, useState, useCallback } from "react";
import { useNavigate, useOutlet, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { getUsers, deactivateUser, resetPassword } from "@/api/users";
import type { UserModel, PagedResult } from "@/api/types";

export function UsersPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const outlet = useOutlet();
  const [data, setData] = useState<PagedResult<UserModel> | null>(null);
  const [resetTarget, setResetTarget] = useState<UserModel | null>(null);
  const [newPw, setNewPw] = useState("");
  const [page, setPage] = useState(1);

  const load = useCallback(() => {
    getUsers({ page, pageSize: 20 }).then(setData).catch(() => {});
  }, [page]);

  useEffect(() => { load(); }, [load]);

  const close = () => navigate("/users");

  const handleDeactivate = async (id: string) => {
    try { await deactivateUser(id); toast.success("User deactivated."); load(); }
    catch { toast.error("Failed to deactivate user."); }
  };

  const handleResetPw = async () => {
    if (!resetTarget || !newPw) return;
    try {
      await resetPassword(resetTarget.id, newPw);
      toast.success("Password reset.");
      setResetTarget(null);
      setNewPw("");
    } catch { toast.error("Failed to reset password."); }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">{t("users.title")}</h1>
        <Button size="sm" onClick={() => navigate("/users/new")}>
          <Plus className="mr-1 h-4 w-4" />{t("users.new")}
        </Button>
      </div>

      <div className="hidden md:block rounded-md border overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("users.fullName")}</TableHead>
              <TableHead>{t("users.email")}</TableHead>
              <TableHead>{t("users.role")}</TableHead>
              <TableHead>{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data?.items.map((u) => (
              <TableRow key={u.id}>
                <TableCell className="font-medium">{u.fullName}</TableCell>
                <TableCell>{u.email}</TableCell>
                <TableCell>{t(`users.role_${u.role}`)}</TableCell>
                <TableCell>
                  <div className="flex gap-2">
                    <Button size="sm" variant="outline" onClick={() => setResetTarget(u)}>{t("users.resetPassword")}</Button>
                    <Button size="sm" variant="destructive" onClick={() => handleDeactivate(u.id)}>{t("users.deactivate")}</Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <div className="flex flex-col gap-2 md:hidden">
        {data?.items.map((u) => (
          <div key={u.id} className="rounded-md border p-3 space-y-1">
            <p className="font-medium">{u.fullName}</p>
            <p className="text-sm text-muted-foreground">{u.email} · {t(`users.role_${u.role}`)}</p>
            <div className="flex gap-2 pt-1">
              <Button size="sm" variant="outline" className="flex-1" onClick={() => setResetTarget(u)}>{t("users.resetPassword")}</Button>
              <Button size="sm" variant="destructive" className="flex-1" onClick={() => handleDeactivate(u.id)}>{t("users.deactivate")}</Button>
            </div>
          </div>
        ))}
      </div>

      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button variant="outline" size="sm" disabled={page === 1} onClick={() => setPage(p => p - 1)}>{t("common.previous")}</Button>
          <span className="text-sm">{t("common.page")} {page} {t("common.of")} {data.totalPages}</span>
          <Button variant="outline" size="sm" disabled={page === data.totalPages} onClick={() => setPage(p => p + 1)}>{t("common.next")}</Button>
        </div>
      )}

      {/* Reset password dialog — stays state-driven, no deep-link value */}
      <Dialog open={!!resetTarget} onOpenChange={(o) => { if (!o) { setResetTarget(null); setNewPw(""); } }}>
        <DialogContent className="max-w-sm">
          <DialogHeader><DialogTitle>{t("users.resetPassword")}</DialogTitle></DialogHeader>
          <div className="flex flex-col gap-3">
            <p className="text-sm text-muted-foreground">{resetTarget?.fullName}</p>
            <input className="border rounded px-3 py-2 text-sm" type="password"
              placeholder={t("users.newPassword")} value={newPw} onChange={(e) => setNewPw(e.target.value)} />
            <div className="flex gap-2">
              <Button onClick={handleResetPw} disabled={newPw.length < 8}>{t("common.confirm")}</Button>
              <Button variant="outline" onClick={() => { setResetTarget(null); setNewPw(""); }}>{t("common.cancel")}</Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* New user modal — route-driven */}
      <Dialog open={!!outlet} onOpenChange={(open) => { if (!open) close(); }}>
        <DialogContent className="max-w-md">
          <Outlet context={{ onClose: close, onSaved: load }} />
        </DialogContent>
      </Dialog>
    </div>
  );
}
```

- [ ] **Step 3: Update users routes in `App.tsx`**

Replace:
```tsx
<Route path="users" element={<ProtectedRoute allowedRoles={["Admin"]}><UsersPage /></ProtectedRoute>} />
```

With:
```tsx
<Route path="users"
  element={<ProtectedRoute allowedRoles={["Admin"]}><UsersPage /></ProtectedRoute>}>
  <Route path="new" element={<UserFormContent />} />
</Route>
```

Add import:
```tsx
import { UserFormContent } from "@/features/users/UserFormContent";
```

- [ ] **Step 4: Verify manually**

Navigate to `/users`. Click "New User" — URL changes to `/users/new`, modal opens. Fill form, create — modal closes, list refreshes. Reset password dialog still works as before (state-driven, no URL change).

- [ ] **Step 5: Delete `UserForm.tsx`**

Delete `frontend/src/features/users/UserForm.tsx`.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/users/ frontend/src/App.tsx
git commit -m "feat: convert users form to modal route"
```

---

### Task 5: Appointments modal route

**Files:**
- Create: `frontend/src/features/appointments/AppointmentFormContent.tsx`
- Modify: `frontend/src/features/appointments/AppointmentsPage.tsx`
- Modify: `frontend/src/App.tsx` (appointments routes section)
- Delete: `frontend/src/features/appointments/AppointmentModal.tsx`

**Interfaces:**
- Consumes: `ModalContext` from `@/types/modal`; appointment data via `useLocation().state` (no single-fetch endpoint exists)
- Produces: nested routes `appointments/new`, `appointments/:id/edit`, `appointments/:id/detail`

- [ ] **Step 1: Create `AppointmentFormContent.tsx`**

```tsx
import { useEffect, useState } from "react";
import { useParams, useLocation, useOutletContext } from "react-router-dom";
import { useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { getPatients } from "@/api/patients";
import { getDoctors } from "@/api/doctors";
import { createAppointment, rescheduleAppointment, updateAppointmentStatus } from "@/api/appointments";
import { appointmentSchema, statusSchema, type AppointmentFormData, type StatusFormData } from "./appointment.schema";
import type { AppointmentModel, PatientModel, DoctorModel, AppointmentStatus } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import type { ModalContext } from "@/types/modal";

export function AppointmentFormContent() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { pathname, state } = useLocation();
  const { role, doctorId } = useAuth();
  const { onClose, onSaved } = useOutletContext<ModalContext>();

  const isDetail = pathname.endsWith("/detail");
  const appointment: AppointmentModel | undefined = state?.appointment;
  const defaultStartsAt: string | undefined = state?.defaultStartsAt;
  const isEdit = !!id;

  const [patients, setPatients] = useState<PatientModel[]>([]);
  const [doctors, setDoctors] = useState<DoctorModel[]>([]);

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<AppointmentFormData>({
    resolver: yupResolver(appointmentSchema) as unknown as Resolver<AppointmentFormData>,
  });

  const { register: registerStatus, handleSubmit: handleStatusSubmit, formState: { isSubmitting: isStatusSubmitting } } = useForm<StatusFormData>({
    resolver: yupResolver(statusSchema),
    defaultValues: { status: appointment?.status },
  });

  useEffect(() => {
    getPatients({ pageSize: 100 }).then((r) => setPatients(r.items)).catch(() => {});
    getDoctors({ pageSize: 100 }).then((r) => setDoctors(r.items)).catch(() => {});
  }, []);

  useEffect(() => {
    if (appointment) {
      reset({
        patientId: appointment.patientId,
        doctorId: appointment.doctorId,
        startsAt: appointment.startsAt.slice(0, 16),
        durationMinutes: appointment.durationMinutes,
        notes: appointment.notes ?? "",
      });
    } else {
      reset({ startsAt: defaultStartsAt?.slice(0, 16) ?? "", durationMinutes: 30 });
    }
  }, [appointment, defaultStartsAt, reset]);

  const onSubmit = async (data: AppointmentFormData) => {
    try {
      if (isEdit) {
        await rescheduleAppointment(appointment!.id, {
          startsAt: data.startsAt,
          durationMinutes: data.durationMinutes,
          notes: data.notes,
        });
        toast.success("Appointment rescheduled.");
      } else {
        await createAppointment(data);
        toast.success("Appointment created.");
      }
      onSaved();
      onClose();
    } catch {
      toast.error("Failed to save appointment.");
    }
  };

  const onStatusSubmit = async (data: StatusFormData) => {
    try {
      await updateAppointmentStatus(appointment!.id, data.status as AppointmentStatus);
      toast.success("Status updated.");
      onSaved();
      onClose();
    } catch {
      toast.error("Failed to update status.");
    }
  };

  return (
    <>
      <DialogHeader>
        <DialogTitle>
          {isDetail
            ? t("appointments.title")
            : isEdit
              ? t("appointments.reschedule")
              : t("appointments.new")}
        </DialogTitle>
      </DialogHeader>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
        <div className="flex flex-col gap-1.5">
          <Label>{t("appointments.patient")}</Label>
          <select
            className="border rounded px-3 py-2 text-sm bg-background"
            {...register("patientId")}
            disabled={isEdit || isDetail}
          >
            <option value="">{t("common.select")}</option>
            {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
          </select>
          {errors.patientId && <p className="text-xs text-destructive">{errors.patientId.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("appointments.doctor")}</Label>
          <select
            className="border rounded px-3 py-2 text-sm bg-background"
            {...register("doctorId")}
            disabled={isEdit || isDetail || role === "Doctor"}
          >
            <option value="">{t("common.select")}</option>
            {(role === "Doctor" ? doctors.filter((d) => d.id === doctorId) : doctors).map((d) => (
              <option key={d.id} value={d.id}>{d.fullName}</option>
            ))}
          </select>
          {errors.doctorId && <p className="text-xs text-destructive">{errors.doctorId.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("appointments.startsAt")}</Label>
          <Input type="datetime-local" {...register("startsAt")} disabled={isDetail} readOnly={isDetail} />
          {errors.startsAt && <p className="text-xs text-destructive">{errors.startsAt.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("appointments.durationMinutes")}</Label>
          <Input type="number" min={5} max={480} step={5} {...register("durationMinutes")} disabled={isDetail} readOnly={isDetail} />
          {errors.durationMinutes && <p className="text-xs text-destructive">{errors.durationMinutes.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("appointments.notes")}</Label>
          <Textarea rows={2} {...register("notes")} disabled={isDetail} readOnly={isDetail} />
        </div>

        <div className="flex gap-2">
          {!isDetail && (
            <Button type="submit" disabled={isSubmitting}>{t("common.save")}</Button>
          )}
          <Button type="button" variant="outline" onClick={onClose}>{t("common.cancel")}</Button>
        </div>
      </form>

      {isEdit && !isDetail && (
        <>
          <hr className="my-2" />
          <form onSubmit={handleStatusSubmit(onStatusSubmit)} className="flex gap-2 items-end">
            <div className="flex flex-col gap-1.5 flex-1">
              <Label>{t("appointments.status")}</Label>
              <select className="border rounded px-3 py-2 text-sm bg-background" {...registerStatus("status")}>
                <option value="Scheduled">{t("appointments.status_Scheduled")}</option>
                <option value="Confirmed">{t("appointments.status_Confirmed")}</option>
                <option value="Completed">{t("appointments.status_Completed")}</option>
                <option value="Cancelled">{t("appointments.status_Cancelled")}</option>
                <option value="NoShow">{t("appointments.status_NoShow")}</option>
              </select>
            </div>
            <Button type="submit" variant="outline" disabled={isStatusSubmitting}>
              {t("appointments.updateStatus")}
            </Button>
          </form>
        </>
      )}
    </>
  );
}
```

- [ ] **Step 2: Update `AppointmentsPage.tsx`**

Replace the full file content with:

```tsx
import { useEffect, useState, useCallback } from "react";
import { useNavigate, useOutlet, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin from "@fullcalendar/interaction";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { Plus, List, CalendarDays } from "lucide-react";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { getAppointments } from "@/api/appointments";
import { getClinicSettings } from "@/api/clinicSettings";
import type { AppointmentModel, ClinicSettingsModel } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";

type Tab = "list" | "calendar";

const STATUS_COLORS: Record<string, string> = {
  Scheduled: "#3b82f6",
  Confirmed: "#10b981",
  Completed: "#6b7280",
  Cancelled: "#ef4444",
  NoShow: "#f59e0b",
};

export function AppointmentsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const outlet = useOutlet();
  const { role, doctorId } = useAuth();
  const [tab, setTab] = useState<Tab>("calendar");
  const [data, setData] = useState<AppointmentModel[]>([]);
  const [listPage, setListPage] = useState(1);
  const [listTotal, setListTotal] = useState(0);
  const [settings, setSettings] = useState<ClinicSettingsModel | null>(null);
  const [calendarKey, setCalendarKey] = useState(0);

  useEffect(() => {
    getClinicSettings().then(setSettings).catch(() => {});
  }, []);

  const loadList = useCallback(() => {
    getAppointments({
      doctorId: role === "Doctor" ? doctorId : undefined,
      page: listPage, pageSize: 20,
    }).then((r) => { setData(r.items); setListTotal(r.totalPages); }).catch(() => {});
  }, [role, doctorId, listPage]);

  const loadCalendar = useCallback((start: string, end: string) => {
    return getAppointments({
      doctorId: role === "Doctor" ? doctorId : undefined,
      startDate: start, endDate: end, pageSize: 200,
    }).then((r) => r.items).catch(() => [] as AppointmentModel[]);
  }, [role, doctorId]);

  useEffect(() => { if (tab === "list") loadList(); }, [tab, loadList]);

  const close = () => navigate("/appointments");

  const onSaved = () => {
    if (tab === "list") loadList();
    else setCalendarKey((k) => k + 1);
  };

  const openAppointment = (a: AppointmentModel) => {
    if (role === "Doctor") navigate(`/appointments/${a.id}/detail`, { state: { appointment: a } });
    else navigate(`/appointments/${a.id}/edit`, { state: { appointment: a } });
  };

  const openDays = settings?.openDays
    ? settings.openDays.split(",").map(Number)
    : [1, 2, 3, 4, 5];

  const slotMinTime = settings ? settings.openTime : "08:00:00";
  const slotMaxTime = settings ? settings.closeTime : "18:00:00";

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-2">
        <h1 className="text-2xl font-semibold">{t("appointments.title")}</h1>
        <div className="flex gap-2 flex-wrap">
          <Button variant={tab === "list" ? "default" : "outline"} size="sm" onClick={() => setTab("list")}>
            <List className="mr-1 h-4 w-4" />{t("appointments.listView")}
          </Button>
          <Button variant={tab === "calendar" ? "default" : "outline"} size="sm" onClick={() => setTab("calendar")}>
            <CalendarDays className="mr-1 h-4 w-4" />{t("appointments.calendarView")}
          </Button>
          <Button size="sm" onClick={() => navigate("/appointments/new")}>
            <Plus className="mr-1 h-4 w-4" />{t("appointments.new")}
          </Button>
        </div>
      </div>

      {tab === "list" && (
        <>
          <div className="hidden md:block rounded-md border overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t("appointments.patient")}</TableHead>
                  <TableHead>{t("appointments.doctor")}</TableHead>
                  <TableHead>{t("appointments.startsAt")}</TableHead>
                  <TableHead>{t("appointments.status")}</TableHead>
                  <TableHead>{t("common.actions")}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.map((a) => (
                  <TableRow key={a.id}>
                    <TableCell>{a.patientName}</TableCell>
                    <TableCell>{a.doctorName}</TableCell>
                    <TableCell>{new Intl.DateTimeFormat(undefined, { dateStyle: "short", timeStyle: "short" }).format(new Date(a.startsAt))}</TableCell>
                    <TableCell>
                      <span className="px-2 py-0.5 rounded-full text-xs text-white" style={{ background: STATUS_COLORS[a.status] }}>
                        {a.status}
                      </span>
                    </TableCell>
                    <TableCell>
                      <Button size="sm" variant="outline" onClick={() => openAppointment(a)}>
                        {role === "Doctor" ? t("common.view") : t("common.edit")}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {data.length === 0 && (
                  <TableRow><TableCell colSpan={5} className="text-center text-muted-foreground py-8">{t("common.noResults")}</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </div>

          <div className="flex flex-col gap-2 md:hidden">
            {data.map((a) => (
              <div key={a.id} className="rounded-md border p-3 space-y-1">
                <div className="flex justify-between">
                  <p className="font-medium">{a.patientName}</p>
                  <span className="px-2 py-0.5 rounded-full text-xs text-white" style={{ background: STATUS_COLORS[a.status] }}>{a.status}</span>
                </div>
                <p className="text-sm text-muted-foreground">{a.doctorName}</p>
                <p className="text-sm">{new Intl.DateTimeFormat(undefined, { dateStyle: "short", timeStyle: "short" }).format(new Date(a.startsAt))}</p>
                <Button size="sm" variant="outline" className="w-full" onClick={() => openAppointment(a)}>
                  {role === "Doctor" ? t("common.view") : t("common.edit")}
                </Button>
              </div>
            ))}
            {data.length === 0 && <p className="text-center text-muted-foreground py-8">{t("common.noResults")}</p>}
          </div>

          {listTotal > 1 && (
            <div className="flex items-center justify-center gap-2">
              <Button variant="outline" size="sm" disabled={listPage === 1} onClick={() => setListPage(p => p - 1)}>{t("common.previous")}</Button>
              <span className="text-sm">{t("common.page")} {listPage} {t("common.of")} {listTotal}</span>
              <Button variant="outline" size="sm" disabled={listPage === listTotal} onClick={() => setListPage(p => p + 1)}>{t("common.next")}</Button>
            </div>
          )}
        </>
      )}

      {tab === "calendar" && (
        <div className="[&_.fc]:text-sm">
          <FullCalendar
            key={calendarKey}
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin] as any[]}
            initialView="timeGridWeek"
            headerToolbar={{
              left: "prev,next today",
              center: "title",
              right: "dayGridMonth,timeGridWeek,timeGridDay",
            }}
            slotMinTime={slotMinTime}
            slotMaxTime={slotMaxTime}
            hiddenDays={[0, 1, 2, 3, 4, 5, 6].filter((d) => !openDays.includes(d))}
            selectable
            selectConstraint={{ daysOfWeek: openDays, startTime: slotMinTime, endTime: slotMaxTime }}
            dateClick={(info: { dateStr: string }) =>
              navigate("/appointments/new", { state: { defaultStartsAt: info.dateStr } })
            }
            eventClick={(info) => {
              const appt = data.find((a) => a.id === info.event.id);
              if (appt) openAppointment(appt);
            }}
            events={async (info, successCb) => {
              const items = await loadCalendar(info.startStr, info.endStr);
              setData(items);
              successCb(items.map((a) => ({
                id: a.id,
                title: `${a.patientName} (${a.doctorName})`,
                start: a.startsAt,
                end: new Date(new Date(a.startsAt).getTime() + a.durationMinutes * 60000).toISOString(),
                backgroundColor: STATUS_COLORS[a.status],
                borderColor: STATUS_COLORS[a.status],
              })));
            }}
            height="auto"
          />
        </div>
      )}

      <Dialog open={!!outlet} onOpenChange={(open) => { if (!open) close(); }}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <Outlet context={{ onClose: close, onSaved }} />
        </DialogContent>
      </Dialog>
    </div>
  );
}
```

- [ ] **Step 3: Update appointments routes in `App.tsx`**

Replace:
```tsx
<Route path="appointments" element={<AppointmentsPage />} />
```

With:
```tsx
<Route path="appointments" element={<AppointmentsPage />}>
  <Route path="new"        element={<AppointmentFormContent />} />
  <Route path=":id/edit"   element={<AppointmentFormContent />} />
  <Route path=":id/detail" element={<AppointmentFormContent />} />
</Route>
```

Add import, remove `AppointmentModal` import (it was in `AppointmentsPage`, not `App.tsx`, so only add):
```tsx
import { AppointmentFormContent } from "@/features/appointments/AppointmentFormContent";
```

Final `App.tsx` imports for reference:
```tsx
import { PatientFormContent } from "@/features/patients/PatientFormContent";
import { DoctorFormContent } from "@/features/doctors/DoctorFormContent";
import { AppointmentFormContent } from "@/features/appointments/AppointmentFormContent";
import { UserFormContent } from "@/features/users/UserFormContent";
```

- [ ] **Step 4: Verify manually**

In calendar view: click a date cell — URL changes to `/appointments/new`, modal opens with pre-filled start time. Create appointment — modal closes, calendar reloads (`calendarKey` increments). Click a calendar event — URL changes to `/appointments/:id/edit` or `/detail`, modal opens with data from navigation state. In list view: Edit/View button navigates correctly, list refreshes after save. Doctor role sees read-only modal for all appointments.

- [ ] **Step 5: Delete `AppointmentModal.tsx`**

Delete `frontend/src/features/appointments/AppointmentModal.tsx`.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/appointments/ frontend/src/App.tsx
git commit -m "feat: convert appointments form to modal route with edit and detail modes"
```
