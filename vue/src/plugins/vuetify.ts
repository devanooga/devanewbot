import { createVuetify } from "vuetify";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";
import { aliases, mdi } from "vuetify/iconsets/mdi";

export default createVuetify({
    components,
    directives,
    icons: { defaultSet: "mdi", aliases, sets: { mdi } },
    defaults: {
        VCard: { rounded: "lg" },
        VTextField: { variant: "outlined", density: "comfortable", hideDetails: "auto" },
        VSelect: { variant: "outlined", density: "comfortable", hideDetails: "auto" },
        VDataTable: { density: "comfortable" },
        VBtn: { variant: "flat" },
    },
    theme: {
        defaultTheme: "devanoogaDark",
        themes: {
            devanoogaDark: {
                dark: true,
                colors: {
                    background: "#0f1115",
                    surface: "#181b22",
                    primary: "#4f8cff",
                    secondary: "#7c8798",
                    success: "#2ea043",
                    warning: "#d29922",
                    error: "#f85149",
                    info: "#58a6ff",
                },
            },
            devanoogaLight: {
                dark: false,
                colors: {
                    background: "#f5f6f8",
                    surface: "#ffffff",
                    primary: "#2563eb",
                    secondary: "#5b6472",
                    success: "#1a7f37",
                    warning: "#9a6700",
                    error: "#b3261e",
                    info: "#0969da",
                },
            },
        },
    },
});
