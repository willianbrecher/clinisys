import { useTranslation } from "react-i18next";
import { Languages } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { updatePreferences } from "@/api/account";
import { useAuth } from "@/auth/AuthContext";
import type { ThemePreference } from "@/api/types";
import { useTheme } from "next-themes";

const LOCALES = ["en-US", "pt-BR", "es-ES"] as const;

export function LanguageSwitcher() {
  const { t, i18n } = useTranslation();
  const { isAuthenticated } = useAuth();
  const { theme } = useTheme();

  const apply = async (locale: string) => {
    await i18n.changeLanguage(locale);
    localStorage.setItem("clinisys_language", locale);
    if (isAuthenticated) {
      const pref: ThemePreference = theme === "light" ? "Light" : theme === "dark" ? "Dark" : "System";
      updatePreferences(pref, locale).catch(() => {});
    }
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon">
          <Languages className="h-4 w-4" />
          <span className="sr-only">Language</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {LOCALES.map((l) => (
          <DropdownMenuItem key={l} onClick={() => apply(l)}>
            {t(`language.${l}`)}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
