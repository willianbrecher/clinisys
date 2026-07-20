import { useTheme } from "next-themes";
import { useTranslation } from "react-i18next";
import { Sun, Moon, Monitor } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { updatePreferences } from "@/api/account";
import { useAuth } from "@/auth/AuthContext";
import type { ThemePreference } from "@/api/types";

export function ThemeToggle() {
  const { t } = useTranslation();
  const { setTheme, theme } = useTheme();
  const { isAuthenticated } = useAuth();

  const apply = (value: "light" | "dark" | "system") => {
    setTheme(value);
    if (isAuthenticated) {
      const pref: ThemePreference = value === "light" ? "Light" : value === "dark" ? "Dark" : "System";
      updatePreferences(pref, localStorage.getItem("clinisys_language") ?? "en-US").catch(() => {});
    }
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon">
          {theme === "dark" ? <Moon className="h-4 w-4" /> : theme === "light" ? <Sun className="h-4 w-4" /> : <Monitor className="h-4 w-4" />}
          <span className="sr-only">Toggle theme</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => apply("light")}><Sun className="mr-2 h-4 w-4" />{t("theme.light")}</DropdownMenuItem>
        <DropdownMenuItem onClick={() => apply("dark")}><Moon className="mr-2 h-4 w-4" />{t("theme.dark")}</DropdownMenuItem>
        <DropdownMenuItem onClick={() => apply("system")}><Monitor className="mr-2 h-4 w-4" />{t("theme.system")}</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
