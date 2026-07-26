import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from "react";
import { useTheme } from "next-themes";
import i18n from "@/i18n";
import { login as apiLogin } from "@/api/auth";
import { getMe } from "@/api/account";
import type { Role } from "@/api/types";

interface AuthState {
  userId: string;
  role: Role;
  fullName: string;
  doctorId?: string;
}

interface AuthContextValue extends AuthState {
  isAuthenticated: boolean;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const TOKEN_KEY = "clinisys_token";

function applyPreferences(theme: string, language: string, setTheme: (t: string) => void) {
  setTheme(theme === "System" ? "system" : theme === "Dark" ? "dark" : "light");
  i18n.changeLanguage(language);
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const { setTheme } = useTheme();
  const [auth, setAuth] = useState<AuthState | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) { setLoading(false); return; }

    getMe()
      .then(me => {
        applyPreferences(me.theme, me.language, setTheme);
        setAuth({ userId: me.userId, role: me.role, fullName: me.fullName, doctorId: me.doctorId ?? undefined });
      })
      .catch(() => localStorage.removeItem(TOKEN_KEY))
      .finally(() => setLoading(false));
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const login = useCallback(async (email: string, password: string) => {
    const token = await apiLogin(email, password);
    localStorage.setItem(TOKEN_KEY, token);
    const me = await getMe();
    applyPreferences(me.theme, me.language, setTheme);
    setAuth({ userId: me.userId, role: me.role, fullName: me.fullName, doctorId: me.doctorId ?? undefined });
  }, [setTheme]);

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY);
    setAuth(null);
  }, []);

  return (
    <AuthContext.Provider value={{
      ...(auth ?? { userId: "", role: "Staff" as Role, fullName: "" }),
      isAuthenticated: auth !== null,
      loading,
      login,
      logout,
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
