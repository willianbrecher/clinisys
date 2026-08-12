import { useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  LayoutDashboard, Users, UserRound, CalendarDays,
  Settings, User, LogOut, Menu, Shield, ChevronDown,
} from "lucide-react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/sheet";
import { Separator } from "@/components/ui/separator";
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { ThemeToggle } from "./ThemeToggle";
import { LanguageSwitcher } from "./LanguageSwitcher";
import { useAuth } from "@/auth/AuthContext";
import { useClinicSettings } from "@/features/settings/useClinicSettings";
import type { Role } from "@/api/types";

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors
   ${isActive ? "bg-primary text-primary-foreground" : "hover:bg-accent hover:text-accent-foreground"}`;

interface NavItem {
  to: string;
  icon: React.ReactNode;
  label: string;
  roles: Role[];
}

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

export function AppLayout() {
  const { t } = useTranslation();
  const { fullName, logout } = useAuth();
  const navigate = useNavigate();
  const [mobileOpen, setMobileOpen] = useState(false);
  const { settings } = useClinicSettings();

  const initials = fullName.split(" ").slice(0, 2).map((n) => n[0]).join("").toUpperCase();

  const handleLogout = () => { logout(); navigate("/login"); };

  const LogoMark = ({ className }: { className?: string }) =>
    settings?.logoBase64
      ? <img src={settings.logoBase64} alt="logo" className={`object-contain rounded ${className ?? "h-8 w-8"}`} />
      : <div className={`rounded bg-primary flex items-center justify-center text-primary-foreground font-bold ${className ?? "h-8 w-8"} text-sm`}>C</div>;

  return (
    <div className="flex min-h-screen">
      {/* Desktop sidebar */}
      <aside className="hidden lg:flex lg:w-56 lg:flex-col lg:fixed lg:inset-y-0 border-r bg-card">
        <div className="flex h-14 items-center gap-2 px-4 border-b">
          <LogoMark />
          <span className="font-semibold text-sm">CliniSys</span>
        </div>
        <div className="flex-1 overflow-y-auto">
          <SidebarContent />
        </div>
      </aside>

      {/* Main content */}
      <div className="flex-1 lg:pl-56 flex flex-col min-h-screen">
        {/* Header */}
        <header className="sticky top-0 z-40 flex h-14 items-center gap-2 border-b bg-background px-4">
          {/* Mobile hamburger */}
          <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
            <SheetTrigger asChild>
              <Button variant="ghost" size="icon" className="lg:hidden">
                <Menu className="h-5 w-5" />
              </Button>
            </SheetTrigger>
            <SheetContent side="left" className="w-56 p-0">
              <div className="flex h-14 items-center gap-2 px-4 border-b">
                <LogoMark />
                <span className="font-semibold text-sm">CliniSys</span>
              </div>
              <SidebarContent onNav={() => setMobileOpen(false)} />
            </SheetContent>
          </Sheet>

          {/* Mobile logo */}
          <div className="flex items-center gap-2 lg:hidden">
            <LogoMark className="h-7 w-7" />
            <span className="font-semibold text-sm">CliniSys</span>
          </div>

          <div className="flex-1" />

          <LanguageSwitcher />
          <ThemeToggle />

          <Separator orientation="vertical" className="h-6" />

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="relative h-8 w-8 rounded-full p-0">
                <Avatar className="h-8 w-8">
                  <AvatarFallback>{initials}</AvatarFallback>
                </Avatar>
              </Button>
            </DropdownMenuTrigger>
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
          </DropdownMenu>
        </header>

        <main className="flex-1 p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
