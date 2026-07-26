# CliniSys Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the CliniSys React SPA — patient/doctor/appointment scheduling UI with CQRS-backed REST API, Shadcn/UI, i18n, dark/light theme, and a FullCalendar appointment view.

**Architecture:** Vite + React 18 + TypeScript. Feature-per-folder under `src/features/`. Shared Shadcn components in `src/components/`. Axios modules per resource in `src/api/`. Auth via JWT decoded in `AuthContext`.

**Tech Stack:** React 18, TypeScript, Vite, Shadcn/UI, Tailwind CSS, React Router v6, Axios, React Hook Form + Yup, next-themes, i18next + react-i18next + i18next-browser-languagedetector, @fullcalendar/react + daygrid + timegrid + interaction, Sonner.

## Global Constraints

- No MUI — Shadcn/UI + Tailwind CSS only; all colors via CSS variables, never inline
- All user-visible strings use `useTranslation()` — no hard-coded text in JSX
- Mobile-first Tailwind breakpoints (`sm` 640 px, `md` 768 px, `lg` 1024 px)
- JWT stored in `localStorage` key `clinisys_token`; decoded with `jwt-decode`
- Axios instance adds `Authorization: Bearer <token>` on every request; 401 → redirect `/login`
- React Hook Form + Yup for all forms; schemas in `*.schema.ts` colocated per feature
- Images uploaded via `FileReader.readAsDataURL`; client-side 512 KB guard before encoding
- `next-themes` attribute mode: `attribute="class"` on `<html>`; CSS variable layer drives all theming
- No test files for v1 (components are composable; tests can be added without restructuring)

---

### Task 1: Project Scaffold

**Files:**
- Create: `frontend/` (Vite project)
- Create: `frontend/tailwind.config.ts`
- Create: `frontend/tsconfig.json` (updated)
- Create: `frontend/vite.config.ts` (updated with proxy)
- Create: `frontend/src/index.css` (Shadcn + Tailwind base)
- Create: `frontend/components.json` (Shadcn config)

**Interfaces:**
- Produces: buildable Vite project with Shadcn/UI, Tailwind, and all npm packages installed

- [ ] **Step 1: Scaffold Vite project**

```bash
cd clinisys
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
```

- [ ] **Step 2: Install all npm packages**

```bash
cd frontend

# Routing + HTTP
npm install react-router-dom axios

# Forms + validation
npm install react-hook-form yup @hookform/resolvers

# Theming
npm install next-themes

# i18n
npm install i18next react-i18next i18next-browser-languagedetector

# Calendar
npm install @fullcalendar/react @fullcalendar/daygrid @fullcalendar/timegrid @fullcalendar/interaction

# JWT decode
npm install jwt-decode

# Notifications
npm install sonner

# Tailwind + Shadcn deps
npm install -D tailwindcss postcss autoprefixer @tailwindcss/typography
npx tailwindcss init -p

# Shadcn/UI (run the init after Tailwind is configured)
npm install class-variance-authority clsx tailwind-merge lucide-react
npm install @radix-ui/react-dialog @radix-ui/react-dropdown-menu @radix-ui/react-select
npm install @radix-ui/react-avatar @radix-ui/react-tabs @radix-ui/react-toast
npm install @radix-ui/react-label @radix-ui/react-separator @radix-ui/react-slot
npm install @radix-ui/react-checkbox @radix-ui/react-popover @radix-ui/react-scroll-area
npm install @radix-ui/react-sheet 2>/dev/null || true
```

> **Note:** Use `npx shadcn@latest init` for the interactive Shadcn setup (answers below), then add individual components as needed per task.

```bash
npx shadcn@latest init
# Answers:
# Style: Default
# Base color: Neutral
# CSS variables: Yes
```

- [ ] **Step 3: Configure Tailwind**

`frontend/tailwind.config.ts`:
```ts
import type { Config } from "tailwindcss";

const config: Config = {
  darkMode: ["class"],
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        border:      "hsl(var(--border))",
        input:       "hsl(var(--input))",
        ring:        "hsl(var(--ring))",
        background:  "hsl(var(--background))",
        foreground:  "hsl(var(--foreground))",
        primary: {
          DEFAULT:    "hsl(var(--primary))",
          foreground: "hsl(var(--primary-foreground))",
        },
        secondary: {
          DEFAULT:    "hsl(var(--secondary))",
          foreground: "hsl(var(--secondary-foreground))",
        },
        muted: {
          DEFAULT:    "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT:    "hsl(var(--accent))",
          foreground: "hsl(var(--accent-foreground))",
        },
        destructive: {
          DEFAULT:    "hsl(var(--destructive))",
          foreground: "hsl(var(--destructive-foreground))",
        },
        card: {
          DEFAULT:    "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
        popover: {
          DEFAULT:    "hsl(var(--popover))",
          foreground: "hsl(var(--popover-foreground))",
        },
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
    },
  },
  plugins: [],
};

export default config;
```

- [ ] **Step 4: Configure Vite proxy**

`frontend/vite.config.ts`:
```ts
import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const backendUrl = env.VITE_BACKEND_URL ?? "http://localhost:5110";
  return {
    plugins: [react()],
    resolve: {
      alias: { "@": path.resolve(__dirname, "./src") },
    },
    server: {
      proxy: {
        "/api": { target: backendUrl, changeOrigin: true },
        "/connect": { target: backendUrl, changeOrigin: true },
      },
    },
  };
});
```

> **Note:** `loadEnv` is required to read `.env` variables inside `vite.config.ts` — `process.env.VITE_BACKEND_URL` does not pick up `.env` files without it. Set `VITE_BACKEND_URL` in `.env` to override the default `http://localhost:5110`.

Also update `tsconfig.json` to add the path alias:
```json
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "baseUrl": ".",
    "paths": { "@/*": ["./src/*"] }
  },
  "include": ["src"]
}
```

- [ ] **Step 5: Add Shadcn components used throughout the app**

```bash
cd frontend
npx shadcn@latest add button input label card avatar badge
npx shadcn@latest add dialog sheet tabs table
npx shadcn@latest add dropdown-menu select checkbox
npx shadcn@latest add toast sonner separator
npx shadcn@latest add form
```

- [ ] **Step 6: Create cn utility**

`frontend/src/lib/utils.ts`:
```ts
import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
```

- [ ] **Step 7: Verify build**

```bash
cd frontend && npm run build
```
Expected: `✓ built in ...`

- [ ] **Step 8: Commit**

```bash
git add frontend/
git commit -m "chore: scaffold CliniSys frontend with Vite, Shadcn/UI, and Tailwind"
```

---

### Task 2: CSS Variables + Theme Setup

**Files:**
- Modify: `frontend/src/index.css`
- Create: `frontend/src/theme/ThemeProvider.tsx`

**Interfaces:**
- Produces: `:root` and `.dark` CSS variable layers; `ThemeProvider` wrapping the app

- [ ] **Step 1: Write index.css with CSS variables and FullCalendar overrides**

`frontend/src/index.css`:
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 222.2 84% 4.9%;
    --card: 0 0% 100%;
    --card-foreground: 222.2 84% 4.9%;
    --popover: 0 0% 100%;
    --popover-foreground: 222.2 84% 4.9%;
    --primary: 221.2 83.2% 53.3%;
    --primary-foreground: 210 40% 98%;
    --secondary: 210 40% 96.1%;
    --secondary-foreground: 222.2 47.4% 11.2%;
    --muted: 210 40% 96.1%;
    --muted-foreground: 215.4 16.3% 46.9%;
    --accent: 210 40% 96.1%;
    --accent-foreground: 222.2 47.4% 11.2%;
    --destructive: 0 84.2% 60.2%;
    --destructive-foreground: 210 40% 98%;
    --border: 214.3 31.8% 91.4%;
    --input: 214.3 31.8% 91.4%;
    --ring: 221.2 83.2% 53.3%;
    --radius: 0.5rem;
  }

  .dark {
    --background: 222.2 84% 4.9%;
    --foreground: 210 40% 98%;
    --card: 222.2 84% 4.9%;
    --card-foreground: 210 40% 98%;
    --popover: 222.2 84% 4.9%;
    --popover-foreground: 210 40% 98%;
    --primary: 217.2 91.2% 59.8%;
    --primary-foreground: 222.2 47.4% 11.2%;
    --secondary: 217.2 32.6% 17.5%;
    --secondary-foreground: 210 40% 98%;
    --muted: 217.2 32.6% 17.5%;
    --muted-foreground: 215 20.2% 65.1%;
    --accent: 217.2 32.6% 17.5%;
    --accent-foreground: 210 40% 98%;
    --destructive: 0 62.8% 30.6%;
    --destructive-foreground: 210 40% 98%;
    --border: 217.2 32.6% 17.5%;
    --input: 217.2 32.6% 17.5%;
    --ring: 224.3 76.3% 48%;
  }
}

@layer base {
  * { @apply border-border; }
  body { @apply bg-background text-foreground; }
}

/* FullCalendar theme overrides */
.fc {
  --fc-border-color: hsl(var(--border));
  --fc-button-bg-color: hsl(var(--primary));
  --fc-button-border-color: hsl(var(--primary));
  --fc-button-hover-bg-color: hsl(var(--primary) / 0.9);
  --fc-button-active-bg-color: hsl(var(--primary) / 0.8);
  --fc-today-bg-color: hsl(var(--accent) / 0.4);
  --fc-page-bg-color: hsl(var(--background));
  --fc-neutral-bg-color: hsl(var(--muted));
  --fc-list-event-hover-bg-color: hsl(var(--accent));
  color: hsl(var(--foreground));
}
```

- [ ] **Step 2: Create ThemeProvider**

`frontend/src/theme/ThemeProvider.tsx`:
```tsx
import { ThemeProvider as NextThemesProvider } from "next-themes";
import type { ReactNode } from "react";

interface Props {
  children: ReactNode;
}

export function ThemeProvider({ children }: Props) {
  return (
    <NextThemesProvider attribute="class" defaultTheme="system" enableSystem>
      {children}
    </NextThemesProvider>
  );
}
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/index.css frontend/src/theme/
git commit -m "feat: add CSS variable theme layer and ThemeProvider"
```

---

### Task 3: i18n Setup

**Files:**
- Create: `frontend/src/i18n.ts`
- Create: `frontend/src/locales/en-US/translation.json`
- Create: `frontend/src/locales/pt-BR/translation.json`
- Create: `frontend/src/locales/es-ES/translation.json`

**Interfaces:**
- Produces: `i18next` initialised with 3 locales; `useTranslation()` available across the app

- [ ] **Step 1: Create i18n initialiser**

`frontend/src/i18n.ts`:
```ts
import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languagedetector";

import enUS from "./locales/en-US/translation.json";
import ptBR from "./locales/pt-BR/translation.json";
import esES from "./locales/es-ES/translation.json";

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      "en-US": { translation: enUS },
      "pt-BR": { translation: ptBR },
      "es-ES": { translation: esES },
    },
    fallbackLng: "en-US",
    supportedLngs: ["en-US", "pt-BR", "es-ES"],
    interpolation: { escapeValue: false },
    detection: {
      order: ["localStorage", "navigator"],
      caches: ["localStorage"],
      lookupLocalStorage: "clinisys_language",
    },
  });

export default i18n;
```

- [ ] **Step 2: Create translation files**

`frontend/src/locales/en-US/translation.json`:
```json
{
  "common": {
    "save": "Save",
    "cancel": "Cancel",
    "delete": "Delete",
    "edit": "Edit",
    "create": "Create",
    "search": "Search",
    "loading": "Loading...",
    "noResults": "No results found.",
    "confirm": "Confirm",
    "remove": "Remove",
    "actions": "Actions",
    "page": "Page",
    "of": "of",
    "previous": "Previous",
    "next": "Next"
  },
  "nav": {
    "dashboard": "Dashboard",
    "patients": "Patients",
    "doctors": "Doctors",
    "appointments": "Appointments",
    "users": "Users",
    "settings": "Settings",
    "account": "Account",
    "logout": "Logout"
  },
  "auth": {
    "login": "Sign In",
    "email": "Email",
    "password": "Password",
    "changePassword": "Change Password",
    "currentPassword": "Current Password",
    "newPassword": "New Password",
    "loginError": "Invalid email or password."
  },
  "patients": {
    "title": "Patients",
    "new": "New Patient",
    "fullName": "Full Name",
    "dateOfBirth": "Date of Birth",
    "phone": "Phone",
    "email": "Email",
    "notes": "Notes",
    "deactivate": "Deactivate Patient",
    "deactivateConfirm": "Are you sure you want to deactivate this patient?"
  },
  "doctors": {
    "title": "Doctors",
    "specialty": "Specialty",
    "editSpecialty": "Edit Specialty"
  },
  "appointments": {
    "title": "Appointments",
    "new": "New Appointment",
    "list": "List",
    "calendar": "Calendar",
    "patient": "Patient",
    "doctor": "Doctor",
    "startsAt": "Start Time",
    "duration": "Duration (min)",
    "status": "Status",
    "notes": "Notes",
    "reschedule": "Reschedule",
    "status_Scheduled": "Scheduled",
    "status_Confirmed": "Confirmed",
    "status_Completed": "Completed",
    "status_Cancelled": "Cancelled",
    "status_NoShow": "No Show"
  },
  "users": {
    "title": "Users",
    "new": "New User",
    "fullName": "Full Name",
    "email": "Email",
    "role": "Role",
    "specialty": "Specialty",
    "password": "Password",
    "deactivate": "Deactivate",
    "resetPassword": "Reset Password",
    "newPassword": "New Password",
    "role_Admin": "Admin",
    "role_Staff": "Staff",
    "role_Doctor": "Doctor"
  },
  "settings": {
    "title": "Clinic Settings",
    "openTime": "Opening Time",
    "closeTime": "Closing Time",
    "openDays": "Open Days",
    "logo": "Clinic Logo",
    "uploadLogo": "Upload Logo",
    "removeLogo": "Remove Logo",
    "day_0": "Sunday",
    "day_1": "Monday",
    "day_2": "Tuesday",
    "day_3": "Wednesday",
    "day_4": "Thursday",
    "day_5": "Friday",
    "day_6": "Saturday"
  },
  "account": {
    "title": "My Account",
    "profilePicture": "Profile Picture",
    "uploadPicture": "Upload Picture",
    "removePicture": "Remove Picture",
    "preferences": "Preferences",
    "theme": "Theme",
    "language": "Language",
    "theme_Light": "Light",
    "theme_Dark": "Dark",
    "theme_System": "System"
  },
  "theme": {
    "light": "Light",
    "dark": "Dark",
    "system": "System"
  },
  "language": {
    "en-US": "English",
    "pt-BR": "Português",
    "es-ES": "Español"
  }
}
```

`frontend/src/locales/pt-BR/translation.json`:
```json
{
  "common": {
    "save": "Salvar",
    "cancel": "Cancelar",
    "delete": "Excluir",
    "edit": "Editar",
    "create": "Criar",
    "search": "Buscar",
    "loading": "Carregando...",
    "noResults": "Nenhum resultado encontrado.",
    "confirm": "Confirmar",
    "remove": "Remover",
    "actions": "Ações",
    "page": "Página",
    "of": "de",
    "previous": "Anterior",
    "next": "Próximo"
  },
  "nav": {
    "dashboard": "Painel",
    "patients": "Pacientes",
    "doctors": "Médicos",
    "appointments": "Consultas",
    "users": "Usuários",
    "settings": "Configurações",
    "account": "Minha Conta",
    "logout": "Sair"
  },
  "auth": {
    "login": "Entrar",
    "email": "E-mail",
    "password": "Senha",
    "changePassword": "Alterar Senha",
    "currentPassword": "Senha Atual",
    "newPassword": "Nova Senha",
    "loginError": "E-mail ou senha inválidos."
  },
  "patients": {
    "title": "Pacientes",
    "new": "Novo Paciente",
    "fullName": "Nome Completo",
    "dateOfBirth": "Data de Nascimento",
    "phone": "Telefone",
    "email": "E-mail",
    "notes": "Observações",
    "deactivate": "Desativar Paciente",
    "deactivateConfirm": "Tem certeza que deseja desativar este paciente?"
  },
  "doctors": {
    "title": "Médicos",
    "specialty": "Especialidade",
    "editSpecialty": "Editar Especialidade"
  },
  "appointments": {
    "title": "Consultas",
    "new": "Nova Consulta",
    "list": "Lista",
    "calendar": "Calendário",
    "patient": "Paciente",
    "doctor": "Médico",
    "startsAt": "Horário de Início",
    "duration": "Duração (min)",
    "status": "Status",
    "notes": "Observações",
    "reschedule": "Reagendar",
    "status_Scheduled": "Agendada",
    "status_Confirmed": "Confirmada",
    "status_Completed": "Realizada",
    "status_Cancelled": "Cancelada",
    "status_NoShow": "Não Compareceu"
  },
  "users": {
    "title": "Usuários",
    "new": "Novo Usuário",
    "fullName": "Nome Completo",
    "email": "E-mail",
    "role": "Papel",
    "specialty": "Especialidade",
    "password": "Senha",
    "deactivate": "Desativar",
    "resetPassword": "Redefinir Senha",
    "newPassword": "Nova Senha",
    "role_Admin": "Administrador",
    "role_Staff": "Recepcionista",
    "role_Doctor": "Médico"
  },
  "settings": {
    "title": "Configurações da Clínica",
    "openTime": "Horário de Abertura",
    "closeTime": "Horário de Fechamento",
    "openDays": "Dias de Funcionamento",
    "logo": "Logo da Clínica",
    "uploadLogo": "Enviar Logo",
    "removeLogo": "Remover Logo",
    "day_0": "Domingo",
    "day_1": "Segunda-feira",
    "day_2": "Terça-feira",
    "day_3": "Quarta-feira",
    "day_4": "Quinta-feira",
    "day_5": "Sexta-feira",
    "day_6": "Sábado"
  },
  "account": {
    "title": "Minha Conta",
    "profilePicture": "Foto de Perfil",
    "uploadPicture": "Enviar Foto",
    "removePicture": "Remover Foto",
    "preferences": "Preferências",
    "theme": "Tema",
    "language": "Idioma",
    "theme_Light": "Claro",
    "theme_Dark": "Escuro",
    "theme_System": "Sistema"
  },
  "theme": {
    "light": "Claro",
    "dark": "Escuro",
    "system": "Sistema"
  },
  "language": {
    "en-US": "English",
    "pt-BR": "Português",
    "es-ES": "Español"
  }
}
```

`frontend/src/locales/es-ES/translation.json`:
```json
{
  "common": {
    "save": "Guardar",
    "cancel": "Cancelar",
    "delete": "Eliminar",
    "edit": "Editar",
    "create": "Crear",
    "search": "Buscar",
    "loading": "Cargando...",
    "noResults": "No se encontraron resultados.",
    "confirm": "Confirmar",
    "remove": "Eliminar",
    "actions": "Acciones",
    "page": "Página",
    "of": "de",
    "previous": "Anterior",
    "next": "Siguiente"
  },
  "nav": {
    "dashboard": "Panel",
    "patients": "Pacientes",
    "doctors": "Médicos",
    "appointments": "Citas",
    "users": "Usuarios",
    "settings": "Configuración",
    "account": "Mi Cuenta",
    "logout": "Cerrar sesión"
  },
  "auth": {
    "login": "Iniciar Sesión",
    "email": "Correo electrónico",
    "password": "Contraseña",
    "changePassword": "Cambiar Contraseña",
    "currentPassword": "Contraseña Actual",
    "newPassword": "Nueva Contraseña",
    "loginError": "Correo o contraseña inválidos."
  },
  "patients": {
    "title": "Pacientes",
    "new": "Nuevo Paciente",
    "fullName": "Nombre Completo",
    "dateOfBirth": "Fecha de Nacimiento",
    "phone": "Teléfono",
    "email": "Correo electrónico",
    "notes": "Notas",
    "deactivate": "Desactivar Paciente",
    "deactivateConfirm": "¿Está seguro de que desea desactivar este paciente?"
  },
  "doctors": {
    "title": "Médicos",
    "specialty": "Especialidad",
    "editSpecialty": "Editar Especialidad"
  },
  "appointments": {
    "title": "Citas",
    "new": "Nueva Cita",
    "list": "Lista",
    "calendar": "Calendario",
    "patient": "Paciente",
    "doctor": "Médico",
    "startsAt": "Hora de inicio",
    "duration": "Duración (min)",
    "status": "Estado",
    "notes": "Notas",
    "reschedule": "Reprogramar",
    "status_Scheduled": "Programada",
    "status_Confirmed": "Confirmada",
    "status_Completed": "Completada",
    "status_Cancelled": "Cancelada",
    "status_NoShow": "No se presentó"
  },
  "users": {
    "title": "Usuarios",
    "new": "Nuevo Usuario",
    "fullName": "Nombre Completo",
    "email": "Correo electrónico",
    "role": "Rol",
    "specialty": "Especialidad",
    "password": "Contraseña",
    "deactivate": "Desactivar",
    "resetPassword": "Restablecer Contraseña",
    "newPassword": "Nueva Contraseña",
    "role_Admin": "Administrador",
    "role_Staff": "Recepcionista",
    "role_Doctor": "Médico"
  },
  "settings": {
    "title": "Configuración de la Clínica",
    "openTime": "Hora de apertura",
    "closeTime": "Hora de cierre",
    "openDays": "Días de atención",
    "logo": "Logo de la Clínica",
    "uploadLogo": "Subir Logo",
    "removeLogo": "Eliminar Logo",
    "day_0": "Domingo",
    "day_1": "Lunes",
    "day_2": "Martes",
    "day_3": "Miércoles",
    "day_4": "Jueves",
    "day_5": "Viernes",
    "day_6": "Sábado"
  },
  "account": {
    "title": "Mi Cuenta",
    "profilePicture": "Foto de Perfil",
    "uploadPicture": "Subir Foto",
    "removePicture": "Eliminar Foto",
    "preferences": "Preferencias",
    "theme": "Tema",
    "language": "Idioma",
    "theme_Light": "Claro",
    "theme_Dark": "Oscuro",
    "theme_System": "Sistema"
  },
  "theme": {
    "light": "Claro",
    "dark": "Oscuro",
    "system": "Sistema"
  },
  "language": {
    "en-US": "English",
    "pt-BR": "Português",
    "es-ES": "Español"
  }
}
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/i18n.ts frontend/src/locales/
git commit -m "feat: add i18n setup with en-US, pt-BR, es-ES translations"
```

---

### Task 4: API Client

**Files:**
- Create: `frontend/src/api/client.ts`
- Create: `frontend/src/api/auth.ts`
- Create: `frontend/src/api/patients.ts`
- Create: `frontend/src/api/doctors.ts`
- Create: `frontend/src/api/appointments.ts`
- Create: `frontend/src/api/users.ts`
- Create: `frontend/src/api/clinicSettings.ts`
- Create: `frontend/src/api/account.ts`

**Interfaces:**
- Produces: typed Axios functions for every backend endpoint; used by all feature components

- [ ] **Step 1: Create shared types**

`frontend/src/api/types.ts`:
```ts
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
}

export interface ClinicSettingsModel {
  id: string;
  openTime: string;
  closeTime: string;
  openDays: string;
  logoBase64?: string;
}
```

- [ ] **Step 2: Create Axios client with interceptors**

`frontend/src/api/client.ts`:
```ts
import axios from "axios";
import i18n from "@/i18n";

const client = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "",
  headers: { "Content-Type": "application/json" },
});

client.interceptors.request.use((config) => {
  const token = localStorage.getItem("clinisys_token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  config.headers["Accept-Language"] = i18n.language ?? "en-US";
  return config;
});

client.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem("clinisys_token");
      window.location.href = "/login";
    }
    return Promise.reject(err);
  }
);

export default client;
```

- [ ] **Step 3: Create API modules**

`frontend/src/api/auth.ts`:
```ts
import client from "./client";

export async function login(email: string, password: string): Promise<string> {
  const body = new URLSearchParams({
    grant_type: "password",
    username: email,
    password,
    scope: "openid",
  });
  const res = await client.post<{ access_token: string }>("/connect/token", body, {
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
  });
  return res.data.access_token;
}

export async function changePassword(currentPassword: string, newPassword: string) {
  await client.post("/api/auth/change-password", { currentPassword, newPassword });
}
```

`frontend/src/api/patients.ts`:
```ts
import client from "./client";
import type { PagedResult, PatientModel } from "./types";

export const getPatients = (params: { search?: string; page?: number; pageSize?: number }) =>
  client.get<PagedResult<PatientModel>>("/api/patients", { params }).then((r) => r.data);

export const getPatientById = (id: string) =>
  client.get<PatientModel>(`/api/patients/${id}`).then((r) => r.data);

export const createPatient = (data: {
  fullName: string; dateOfBirth: string; phone: string; email?: string; notes?: string;
}) => client.post<{ id: string }>("/api/patients", data).then((r) => r.data.id);

export const updatePatient = (id: string, data: {
  fullName: string; dateOfBirth: string; phone: string; email?: string; notes?: string;
}) => client.put(`/api/patients/${id}`, data);

export const deactivatePatient = (id: string) =>
  client.delete(`/api/patients/${id}`);
```

`frontend/src/api/doctors.ts`:
```ts
import client from "./client";
import type { PagedResult, DoctorModel } from "./types";

export const getDoctors = (params?: { page?: number; pageSize?: number }) =>
  client.get<PagedResult<DoctorModel>>("/api/doctors", { params }).then((r) => r.data);

export const getDoctorById = (id: string) =>
  client.get<DoctorModel>(`/api/doctors/${id}`).then((r) => r.data);

export const updateDoctor = (id: string, specialty: string) =>
  client.patch(`/api/doctors/${id}`, { specialty });
```

`frontend/src/api/appointments.ts`:
```ts
import client from "./client";
import type { PagedResult, AppointmentModel, AppointmentStatus } from "./types";

export interface AppointmentFilters {
  doctorId?: string;
  patientId?: string;
  date?: string;
  startDate?: string;
  endDate?: string;
  status?: AppointmentStatus;
  page?: number;
  pageSize?: number;
}

export const getAppointments = (params: AppointmentFilters) =>
  client.get<PagedResult<AppointmentModel>>("/api/appointments", { params }).then((r) => r.data);

export const createAppointment = (data: {
  patientId: string; doctorId: string; startsAt: string;
  durationMinutes: number; notes?: string;
}) => client.post<{ id: string }>("/api/appointments", data).then((r) => r.data.id);

export const rescheduleAppointment = (id: string, data: {
  startsAt: string; durationMinutes: number;
}) => client.put(`/api/appointments/${id}`, data);

export const updateAppointmentStatus = (id: string, status: AppointmentStatus) =>
  client.patch(`/api/appointments/${id}/status`, { status });
```

`frontend/src/api/users.ts`:
```ts
import client from "./client";
import type { PagedResult, UserModel, Role } from "./types";

export const getUsers = (params?: { page?: number; pageSize?: number }) =>
  client.get<PagedResult<UserModel>>("/api/users", { params }).then((r) => r.data);

export const createUser = (data: {
  email: string; fullName: string; password: string; role: Role; specialty?: string;
}) => client.post<{ id: string }>("/api/users", data).then((r) => r.data.id);

export const deactivateUser = (id: string) =>
  client.patch(`/api/users/${id}/deactivate`);

export const resetPassword = (id: string, newPassword: string) =>
  client.post(`/api/users/${id}/reset-password`, { newPassword });
```

`frontend/src/api/clinicSettings.ts`:
```ts
import client from "./client";
import type { ClinicSettingsModel } from "./types";

export const getClinicSettings = () =>
  client.get<ClinicSettingsModel>("/api/clinic-settings").then((r) => r.data);

export const updateClinicSettings = (data: {
  openTime: string; closeTime: string; openDays: string; logoBase64?: string | null;
}) => client.put("/api/clinic-settings", data);
```

`frontend/src/api/account.ts`:
```ts
import client from "./client";
import type { ThemePreference } from "./types";

export const updateProfilePicture = (profilePictureBase64: string | null) =>
  client.patch("/api/account/profile-picture", { profilePictureBase64 });

export const updatePreferences = (theme: ThemePreference, language: string) =>
  client.patch("/api/account/preferences", { theme, language });
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/api/
git commit -m "feat: add Axios API client and typed API modules"
```

---

### Task 5: Auth — AuthContext + ProtectedRoute

**Files:**
- Create: `frontend/src/auth/AuthContext.tsx`
- Create: `frontend/src/auth/ProtectedRoute.tsx`

**Interfaces:**
- Produces: `useAuth()` hook exposing `role`, `userId`, `doctorId`, `fullName`, `login()`, `logout()`; `ProtectedRoute` for guarding pages

- [ ] **Step 1: Create AuthContext**

`frontend/src/auth/AuthContext.tsx`:
```tsx
import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from "react";
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
```

- [ ] **Step 2: Create ProtectedRoute**

`frontend/src/auth/ProtectedRoute.tsx`:
```tsx
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
```

- [ ] **Step 3: Wire up main.tsx**

`frontend/src/main.tsx`:
```tsx
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { Toaster } from "sonner";
import "./index.css";
import "./i18n";
import { ThemeProvider } from "./theme/ThemeProvider";
import { AuthProvider } from "./auth/AuthContext";
import App from "./App";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter>
      <ThemeProvider>
        <AuthProvider>
          <App />
          <Toaster richColors position="top-right" />
        </AuthProvider>
      </ThemeProvider>
    </BrowserRouter>
  </StrictMode>
);
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/auth/ frontend/src/main.tsx
git commit -m "feat: add AuthContext with JWT decode and ProtectedRoute"
```

---

### Task 6: AppLayout — Sidebar, Header, ThemeToggle, LanguageSwitcher

**Files:**
- Create: `frontend/src/components/AppLayout.tsx`
- Create: `frontend/src/components/ThemeToggle.tsx`
- Create: `frontend/src/components/LanguageSwitcher.tsx`

**Interfaces:**
- Produces: responsive shell used by every authenticated page; sidebar on `lg+`, Sheet drawer below `lg`

- [ ] **Step 1: Create ThemeToggle**

`frontend/src/components/ThemeToggle.tsx`:
```tsx
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
```

- [ ] **Step 2: Create LanguageSwitcher**

`frontend/src/components/LanguageSwitcher.tsx`:
```tsx
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
```

- [ ] **Step 3: Create AppLayout**

`frontend/src/components/AppLayout.tsx`:
```tsx
import { useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  LayoutDashboard, Users, UserRound, CalendarDays,
  Settings, User, LogOut, Menu,
} from "lucide-react";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
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

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors
   ${isActive ? "bg-primary text-primary-foreground" : "hover:bg-accent hover:text-accent-foreground"}`;

function SidebarContent({ onNav }: { onNav?: () => void }) {
  const { t } = useTranslation();
  const { role } = useAuth();

  const links = [
    { to: "/", icon: <LayoutDashboard className="h-4 w-4" />, label: t("nav.dashboard"), roles: ["Admin","Staff","Doctor"] },
    { to: "/patients", icon: <UserRound className="h-4 w-4" />, label: t("nav.patients"), roles: ["Admin","Staff"] },
    { to: "/doctors", icon: <Users className="h-4 w-4" />, label: t("nav.doctors"), roles: ["Admin","Staff"] },
    { to: "/appointments", icon: <CalendarDays className="h-4 w-4" />, label: t("nav.appointments"), roles: ["Admin","Staff","Doctor"] },
    { to: "/users", icon: <Users className="h-4 w-4" />, label: t("nav.users"), roles: ["Admin"] },
    { to: "/settings", icon: <Settings className="h-4 w-4" />, label: t("nav.settings"), roles: ["Admin"] },
    { to: "/account", icon: <User className="h-4 w-4" />, label: t("nav.account"), roles: ["Admin","Staff","Doctor"] },
  ] as const;

  return (
    <nav className="flex flex-col gap-1 p-4">
      {links.filter((l) => (l.roles as readonly string[]).includes(role)).map((l) => (
        <NavLink key={l.to} to={l.to} end={l.to === "/"} className={navLinkClass} onClick={onNav}>
          {l.icon}{l.label}
        </NavLink>
      ))}
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

  return (
    <div className="flex min-h-screen">
      {/* Desktop sidebar */}
      <aside className="hidden lg:flex lg:w-56 lg:flex-col lg:fixed lg:inset-y-0 border-r bg-card">
        <div className="flex items-center gap-2 px-4 py-4 border-b">
          {settings?.logoBase64
            ? <img src={settings.logoBase64} alt="logo" className="h-8 w-8 object-contain rounded" />
            : <div className="h-8 w-8 rounded bg-primary flex items-center justify-center text-primary-foreground text-sm font-bold">C</div>}
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
              <div className="flex items-center gap-2 px-4 py-4 border-b">
                {settings?.logoBase64
                  ? <img src={settings.logoBase64} alt="logo" className="h-8 w-8 object-contain rounded" />
                  : <div className="h-8 w-8 rounded bg-primary flex items-center justify-center text-primary-foreground text-sm font-bold">C</div>}
                <span className="font-semibold text-sm">CliniSys</span>
              </div>
              <SidebarContent onNav={() => setMobileOpen(false)} />
            </SheetContent>
          </Sheet>

          {/* Mobile logo */}
          <div className="flex items-center gap-2 lg:hidden">
            {settings?.logoBase64
              ? <img src={settings.logoBase64} alt="logo" className="h-7 w-7 object-contain rounded" />
              : <div className="h-7 w-7 rounded bg-primary flex items-center justify-center text-primary-foreground text-xs font-bold">C</div>}
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
```

- [ ] **Step 4: Create useClinicSettings hook used by AppLayout**

`frontend/src/features/settings/useClinicSettings.ts`:
```ts
import { useEffect, useState } from "react";
import { getClinicSettings } from "@/api/clinicSettings";
import type { ClinicSettingsModel } from "@/api/types";

export function useClinicSettings() {
  const [settings, setSettings] = useState<ClinicSettingsModel | null>(null);

  useEffect(() => {
    getClinicSettings().then(setSettings).catch(() => {});
  }, []);

  return { settings, setSettings };
}
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/ frontend/src/features/settings/useClinicSettings.ts
git commit -m "feat: add AppLayout with responsive sidebar, ThemeToggle, LanguageSwitcher"
```

---

### Task 7: App Router + Login Page

**Files:**
- Create: `frontend/src/App.tsx`
- Create: `frontend/src/features/auth/LoginPage.tsx`
- Create: `frontend/src/features/auth/login.schema.ts`
- Create: `frontend/src/features/dashboard/DashboardPage.tsx`

**Interfaces:**
- Produces: full route tree; working login form that stores JWT and decodes theme/language

- [ ] **Step 1: Create App.tsx with full route tree**

`frontend/src/App.tsx`:
```tsx
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
```

- [ ] **Step 2: Create login schema**

`frontend/src/features/auth/login.schema.ts`:
```ts
import * as yup from "yup";

export const loginSchema = yup.object({
  email: yup.string().email("Invalid email").required("Email is required"),
  password: yup.string().min(1, "Password is required").required(),
});

export type LoginFormData = yup.InferType<typeof loginSchema>;
```

- [ ] **Step 3: Create LoginPage**

`frontend/src/features/auth/LoginPage.tsx`:
```tsx
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useAuth } from "@/auth/AuthContext";
import { getClinicSettings } from "@/api/clinicSettings";
import { loginSchema, type LoginFormData } from "./login.schema";

export function LoginPage() {
  const { t } = useTranslation();
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [logoBase64, setLogoBase64] = useState<string | null>(null);

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<LoginFormData>({
    resolver: yupResolver(loginSchema),
  });

  useEffect(() => {
    if (isAuthenticated) { navigate("/"); return; }
    getClinicSettings().then((s) => setLogoBase64(s.logoBase64 ?? null)).catch(() => {});
  }, [isAuthenticated, navigate]);

  const onSubmit = async (data: LoginFormData) => {
    try {
      await login(data.email, data.password);
      navigate("/");
    } catch {
      toast.error(t("auth.loginError"));
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="flex flex-col items-center gap-3 pb-2">
          {logoBase64
            ? <img src={logoBase64} alt="Clinic logo" className="h-16 w-16 object-contain" />
            : <div className="h-16 w-16 rounded-full bg-primary flex items-center justify-center text-primary-foreground text-2xl font-bold">C</div>}
          <CardTitle className="text-xl">CliniSys</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="email">{t("auth.email")}</Label>
              <Input id="email" type="email" autoComplete="email" {...register("email")} />
              {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="password">{t("auth.password")}</Label>
              <Input id="password" type="password" autoComplete="current-password" {...register("password")} />
              {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
            </div>
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? t("common.loading") : t("auth.login")}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
```

- [ ] **Step 4: Create minimal DashboardPage**

`frontend/src/features/dashboard/DashboardPage.tsx`:
```tsx
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { getAppointments } from "@/api/appointments";
import { useAuth } from "@/auth/AuthContext";
import type { AppointmentModel } from "@/api/types";

export function DashboardPage() {
  const { t } = useTranslation();
  const { role, doctorId } = useAuth();
  const [appointments, setAppointments] = useState<AppointmentModel[]>([]);

  useEffect(() => {
    const today = new Date().toISOString().split("T")[0];
    getAppointments({
      date: today,
      doctorId: role === "Doctor" ? doctorId : undefined,
      pageSize: 20,
    }).then((r) => setAppointments(r.items)).catch(() => {});
  }, [role, doctorId]);

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold">{t("nav.dashboard")}</h1>
      <p className="text-muted-foreground">
        {t("appointments.title")} {t("common.of")} {new Intl.DateTimeFormat(undefined, { dateStyle: "full" }).format(new Date())}
      </p>
      <div className="grid gap-2">
        {appointments.length === 0 && (
          <p className="text-sm text-muted-foreground">{t("common.noResults")}</p>
        )}
        {appointments.map((a) => (
          <div key={a.id} className="flex items-center justify-between rounded-md border p-3 text-sm">
            <div>
              <p className="font-medium">{a.patientName}</p>
              <p className="text-muted-foreground">{a.doctorName}</p>
            </div>
            <div className="text-right">
              <p>{new Intl.DateTimeFormat(undefined, { timeStyle: "short" }).format(new Date(a.startsAt))}</p>
              <p className="text-muted-foreground">{a.durationMinutes} min</p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

- [ ] **Step 5: Verify dev server starts**

```bash
cd frontend && npm run dev
```

Open `http://localhost:5173/login` — login form visible with logo fallback "C". No console errors.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/App.tsx frontend/src/features/auth/ frontend/src/features/dashboard/
git commit -m "feat: add App router, LoginPage with clinic logo, DashboardPage"
```

---

### Task 8: Patients Feature

**Files:**
- Create: `frontend/src/features/patients/PatientsPage.tsx`
- Create: `frontend/src/features/patients/PatientForm.tsx`
- Create: `frontend/src/features/patients/patient.schema.ts`

**Interfaces:**
- Consumes: `getPatients`, `getPatientById`, `createPatient`, `updatePatient`, `deactivatePatient`
- Produces: `/patients` list with search + pagination; `/patients/new` and `/patients/:id` forms

- [ ] **Step 1: Create patient schema**

`frontend/src/features/patients/patient.schema.ts`:
```ts
import * as yup from "yup";

export const patientSchema = yup.object({
  fullName: yup.string().required("Full name is required").max(200),
  dateOfBirth: yup.string().required("Date of birth is required"),
  phone: yup.string().required("Phone is required").max(30),
  email: yup.string().email("Invalid email").optional(),
  notes: yup.string().optional(),
});

export type PatientFormData = yup.InferType<typeof patientSchema>;
```

- [ ] **Step 2: Create PatientsPage**

`frontend/src/features/patients/PatientsPage.tsx`:
```tsx
import { useEffect, useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Plus, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { getPatients, deactivatePatient } from "@/api/patients";
import type { PatientModel, PagedResult } from "@/api/types";

export function PatientsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [data, setData] = useState<PagedResult<PatientModel> | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);

  const load = useCallback(() => {
    getPatients({ search: search || undefined, page, pageSize: 20 })
      .then(setData)
      .catch(() => toast.error("Failed to load patients."));
  }, [search, page]);

  useEffect(() => { load(); }, [load]);

  const handleDeactivate = async (id: string) => {
    try {
      await deactivatePatient(id);
      toast.success("Patient deactivated.");
      load();
    } catch {
      toast.error("Failed to deactivate patient.");
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2">
        <h1 className="text-2xl font-semibold">{t("patients.title")}</h1>
        <Button onClick={() => navigate("/patients/new")} size="sm">
          <Plus className="mr-1 h-4 w-4" />{t("patients.new")}
        </Button>
      </div>

      <div className="relative max-w-sm">
        <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
        <Input className="pl-8" placeholder={t("common.search")}
          value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
      </div>

      {/* Desktop table */}
      <div className="hidden md:block rounded-md border overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("patients.fullName")}</TableHead>
              <TableHead>{t("patients.phone")}</TableHead>
              <TableHead>{t("patients.email")}</TableHead>
              <TableHead>{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data?.items.map((p) => (
              <TableRow key={p.id}>
                <TableCell className="font-medium">{p.fullName}</TableCell>
                <TableCell>{p.phone}</TableCell>
                <TableCell>{p.email ?? "—"}</TableCell>
                <TableCell>
                  <div className="flex gap-2">
                    <Button size="sm" variant="outline" onClick={() => navigate(`/patients/${p.id}`)}>
                      {t("common.edit")}
                    </Button>
                    <Button size="sm" variant="destructive" onClick={() => handleDeactivate(p.id)}>
                      {t("patients.deactivate")}
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
            {data?.items.length === 0 && (
              <TableRow><TableCell colSpan={4} className="text-center text-muted-foreground py-8">{t("common.noResults")}</TableCell></TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {/* Mobile cards */}
      <div className="flex flex-col gap-2 md:hidden">
        {data?.items.map((p) => (
          <div key={p.id} className="rounded-md border p-3 space-y-1">
            <p className="font-medium">{p.fullName}</p>
            <p className="text-sm text-muted-foreground">{p.phone}</p>
            {p.email && <p className="text-sm text-muted-foreground">{p.email}</p>}
            <div className="flex gap-2 pt-1">
              <Button size="sm" variant="outline" className="flex-1" onClick={() => navigate(`/patients/${p.id}`)}>
                {t("common.edit")}
              </Button>
              <Button size="sm" variant="destructive" className="flex-1" onClick={() => handleDeactivate(p.id)}>
                {t("patients.deactivate")}
              </Button>
            </div>
          </div>
        ))}
        {data?.items.length === 0 && <p className="text-center text-muted-foreground py-8">{t("common.noResults")}</p>}
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button variant="outline" size="sm" disabled={page === 1} onClick={() => setPage(page - 1)}>
            {t("common.previous")}
          </Button>
          <span className="text-sm">{t("common.page")} {page} {t("common.of")} {data.totalPages}</span>
          <Button variant="outline" size="sm" disabled={page === data.totalPages} onClick={() => setPage(page + 1)}>
            {t("common.next")}
          </Button>
        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 3: Create PatientForm**

`frontend/src/features/patients/PatientForm.tsx`:
```tsx
import { useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { getPatientById, createPatient, updatePatient } from "@/api/patients";
import { patientSchema, type PatientFormData } from "./patient.schema";

export function PatientForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id;

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<PatientFormData>({
    resolver: yupResolver(patientSchema),
  });

  useEffect(() => {
    if (id) {
      getPatientById(id).then((p) => reset({
        fullName: p.fullName, dateOfBirth: p.dateOfBirth,
        phone: p.phone, email: p.email ?? "", notes: p.notes ?? "",
      })).catch(() => toast.error("Failed to load patient."));
    }
  }, [id, reset]);

  const onSubmit = async (data: PatientFormData) => {
    try {
      if (isEdit) {
        await updatePatient(id!, data);
        toast.success("Patient updated.");
      } else {
        await createPatient(data);
        toast.success("Patient created.");
      }
      navigate("/patients");
    } catch {
      toast.error("Failed to save patient.");
    }
  };

  return (
    <div className="max-w-2xl space-y-4">
      <h1 className="text-2xl font-semibold">
        {isEdit ? t("common.edit") : t("patients.new")} {t("patients.title").toLowerCase()}
      </h1>

      <form onSubmit={handleSubmit(onSubmit)} className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label>{t("patients.fullName")}</Label>
          <Input {...register("fullName")} />
          {errors.fullName && <p className="text-xs text-destructive">{errors.fullName.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("patients.dateOfBirth")}</Label>
          <Input type="date" {...register("dateOfBirth")} />
          {errors.dateOfBirth && <p className="text-xs text-destructive">{errors.dateOfBirth.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>{t("patients.phone")}</Label>
          <Input {...register("phone")} />
          {errors.phone && <p className="text-xs text-destructive">{errors.phone.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label>{t("patients.email")}</Label>
          <Input type="email" {...register("email")} />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label>{t("patients.notes")}</Label>
          <Textarea {...register("notes")} rows={3} />
        </div>

        <div className="flex gap-2 sm:col-span-2">
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? t("common.loading") : t("common.save")}
          </Button>
          <Button type="button" variant="outline" onClick={() => navigate("/patients")}>
            {t("common.cancel")}
          </Button>
        </div>
      </form>
    </div>
  );
}
```

- [ ] **Step 4: Add Textarea to Shadcn components**

```bash
cd frontend && npx shadcn@latest add textarea
```

- [ ] **Step 5: Verify in browser**

```bash
cd frontend && npm run dev
```

Log in → navigate to `/patients`. Verify: list renders, search filters by name, "New Patient" opens form, form saves and navigates back.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/patients/
git commit -m "feat: add Patients list page and create/edit form"
```

---

### Task 9: Doctors + Users Features

**Files:**
- Create: `frontend/src/features/doctors/DoctorsPage.tsx`
- Create: `frontend/src/features/doctors/DoctorForm.tsx`
- Create: `frontend/src/features/users/UsersPage.tsx`
- Create: `frontend/src/features/users/UserForm.tsx`
- Create: `frontend/src/features/users/user.schema.ts`

**Interfaces:**
- Produces: `/doctors` list + edit specialty; `/users` admin panel with create, deactivate, reset-password

- [ ] **Step 1: Create DoctorsPage**

`frontend/src/features/doctors/DoctorsPage.tsx`:
```tsx
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { getDoctors } from "@/api/doctors";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { DoctorModel, PagedResult } from "@/api/types";

export function DoctorsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [data, setData] = useState<PagedResult<DoctorModel> | null>(null);
  const [page, setPage] = useState(1);

  useEffect(() => {
    getDoctors({ page, pageSize: 20 }).then(setData).catch(() => {});
  }, [page]);

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold">{t("doctors.title")}</h1>

      {/* Desktop table */}
      <div className="hidden md:block rounded-md border overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("patients.fullName")}</TableHead>
              <TableHead>{t("patients.email")}</TableHead>
              <TableHead>{t("doctors.specialty")}</TableHead>
              <TableHead>{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data?.items.map((d) => (
              <TableRow key={d.id}>
                <TableCell className="font-medium">{d.fullName}</TableCell>
                <TableCell>{d.email ?? "—"}</TableCell>
                <TableCell>{d.specialty}</TableCell>
                <TableCell>
                  <Button size="sm" variant="outline" onClick={() => navigate(`/doctors/${d.id}`)}>
                    {t("doctors.editSpecialty")}
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {/* Mobile cards */}
      <div className="flex flex-col gap-2 md:hidden">
        {data?.items.map((d) => (
          <div key={d.id} className="rounded-md border p-3 space-y-1">
            <p className="font-medium">{d.fullName}</p>
            <p className="text-sm text-muted-foreground">{d.specialty}</p>
            <Button size="sm" variant="outline" className="w-full mt-1" onClick={() => navigate(`/doctors/${d.id}`)}>
              {t("doctors.editSpecialty")}
            </Button>
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
    </div>
  );
}
```

- [ ] **Step 2: Create DoctorForm**

`frontend/src/features/doctors/DoctorForm.tsx`:
```tsx
import { useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getDoctorById, updateDoctor } from "@/api/doctors";

export function DoctorForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm<{ specialty: string }>();

  useEffect(() => {
    if (id) getDoctorById(id).then((d) => reset({ specialty: d.specialty })).catch(() => {});
  }, [id, reset]);

  const onSubmit = async (data: { specialty: string }) => {
    try { await updateDoctor(id!, data.specialty); toast.success("Specialty updated."); navigate("/doctors"); }
    catch { toast.error("Failed to update specialty."); }
  };

  return (
    <div className="max-w-sm space-y-4">
      <h1 className="text-2xl font-semibold">{t("doctors.editSpecialty")}</h1>
      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label>{t("doctors.specialty")}</Label>
          <Input {...register("specialty", { required: true })} />
        </div>
        <div className="flex gap-2">
          <Button type="submit" disabled={isSubmitting}>{t("common.save")}</Button>
          <Button type="button" variant="outline" onClick={() => navigate("/doctors")}>{t("common.cancel")}</Button>
        </div>
      </form>
    </div>
  );
}
```

- [ ] **Step 3: Create user schema**

`frontend/src/features/users/user.schema.ts`:
```ts
import * as yup from "yup";

export const createUserSchema = yup.object({
  email: yup.string().email().required("Email is required"),
  fullName: yup.string().required("Full name is required"),
  password: yup.string().min(8, "At least 8 characters").required(),
  role: yup.string().oneOf(["Admin","Staff","Doctor"]).required("Role is required"),
  specialty: yup.string().when("role", {
    is: "Doctor",
    then: (s) => s.required("Specialty is required for Doctors"),
    otherwise: (s) => s.optional(),
  }),
});

export const resetPasswordSchema = yup.object({
  newPassword: yup.string().min(8, "At least 8 characters").required(),
});

export type CreateUserFormData = yup.InferType<typeof createUserSchema>;
export type ResetPasswordFormData = yup.InferType<typeof resetPasswordSchema>;
```

- [ ] **Step 4: Create UsersPage**

`frontend/src/features/users/UsersPage.tsx`:
```tsx
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { getUsers, createUser, deactivateUser, resetPassword } from "@/api/users";
import { UserForm } from "./UserForm";
import type { UserModel, PagedResult } from "@/api/types";

export function UsersPage() {
  const { t } = useTranslation();
  const [data, setData] = useState<PagedResult<UserModel> | null>(null);
  const [open, setOpen] = useState(false);
  const [resetTarget, setResetTarget] = useState<UserModel | null>(null);
  const [newPw, setNewPw] = useState("");
  const [page, setPage] = useState(1);

  const load = () => getUsers({ page, pageSize: 20 }).then(setData).catch(() => {});
  useEffect(() => { load(); }, [page]);

  const handleCreate = async (data: Parameters<typeof createUser>[0]) => {
    try { await createUser(data); toast.success("User created."); setOpen(false); load(); }
    catch { toast.error("Failed to create user."); }
  };

  const handleDeactivate = async (id: string) => {
    try { await deactivateUser(id); toast.success("User deactivated."); load(); }
    catch { toast.error("Failed to deactivate user."); }
  };

  const handleResetPw = async () => {
    if (!resetTarget || !newPw) return;
    try { await resetPassword(resetTarget.id, newPw); toast.success("Password reset."); setResetTarget(null); setNewPw(""); }
    catch { toast.error("Failed to reset password."); }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">{t("users.title")}</h1>
        <Dialog open={open} onOpenChange={setOpen}>
          <DialogTrigger asChild>
            <Button size="sm"><Plus className="mr-1 h-4 w-4" />{t("users.new")}</Button>
          </DialogTrigger>
          <DialogContent className="max-w-md">
            <DialogHeader><DialogTitle>{t("users.new")}</DialogTitle></DialogHeader>
            <UserForm onSubmit={handleCreate} onCancel={() => setOpen(false)} />
          </DialogContent>
        </Dialog>
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

      {/* Mobile cards */}
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

      {/* Reset password dialog */}
      <Dialog open={!!resetTarget} onOpenChange={(o) => { if (!o) { setResetTarget(null); setNewPw(""); }}}>
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

      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button variant="outline" size="sm" disabled={page === 1} onClick={() => setPage(p => p - 1)}>{t("common.previous")}</Button>
          <span className="text-sm">{t("common.page")} {page} {t("common.of")} {data.totalPages}</span>
          <Button variant="outline" size="sm" disabled={page === data.totalPages} onClick={() => setPage(p => p + 1)}>{t("common.next")}</Button>
        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 5: Create UserForm**

`frontend/src/features/users/UserForm.tsx`:
```tsx
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createUserSchema, type CreateUserFormData } from "./user.schema";

interface Props {
  onSubmit: (data: CreateUserFormData) => Promise<void>;
  onCancel: () => void;
}

export function UserForm({ onSubmit, onCancel }: Props) {
  const { t } = useTranslation();
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<CreateUserFormData>({
    resolver: yupResolver(createUserSchema),
  });
  const role = watch("role");

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
      <div className="flex flex-col gap-1.5">
        <Label>{t("users.fullName")}</Label>
        <Input {...register("fullName")} />
        {errors.fullName && <p className="text-xs text-destructive">{errors.fullName.message}</p>}
      </div>
      <div className="flex flex-col gap-1.5">
        <Label>{t("users.email")}</Label>
        <Input type="email" {...register("email")} />
        {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
      </div>
      <div className="flex flex-col gap-1.5">
        <Label>{t("users.password")}</Label>
        <Input type="password" {...register("password")} />
        {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
      </div>
      <div className="flex flex-col gap-1.5">
        <Label>{t("users.role")}</Label>
        <select className="border rounded px-3 py-2 text-sm bg-background" {...register("role")}>
          <option value="Admin">{t("users.role_Admin")}</option>
          <option value="Staff">{t("users.role_Staff")}</option>
          <option value="Doctor">{t("users.role_Doctor")}</option>
        </select>
        {errors.role && <p className="text-xs text-destructive">{errors.role.message}</p>}
      </div>
      {role === "Doctor" && (
        <div className="flex flex-col gap-1.5">
          <Label>{t("users.specialty")}</Label>
          <Input {...register("specialty")} />
          {errors.specialty && <p className="text-xs text-destructive">{errors.specialty.message}</p>}
        </div>
      )}
      <div className="flex gap-2 pt-1">
        <Button type="submit" disabled={isSubmitting}>{t("common.create")}</Button>
        <Button type="button" variant="outline" onClick={onCancel}>{t("common.cancel")}</Button>
      </div>
    </form>
  );
}
```

- [ ] **Step 6: Verify and commit**

```bash
cd frontend && npm run dev
```

Log in as Admin → navigate to `/doctors` and `/users`. Verify: list renders, specialty edit works, user create dialog opens with conditional Specialty field for Doctor role.

```bash
git add frontend/src/features/doctors/ frontend/src/features/users/
git commit -m "feat: add Doctors and Users feature pages"
```

---

### Task 10: Appointments Feature (List + Calendar)

**Files:**
- Create: `frontend/src/features/appointments/AppointmentsPage.tsx`
- Create: `frontend/src/features/appointments/AppointmentModal.tsx`
- Create: `frontend/src/features/appointments/appointment.schema.ts`

**Interfaces:**
- Consumes: `getAppointments`, `createAppointment`, `rescheduleAppointment`, `updateAppointmentStatus`, `getPatients`, `getDoctors`, `getClinicSettings`
- Produces: `/appointments` with List tab and Calendar tab; modal for create/reschedule/status update

- [ ] **Step 1: Install FullCalendar packages**

```bash
cd frontend && npm install @fullcalendar/react @fullcalendar/daygrid @fullcalendar/timegrid @fullcalendar/interaction
```

- [ ] **Step 2: Create appointment schema**

`frontend/src/features/appointments/appointment.schema.ts`:
```ts
import * as yup from "yup";

export const appointmentSchema = yup.object({
  patientId: yup.string().uuid().required("Patient is required"),
  doctorId: yup.string().uuid().required("Doctor is required"),
  startsAt: yup.string().required("Start date/time is required"),
  durationMinutes: yup.number().min(5).max(480).required("Duration is required"),
  notes: yup.string().optional(),
});

export const statusSchema = yup.object({
  status: yup.string().oneOf(["Scheduled","Confirmed","Completed","Cancelled","NoShow"]).required(),
});

export type AppointmentFormData = yup.InferType<typeof appointmentSchema>;
export type StatusFormData = yup.InferType<typeof statusSchema>;
```

- [ ] **Step 3: Create AppointmentModal**

`frontend/src/features/appointments/AppointmentModal.tsx`:
```tsx
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { getPatients } from "@/api/patients";
import { getDoctors } from "@/api/doctors";
import { createAppointment, rescheduleAppointment, updateAppointmentStatus } from "@/api/appointments";
import { appointmentSchema, statusSchema, type AppointmentFormData, type StatusFormData } from "./appointment.schema";
import type { AppointmentModel, PatientModel, DoctorModel } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";

interface Props {
  open: boolean;
  onClose: () => void;
  onSaved: () => void;
  appointment?: AppointmentModel;
  defaultStartsAt?: string;
}

export function AppointmentModal({ open, onClose, onSaved, appointment, defaultStartsAt }: Props) {
  const { t } = useTranslation();
  const { role, doctorId } = useAuth();
  const isEdit = !!appointment;
  const [patients, setPatients] = useState<PatientModel[]>([]);
  const [doctors, setDoctors] = useState<DoctorModel[]>([]);

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<AppointmentFormData>({
    resolver: yupResolver(appointmentSchema),
  });

  const { register: registerStatus, handleSubmit: handleStatusSubmit, formState: { isSubmitting: isStatusSubmitting } } = useForm<StatusFormData>({
    resolver: yupResolver(statusSchema),
    defaultValues: { status: appointment?.status },
  });

  useEffect(() => {
    getPatients({ pageSize: 100 }).then((r) => setPatients(r.items)).catch(() => {});
    getDoctors({ pageSize: 100 }).then((r) => setDoctors(r.items)).catch(() => {});
  }, []);

  useEffect(() => {
    if (appointment) {
      reset({
        patientId: appointment.patientId,
        doctorId: appointment.doctorId,
        startsAt: appointment.startsAt.slice(0, 16),
        durationMinutes: appointment.durationMinutes,
        notes: appointment.notes ?? "",
      });
    } else {
      reset({ startsAt: defaultStartsAt?.slice(0, 16) ?? "", durationMinutes: 30 });
    }
  }, [appointment, defaultStartsAt, reset]);

  const onSubmit = async (data: AppointmentFormData) => {
    try {
      if (isEdit) {
        await rescheduleAppointment(appointment!.id, { startsAt: data.startsAt, durationMinutes: data.durationMinutes, notes: data.notes });
        toast.success("Appointment rescheduled.");
      } else {
        await createAppointment(data);
        toast.success("Appointment created.");
      }
      onSaved();
      onClose();
    } catch {
      toast.error("Failed to save appointment.");
    }
  };

  const onStatusSubmit = async (data: StatusFormData) => {
    try {
      await updateAppointmentStatus(appointment!.id, data.status);
      toast.success("Status updated.");
      onSaved();
      onClose();
    } catch {
      toast.error("Failed to update status.");
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose(); }}>
      <DialogContent className="max-w-md max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{isEdit ? t("appointments.reschedule") : t("appointments.new")}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <Label>{t("appointments.patient")}</Label>
            <select className="border rounded px-3 py-2 text-sm bg-background" {...register("patientId")} disabled={isEdit}>
              <option value="">{t("common.select")}</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
            {errors.patientId && <p className="text-xs text-destructive">{errors.patientId.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label>{t("appointments.doctor")}</Label>
            <select className="border rounded px-3 py-2 text-sm bg-background" {...register("doctorId")} disabled={isEdit || role === "Doctor"}>
              <option value="">{t("common.select")}</option>
              {(role === "Doctor" ? doctors.filter((d) => d.id === doctorId) : doctors).map((d) => (
                <option key={d.id} value={d.id}>{d.fullName}</option>
              ))}
            </select>
            {errors.doctorId && <p className="text-xs text-destructive">{errors.doctorId.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label>{t("appointments.startsAt")}</Label>
            <Input type="datetime-local" {...register("startsAt")} />
            {errors.startsAt && <p className="text-xs text-destructive">{errors.startsAt.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label>{t("appointments.durationMinutes")}</Label>
            <Input type="number" min={5} max={480} step={5} {...register("durationMinutes")} />
            {errors.durationMinutes && <p className="text-xs text-destructive">{errors.durationMinutes.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label>{t("appointments.notes")}</Label>
            <Textarea rows={2} {...register("notes")} />
          </div>

          <div className="flex gap-2">
            <Button type="submit" disabled={isSubmitting}>{t("common.save")}</Button>
            <Button type="button" variant="outline" onClick={onClose}>{t("common.cancel")}</Button>
          </div>
        </form>

        {isEdit && (
          <>
            <hr className="my-2" />
            <form onSubmit={handleStatusSubmit(onStatusSubmit)} className="flex gap-2 items-end">
              <div className="flex flex-col gap-1.5 flex-1">
                <Label>{t("appointments.status")}</Label>
                <select className="border rounded px-3 py-2 text-sm bg-background" {...registerStatus("status")}>
                  <option value="Scheduled">Scheduled</option>
                  <option value="Confirmed">Confirmed</option>
                  <option value="Completed">Completed</option>
                  <option value="Cancelled">Cancelled</option>
                  <option value="NoShow">NoShow</option>
                </select>
              </div>
              <Button type="submit" variant="outline" disabled={isStatusSubmitting}>{t("appointments.updateStatus")}</Button>
            </form>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
```

- [ ] **Step 4: Create AppointmentsPage**

`frontend/src/features/appointments/AppointmentsPage.tsx`:
```tsx
import { useEffect, useState, useCallback } from "react";
import { useTranslation } from "react-i18next";
import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin, { type DateClickArg } from "@fullcalendar/interaction";
import { Button } from "@/components/ui/button";
import { Plus, List, CalendarDays } from "lucide-react";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { getAppointments } from "@/api/appointments";
import { getClinicSettings } from "@/api/clinicSettings";
import { AppointmentModal } from "./AppointmentModal";
import type { AppointmentModel, ClinicSettingsModel } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";

type Tab = "list" | "calendar";

const STATUS_COLORS: Record<string, string> = {
  Scheduled: "#3b82f6",
  Confirmed: "#10b981",
  Completed: "#6b7280",
  Cancelled: "#ef4444",
  NoShow: "#f59e0b",
};

export function AppointmentsPage() {
  const { t } = useTranslation();
  const { role, doctorId } = useAuth();
  const [tab, setTab] = useState<Tab>("calendar");
  const [data, setData] = useState<AppointmentModel[]>([]);
  const [listPage, setListPage] = useState(1);
  const [listTotal, setListTotal] = useState(0);
  const [settings, setSettings] = useState<ClinicSettingsModel | null>(null);
  const [selected, setSelected] = useState<AppointmentModel | null>(null);
  const [defaultStartsAt, setDefaultStartsAt] = useState<string | undefined>();
  const [modalOpen, setModalOpen] = useState(false);

  useEffect(() => {
    getClinicSettings().then(setSettings).catch(() => {});
  }, []);

  const loadList = useCallback(() => {
    getAppointments({
      doctorId: role === "Doctor" ? doctorId : undefined,
      page: listPage, pageSize: 20,
    }).then((r) => { setData(r.items); setListTotal(r.totalPages); }).catch(() => {});
  }, [role, doctorId, listPage]);

  const loadCalendar = useCallback((start: string, end: string) => {
    return getAppointments({
      doctorId: role === "Doctor" ? doctorId : undefined,
      startDate: start, endDate: end,
    }).then((r) => r.items).catch(() => [] as AppointmentModel[]);
  }, [role, doctorId]);

  useEffect(() => { if (tab === "list") loadList(); }, [tab, loadList]);

  const openNew = (startsAt?: string) => {
    setSelected(null);
    setDefaultStartsAt(startsAt);
    setModalOpen(true);
  };

  const openEdit = (a: AppointmentModel) => {
    setSelected(a);
    setDefaultStartsAt(undefined);
    setModalOpen(true);
  };

  const openDays = settings?.openDays
    ? settings.openDays.split(",").map(Number)
    : [1, 2, 3, 4, 5];

  const slotMinTime = settings ? `${settings.openTime}` : "08:00:00";
  const slotMaxTime = settings ? `${settings.closeTime}` : "18:00:00";

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-2">
        <h1 className="text-2xl font-semibold">{t("appointments.title")}</h1>
        <div className="flex gap-2 flex-wrap">
          <Button variant={tab === "list" ? "default" : "outline"} size="sm" onClick={() => setTab("list")}>
            <List className="mr-1 h-4 w-4" />{t("appointments.listView")}
          </Button>
          <Button variant={tab === "calendar" ? "default" : "outline"} size="sm" onClick={() => setTab("calendar")}>
            <CalendarDays className="mr-1 h-4 w-4" />{t("appointments.calendarView")}
          </Button>
          <Button size="sm" onClick={() => openNew()}><Plus className="mr-1 h-4 w-4" />{t("appointments.new")}</Button>
        </div>
      </div>

      {tab === "list" && (
        <>
          <div className="hidden md:block rounded-md border overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t("appointments.patient")}</TableHead>
                  <TableHead>{t("appointments.doctor")}</TableHead>
                  <TableHead>{t("appointments.startsAt")}</TableHead>
                  <TableHead>{t("appointments.status")}</TableHead>
                  <TableHead>{t("common.actions")}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.map((a) => (
                  <TableRow key={a.id}>
                    <TableCell>{a.patientName}</TableCell>
                    <TableCell>{a.doctorName}</TableCell>
                    <TableCell>{new Intl.DateTimeFormat(undefined, { dateStyle: "short", timeStyle: "short" }).format(new Date(a.startsAt))}</TableCell>
                    <TableCell>
                      <span className="px-2 py-0.5 rounded-full text-xs text-white" style={{ background: STATUS_COLORS[a.status] }}>
                        {a.status}
                      </span>
                    </TableCell>
                    <TableCell>
                      <Button size="sm" variant="outline" onClick={() => openEdit(a)}>{t("common.edit")}</Button>
                    </TableCell>
                  </TableRow>
                ))}
                {data.length === 0 && (
                  <TableRow><TableCell colSpan={5} className="text-center text-muted-foreground py-8">{t("common.noResults")}</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </div>

          <div className="flex flex-col gap-2 md:hidden">
            {data.map((a) => (
              <div key={a.id} className="rounded-md border p-3 space-y-1">
                <div className="flex justify-between">
                  <p className="font-medium">{a.patientName}</p>
                  <span className="px-2 py-0.5 rounded-full text-xs text-white" style={{ background: STATUS_COLORS[a.status] }}>{a.status}</span>
                </div>
                <p className="text-sm text-muted-foreground">{a.doctorName}</p>
                <p className="text-sm">{new Intl.DateTimeFormat(undefined, { dateStyle: "short", timeStyle: "short" }).format(new Date(a.startsAt))}</p>
                <Button size="sm" variant="outline" className="w-full" onClick={() => openEdit(a)}>{t("common.edit")}</Button>
              </div>
            ))}
            {data.length === 0 && <p className="text-center text-muted-foreground py-8">{t("common.noResults")}</p>}
          </div>

          {listTotal > 1 && (
            <div className="flex items-center justify-center gap-2">
              <Button variant="outline" size="sm" disabled={listPage === 1} onClick={() => setListPage(p => p - 1)}>{t("common.previous")}</Button>
              <span className="text-sm">{t("common.page")} {listPage} {t("common.of")} {listTotal}</span>
              <Button variant="outline" size="sm" disabled={listPage === listTotal} onClick={() => setListPage(p => p + 1)}>{t("common.next")}</Button>
            </div>
          )}
        </>
      )}

      {tab === "calendar" && (
        <div className="[&_.fc]:text-sm">
          <FullCalendar
            plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
            initialView="timeGridWeek"
            headerToolbar={{
              left: "prev,next today",
              center: "title",
              right: "dayGridMonth,timeGridWeek,timeGridDay",
            }}
            slotMinTime={slotMinTime}
            slotMaxTime={slotMaxTime}
            hiddenDays={[0, 1, 2, 3, 4, 5, 6].filter((d) => !openDays.includes(d))}
            selectable
            selectConstraint={{ daysOfWeek: openDays, startTime: slotMinTime, endTime: slotMaxTime }}
            dateClick={(info: DateClickArg) => openNew(info.dateStr)}
            eventClick={(info) => {
              const id = info.event.id;
              const appt = data.find((a) => a.id === id);
              if (appt) openEdit(appt);
            }}
            events={async (info, successCb) => {
              const items = await loadCalendar(info.startStr, info.endStr);
              setData(items);
              successCb(items.map((a) => ({
                id: a.id,
                title: `${a.patientName} (${a.doctorName})`,
                start: a.startsAt,
                end: new Date(new Date(a.startsAt).getTime() + a.durationMinutes * 60000).toISOString(),
                backgroundColor: STATUS_COLORS[a.status],
                borderColor: STATUS_COLORS[a.status],
              })));
            }}
            height="auto"
          />
        </div>
      )}

      <AppointmentModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        onSaved={() => { if (tab === "list") loadList(); }}
        appointment={selected ?? undefined}
        defaultStartsAt={defaultStartsAt}
      />
    </div>
  );
}
```

- [ ] **Step 5: Verify in browser**

```bash
cd frontend && npm run dev
```

Navigate to `/appointments`. Verify: Calendar shows week view with clinic hours; clicking empty slot opens modal with pre-filled time; clicking event opens edit modal with status dropdown. List tab shows table/cards with pagination.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/appointments/
git commit -m "feat: add Appointments page with FullCalendar and list view"
```

---

### Task 11: ClinicSettings Page

**Files:**
- Create: `frontend/src/features/settings/SettingsPage.tsx`
- Create: `frontend/src/features/settings/settings.schema.ts`

**Interfaces:**
- Consumes: `getClinicSettings`, `updateClinicSettings`
- Produces: `/settings` admin-only form with logo upload, open hours, open days checkboxes

- [ ] **Step 1: Create settings schema**

`frontend/src/features/settings/settings.schema.ts`:
```ts
import * as yup from "yup";

export const settingsSchema = yup.object({
  openTime: yup.string().required("Open time is required"),
  closeTime: yup.string().required("Close time is required"),
  openDays: yup.string().required(),
  logoBase64: yup.string().optional(),
});

export type SettingsFormData = yup.InferType<typeof settingsSchema>;
```

- [ ] **Step 2: Add Checkbox Shadcn component**

```bash
cd frontend && npx shadcn@latest add checkbox
```

- [ ] **Step 3: Create SettingsPage**

`frontend/src/features/settings/SettingsPage.tsx`:
```tsx
import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { getClinicSettings, updateClinicSettings } from "@/api/clinicSettings";
import { settingsSchema, type SettingsFormData } from "./settings.schema";

const DAYS = [
  { value: 1, label: "Monday" },
  { value: 2, label: "Tuesday" },
  { value: 3, label: "Wednesday" },
  { value: 4, label: "Thursday" },
  { value: 5, label: "Friday" },
  { value: 6, label: "Saturday" },
  { value: 0, label: "Sunday" },
];

const MAX_LOGO_BYTES = 512 * 1024;

export function SettingsPage() {
  const { t } = useTranslation();
  const [openDays, setOpenDays] = useState<number[]>([1, 2, 3, 4, 5]);
  const [logoPreview, setLogoPreview] = useState<string | null>(null);
  const [logoBase64, setLogoBase64] = useState<string | undefined>();
  const fileRef = useRef<HTMLInputElement>(null);

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<SettingsFormData>({
    resolver: yupResolver(settingsSchema),
    defaultValues: { openTime: "08:00", closeTime: "18:00", openDays: "1,2,3,4,5" },
  });

  useEffect(() => {
    getClinicSettings().then((s) => {
      reset({ openTime: s.openTime, closeTime: s.closeTime, openDays: s.openDays });
      setOpenDays(s.openDays.split(",").map(Number));
      setLogoPreview(s.logoBase64 ?? null);
      setLogoBase64(s.logoBase64 ?? undefined);
    }).catch(() => {});
  }, [reset]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > MAX_LOGO_BYTES) { toast.error("Logo must be under 512 KB."); return; }
    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result as string;
      setLogoPreview(result);
      setLogoBase64(result);
    };
    reader.readAsDataURL(file);
  };

  const toggleDay = (day: number) => {
    setOpenDays((prev) => prev.includes(day) ? prev.filter((d) => d !== day) : [...prev, day].sort());
  };

  const onSubmit = async (_data: SettingsFormData) => {
    try {
      await updateClinicSettings({
        openTime: _data.openTime,
        closeTime: _data.closeTime,
        openDays: openDays.join(","),
        logoBase64: logoBase64 ?? null,
      });
      toast.success("Settings saved.");
    } catch {
      toast.error("Failed to save settings.");
    }
  };

  return (
    <div className="max-w-lg space-y-6">
      <h1 className="text-2xl font-semibold">{t("settings.title")}</h1>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
        <div className="flex flex-col gap-2">
          <Label>{t("settings.logo")}</Label>
          <div className="flex items-center gap-4">
            {logoPreview
              ? <img src={logoPreview} alt="logo preview" className="h-16 w-16 object-contain rounded border" />
              : <div className="h-16 w-16 rounded border flex items-center justify-center text-muted-foreground text-xs">{t("settings.noLogo")}</div>}
            <div className="flex flex-col gap-1">
              <Button type="button" variant="outline" size="sm" onClick={() => fileRef.current?.click()}>
                {t("settings.uploadLogo")}
              </Button>
              {logoPreview && (
                <Button type="button" variant="ghost" size="sm" className="text-destructive"
                  onClick={() => { setLogoPreview(null); setLogoBase64(undefined); }}>
                  {t("settings.removeLogo")}
                </Button>
              )}
              <input ref={fileRef} type="file" accept="image/*" className="hidden" onChange={handleFileChange} />
            </div>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-1.5">
            <Label>{t("settings.openTime")}</Label>
            <Input type="time" {...register("openTime")} />
            {errors.openTime && <p className="text-xs text-destructive">{errors.openTime.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>{t("settings.closeTime")}</Label>
            <Input type="time" {...register("closeTime")} />
            {errors.closeTime && <p className="text-xs text-destructive">{errors.closeTime.message}</p>}
          </div>
        </div>

        <div className="flex flex-col gap-2">
          <Label>{t("settings.openDays")}</Label>
          <div className="flex flex-wrap gap-3">
            {DAYS.map((d) => (
              <div key={d.value} className="flex items-center gap-1.5">
                <Checkbox
                  id={`day-${d.value}`}
                  checked={openDays.includes(d.value)}
                  onCheckedChange={() => toggleDay(d.value)}
                />
                <label htmlFor={`day-${d.value}`} className="text-sm cursor-pointer">{d.label}</label>
              </div>
            ))}
          </div>
        </div>

        <Button type="submit" disabled={isSubmitting}>{t("common.save")}</Button>
      </form>
    </div>
  );
}
```

- [ ] **Step 4: Verify in browser**

Log in as Admin → navigate to `/settings`. Verify: current settings load, logo preview shows, open days checkboxes reflect saved state, save updates values.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/settings/
git commit -m "feat: add ClinicSettings page with logo upload and open hours"
```

---

### Task 12: Account Page

**Files:**
- Create: `frontend/src/features/account/AccountPage.tsx`
- Create: `frontend/src/features/account/account.schema.ts`

**Interfaces:**
- Consumes: `updateProfilePicture`, `updatePreferences`, `changePassword`
- Produces: `/account` page with profile picture upload, theme/language prefs, change password form

- [ ] **Step 1: Create account schema**

`frontend/src/features/account/account.schema.ts`:
```ts
import * as yup from "yup";

export const changePasswordSchema = yup.object({
  currentPassword: yup.string().required("Current password is required"),
  newPassword: yup.string().min(8, "At least 8 characters").required(),
  confirmPassword: yup.string()
    .oneOf([yup.ref("newPassword")], "Passwords do not match")
    .required(),
});

export type ChangePasswordFormData = yup.InferType<typeof changePasswordSchema>;
```

- [ ] **Step 2: Create AccountPage**

`frontend/src/features/account/AccountPage.tsx`:
```tsx
import { useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { useTheme } from "next-themes";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { updateProfilePicture, updatePreferences, changePassword } from "@/api/account";
import { changePasswordSchema, type ChangePasswordFormData } from "./account.schema";
import { useAuth } from "@/auth/AuthContext";
import type { ThemePreference } from "@/api/types";

const MAX_PIC_BYTES = 512 * 1024;

export function AccountPage() {
  const { t, i18n } = useTranslation();
  const { fullName } = useAuth();
  const { theme, setTheme } = useTheme();
  const [preview, setPreview] = useState<string | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  const initials = fullName.split(" ").slice(0, 2).map((n) => n[0]).join("").toUpperCase();

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<ChangePasswordFormData>({
    resolver: yupResolver(changePasswordSchema),
  });

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > MAX_PIC_BYTES) { toast.error("Image must be under 512 KB."); return; }
    const reader = new FileReader();
    reader.onload = async () => {
      const result = reader.result as string;
      setPreview(result);
      try { await updateProfilePicture(result); toast.success("Profile picture updated."); }
      catch { toast.error("Failed to update picture."); }
    };
    reader.readAsDataURL(file);
  };

  const handleThemeChange = async (value: string) => {
    setTheme(value);
    const pref: ThemePreference = value === "light" ? "Light" : value === "dark" ? "Dark" : "System";
    const lang = localStorage.getItem("clinisys_language") ?? "en-US";
    try { await updatePreferences(pref, lang); }
    catch { toast.error("Failed to save theme preference."); }
  };

  const handleLanguageChange = async (lang: string) => {
    await i18n.changeLanguage(lang);
    localStorage.setItem("clinisys_language", lang);
    const pref: ThemePreference = theme === "light" ? "Light" : theme === "dark" ? "Dark" : "System";
    try { await updatePreferences(pref, lang); }
    catch { toast.error("Failed to save language preference."); }
  };

  const onPasswordSubmit = async (data: ChangePasswordFormData) => {
    try {
      await changePassword(data.currentPassword, data.newPassword);
      toast.success("Password changed.");
      reset();
    } catch {
      toast.error("Failed to change password.");
    }
  };

  return (
    <div className="max-w-lg space-y-8">
      <h1 className="text-2xl font-semibold">{t("nav.account")}</h1>

      <section className="space-y-3">
        <h2 className="text-lg font-medium">{t("account.profilePicture")}</h2>
        <div className="flex items-center gap-4">
          <Avatar className="h-16 w-16">
            {preview && <AvatarImage src={preview} />}
            <AvatarFallback className="text-xl">{initials}</AvatarFallback>
          </Avatar>
          <div>
            <Button type="button" variant="outline" size="sm" onClick={() => fileRef.current?.click()}>
              {t("account.uploadPicture")}
            </Button>
            <p className="text-xs text-muted-foreground mt-1">{t("account.maxSize")}</p>
            <input ref={fileRef} type="file" accept="image/*" className="hidden" onChange={handleFileChange} />
          </div>
        </div>
      </section>

      <section className="space-y-3">
        <h2 className="text-lg font-medium">{t("account.preferences")}</h2>
        <div className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <Label>{t("account.theme")}</Label>
            <select className="border rounded px-3 py-2 text-sm bg-background max-w-xs"
              value={theme ?? "system"} onChange={(e) => handleThemeChange(e.target.value)}>
              <option value="light">{t("theme.light")}</option>
              <option value="dark">{t("theme.dark")}</option>
              <option value="system">{t("theme.system")}</option>
            </select>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>{t("account.language")}</Label>
            <select className="border rounded px-3 py-2 text-sm bg-background max-w-xs"
              value={i18n.language} onChange={(e) => handleLanguageChange(e.target.value)}>
              <option value="en-US">{t("language.en-US")}</option>
              <option value="pt-BR">{t("language.pt-BR")}</option>
              <option value="es-ES">{t("language.es-ES")}</option>
            </select>
          </div>
        </div>
      </section>

      <section className="space-y-3">
        <h2 className="text-lg font-medium">{t("account.changePassword")}</h2>
        <form onSubmit={handleSubmit(onPasswordSubmit)} className="flex flex-col gap-3 max-w-sm">
          <div className="flex flex-col gap-1.5">
            <Label>{t("account.currentPassword")}</Label>
            <Input type="password" {...register("currentPassword")} />
            {errors.currentPassword && <p className="text-xs text-destructive">{errors.currentPassword.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>{t("account.newPassword")}</Label>
            <Input type="password" {...register("newPassword")} />
            {errors.newPassword && <p className="text-xs text-destructive">{errors.newPassword.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label>{t("account.confirmPassword")}</Label>
            <Input type="password" {...register("confirmPassword")} />
            {errors.confirmPassword && <p className="text-xs text-destructive">{errors.confirmPassword.message}</p>}
          </div>
          <Button type="submit" disabled={isSubmitting} className="self-start">
            {t("account.changePassword")}
          </Button>
        </form>
      </section>
    </div>
  );
}
```

- [ ] **Step 3: Verify in browser**

Navigate to `/account`. Verify: avatar shows initials fallback, file picker allows image upload with preview, theme/language selects work and persist after page refresh, change password form validates confirm match before submitting.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/features/account/
git commit -m "feat: add Account page with profile picture, preferences, and change password"
```

---

### Task 13: Final Shadcn Component Pass + Build Verification

**Objective:** Confirm all Shadcn/UI components are installed and the production build is clean.

- [ ] **Step 1: Add any missing Shadcn components**

```bash
cd frontend && npx shadcn@latest add avatar badge card dialog dropdown-menu input label separator sheet skeleton table textarea
```

Components already installed are skipped automatically.

- [ ] **Step 2: Run production build**

```bash
cd frontend && npm run build 2>&1
```

Expected: zero TypeScript errors, zero "Cannot find module" errors, build output in `frontend/dist/`.

- [ ] **Step 3: Fix any type errors found in Step 2**

If `npm run build` reports TypeScript errors, fix them now before proceeding. Common issues:
- Missing `type` import — add `import type { X }` where X is a type-only import
- Unused variable — remove it or prefix with `_`
- Implicit `any` — add an explicit type annotation

- [ ] **Step 4: Final smoke test**

```bash
cd frontend && npm run dev
```

Log in as `admin@clinisys.local` / `Admin@12345`. Walk through every route:
1. `/` — Dashboard shows today's appointments (empty list is fine)
2. `/patients` — Table visible; "New Patient" button works; form saves
3. `/doctors` — Table visible; "Edit Specialty" opens form
4. `/appointments` — Calendar renders with clinic hours; "New" button opens modal
5. `/users` — Table visible; "New User" dialog opens; Doctor role shows Specialty field
6. `/settings` — Open hours and logo fields load existing values
7. `/account` — Avatar initials shown; change password form validates

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: frontend complete — all pages, components, and build verified"
```
