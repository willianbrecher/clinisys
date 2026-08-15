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

document.documentElement.lang = i18n.language;
i18n.on("languageChanged", (lng) => {
  document.documentElement.lang = lng;
});

export default i18n;
