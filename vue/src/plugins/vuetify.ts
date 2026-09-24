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
                    primary: "#E0644F",
                    secondary: "#7c8798",
                    success: "#2ea043",
                    warning: "#d6a419",
                    // Pushed away from the coral brand colour so a failure never reads as a brand accent.
                    error: "#c9333f",
                    info: "#58a6ff",
                },
            },
            devanoogaLight: {
                dark: false,
                colors: {
                    background: "#f5f6f8",
                    surface: "#ffffff",
                    primary: "#C4513D",
                    secondary: "#5b6472",
                    success: "#1a7f37",
                    warning: "#9a6700",
                    error: "#a1121f",
                    info: "#0969da",
                },
            },
        },
    },
});
