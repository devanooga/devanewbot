const { defineConfig } = require("@vue/cli-service");

const AppName = "Devanooga";
const Background = "#11141a";
const Accent = "#E0644F"; // Devanooga coral, from the site logo

module.exports = defineConfig({
    transpileDependencies: true,
    outputDir: "../wwwroot",
    chainWebpack: (config) => {
        config.plugin("html").tap((args) => {
            args[0].title = AppName;
            return args;
        });
    },
    pwa: {
        name: AppName,
        themeColor: Accent,
        msTileColor: Background,
        appleMobileWebAppStatusBarStyle: "black-translucent",
        manifestOptions: {
            name: AppName,
            short_name: AppName,
            description: "Devanooga community signup and the devanewbot admin panel",
            background_color: Background,
            theme_color: Accent,
        },
        iconPaths: {
            faviconSVG: "img/icons/favicon.svg",
            favicon32: null,
            favicon16: null,
            appleTouchIcon: "img/icons/apple-touch-icon.png",
            maskIcon: null,
            msTileImage: "img/icons/msapplication-icon-144x144.png",
        },
    },
});
