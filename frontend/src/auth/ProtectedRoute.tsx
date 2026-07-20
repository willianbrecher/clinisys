import { Navigate } from "react-router-dom";
import { useAuth } from "./AuthContext";
import type { Role } from "@/api/types";

interface Props {
  children: React.ReactNode;
  allowedRoles?: Role[];
}

export function ProtectedRoute({ children, allowedRoles }: Props) {
  const { isAuthenticated, role } = useAuth();

  if (!isAuthenticated) return <Navigate to="/login" replace />;

  if (allowedRoles && !allowedRoles.includes(role))
    return <Navigate to="/" replace />;

  return <>{children}</>;
}
