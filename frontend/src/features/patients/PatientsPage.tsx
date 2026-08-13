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
import { getPatients } from "@/api/patients";
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
              <Button size="sm" variant="outline" onClick={() => navigate(`/patients/${p.id}/edit`)}>
                {t("common.edit")}
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
