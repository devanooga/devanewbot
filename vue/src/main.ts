import { createApp } from "vue";
import App from "./App.vue";
import "./registerServiceWorker";
import router from "./router";
import axios from "axios";
import VueAxios from "vue-axios";
import { VueReCaptcha } from "vue-recaptcha-v3";

interface TokenResponse {
    token: string;
}

axios.get<TokenResponse>("/api/v0/signup").then((response) => {
    createApp(App)
        .use(router)
        .use(VueAxios, { $http: axios })
        .use(VueReCaptcha, { siteKey: response.data.token })
        .mount("#app");
});
