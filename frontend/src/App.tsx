import { Routes, Route, Navigate } from "react-router-dom";
import { AppLayout } from "@/components/AppLayout";
import { ProtectedRoute } from "@/auth/ProtectedRoute";
import { LoginPage } from "@/features/auth/LoginPage";
import { DashboardPage } from "@/features/dashboard/DashboardPage";
import { PatientsPage } from "@/features/patients/PatientsPage";
import { PatientForm } from "@/features/patients/PatientForm";
import { DoctorsPage } from "@/features/doctors/DoctorsPage";
import { DoctorForm } from "@/features/doctors/DoctorForm";
import { AppointmentsPage } from "@/features/appointments/AppointmentsPage";
import { UsersPage } from "@/features/users/UsersPage";
import { SettingsPage } from "@/features/settings/SettingsPage";
import { AccountPage } from "@/features/account/AccountPage";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>
        <Route index element={<DashboardPage />} />

        <Route path="patients" element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><PatientsPage /></ProtectedRoute>} />
        <Route path="patients/new" element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><PatientForm /></ProtectedRoute>} />
        <Route path="patients/:id" element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><PatientForm /></ProtectedRoute>} />

        <Route path="doctors" element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><DoctorsPage /></ProtectedRoute>} />
        <Route path="doctors/:id" element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><DoctorForm /></ProtectedRoute>} />

        <Route path="appointments" element={<AppointmentsPage />} />

        <Route path="users" element={<ProtectedRoute allowedRoles={["Admin"]}><UsersPage /></ProtectedRoute>} />
        <Route path="settings" element={<ProtectedRoute allowedRoles={["Admin"]}><SettingsPage /></ProtectedRoute>} />
        <Route path="account" element={<AccountPage />} />

        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
