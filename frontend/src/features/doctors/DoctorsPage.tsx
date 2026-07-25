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
