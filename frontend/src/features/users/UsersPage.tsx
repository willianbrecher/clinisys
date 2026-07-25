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
