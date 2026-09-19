import { createApp } from "vue";
import App from "./App.vue";
import "./registerServiceWorker";
import router from "./router";
import axios from "axios";
import VueAxios from "vue-axios";
import { VueReCaptcha } from "vue-recaptcha-v3";
import vuetify from "./plugins/vuetify";

interface TokenResponse {
    token: string;
}

const app = createApp(App).use(router).use(VueAxios, axios).use(vuetify);

axios
    .get<TokenResponse>("/api/v0/signup")
    .then((response) => {
        app.use(VueReCaptcha, {
            loaderOptions: { autoHideBadge: true },
            siteKey: response.data.token,
        });
    })
    .catch(() => {
        // The signup widget is only needed on the public page; the admin app loads without it.
    })
    .finally(() => app.mount("#app"));
