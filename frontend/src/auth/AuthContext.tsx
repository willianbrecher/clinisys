import { createContext, useContext, useState, useCallback, type ReactNode } from "react";
import { jwtDecode } from "jwt-decode";
import { useTheme } from "next-themes";
import i18n from "@/i18n";
import { login as apiLogin } from "@/api/auth";
import type { Role, ThemePreference } from "@/api/types";

interface JwtPayload {
  sub: string;
  role: Role;
  theme: ThemePreference;
  language: string;
  fullName: string;
  doctorId?: string;
  exp: number;
}

interface AuthState {
  userId: string;
  role: Role;
  fullName: string;
  doctorId?: string;
}

interface AuthContextValue extends AuthState {
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const TOKEN_KEY = "clinisys_token";

function decodeToken(token: string): JwtPayload {
  return jwtDecode<JwtPayload>(token);
}

function stateFromToken(token: string): AuthState {
  const payload = decodeToken(token);
  return {
    userId: payload.sub,
    role: payload.role,
    fullName: payload.fullName,
    doctorId: payload.doctorId,
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const { setTheme } = useTheme();
  const [auth, setAuth] = useState<AuthState | null>(() => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return null;
    try { return stateFromToken(token); } catch { return null; }
  });

  const login = useCallback(async (email: string, password: string) => {
    const token = await apiLogin(email, password);
    const payload = decodeToken(token);
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem("clinisys_language", payload.language);
    setTheme(payload.theme === "System" ? "system"
           : payload.theme === "Dark"   ? "dark" : "light");
    await i18n.changeLanguage(payload.language);
    setAuth(stateFromToken(token));
  }, [setTheme]);

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY);
    setAuth(null);
  }, []);

  return (
    <AuthContext.Provider value={{
      ...(auth ?? { userId: "", role: "Staff" as Role, fullName: "" }),
      isAuthenticated: auth !== null,
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
