import { Routes, Route, Navigate } from "react-router-dom";
import { AppLayout } from "@/components/AppLayout";
import { ProtectedRoute } from "@/auth/ProtectedRoute";
import { LoginPage } from "@/features/auth/LoginPage";
import { DashboardPage } from "@/features/dashboard/DashboardPage";
import { PatientsPage } from "@/features/patients/PatientsPage";
import { PatientFormContent } from "@/features/patients/PatientFormContent";
import { DoctorsPage } from "@/features/doctors/DoctorsPage";
import { DoctorFormContent } from "@/features/doctors/DoctorFormContent";
import { HealthPlansPage } from "@/features/healthPlans/HealthPlansPage";
import { HealthPlanFormContent } from "@/features/healthPlans/HealthPlanFormContent";
import { AppointmentsPage } from "@/features/appointments/AppointmentsPage";
import { AppointmentFormContent } from "@/features/appointments/AppointmentFormContent";
import { UsersPage } from "@/features/users/UsersPage";
import { UserFormContent } from "@/features/users/UserFormContent";
import { SettingsPage } from "@/features/settings/SettingsPage";
import { AccountPage } from "@/features/account/AccountPage";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>
        <Route index element={<DashboardPage />} />

        <Route path="patients"
          element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><PatientsPage /></ProtectedRoute>}>
          <Route path="new"      element={<PatientFormContent />} />
          <Route path=":id/edit" element={<PatientFormContent />} />
        </Route>

        <Route path="doctors"
          element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><DoctorsPage /></ProtectedRoute>}>
          <Route path=":id/edit"   element={<DoctorFormContent />} />
          <Route path=":id/detail" element={<DoctorFormContent />} />
        </Route>

        <Route path="health-plans"
          element={<ProtectedRoute allowedRoles={["Admin","Staff"]}><HealthPlansPage /></ProtectedRoute>}>
          <Route path="new"      element={<HealthPlanFormContent />} />
          <Route path=":id/edit" element={<HealthPlanFormContent />} />
        </Route>

        <Route path="appointments" element={<AppointmentsPage />}>
          <Route path="new"        element={<AppointmentFormContent />} />
          <Route path=":id/edit"   element={<AppointmentFormContent />} />
          <Route path=":id/detail" element={<AppointmentFormContent />} />
        </Route>

        <Route path="users"
          element={<ProtectedRoute allowedRoles={["Admin"]}><UsersPage /></ProtectedRoute>}>
          <Route path="new" element={<UserFormContent />} />
        </Route>
        <Route path="settings" element={<ProtectedRoute allowedRoles={["Admin"]}><SettingsPage /></ProtectedRoute>} />
        <Route path="account" element={<AccountPage />} />

        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
