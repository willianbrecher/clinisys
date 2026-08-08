export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type Role = "Admin" | "Staff" | "Doctor";
export type ThemePreference = "Light" | "Dark" | "System";
export type AppointmentStatus =
  | "Scheduled" | "Confirmed" | "Completed" | "Cancelled" | "NoShow";

export interface PatientModel {
  id: string;
  fullName: string;
  dateOfBirth: string;
  phone: string;
  email?: string;
  notes?: string;
  isActive: boolean;
}

export interface DoctorModel {
  id: string;
  userId: string;
  fullName: string;
  email?: string;
  specialty: string;
  isActive: boolean;
}

export interface AppointmentModel {
  id: string;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  startsAt: string;
  durationMinutes: number;
  status: AppointmentStatus;
  notes?: string;
  createdAt: string;
}

export interface UserModel {
  id: string;
  email?: string;
  fullName: string;
  role: Role;
  themePreference: ThemePreference;
  languagePreference: string;
  isActive: boolean;
}

export interface ClinicSettingsModel {
  id: string;
  openTime: string;
  closeTime: string;
  openDays: string;
  logoBase64?: string;
}
