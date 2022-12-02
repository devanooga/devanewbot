<template>
    <div class="header">
        <h1>
            <strong>Devanooga</strong>
        </h1>
        <h2>Join the Devanooga Community</h2>
    </div>
    <div class="error" v-if="showAlreadyInTeamMessage">
        This e-mail address is already a member of the community.
    </div>
    <div class="content">
        <div class="information">
            <form class="form" @submit.prevent="signup" v-if="!signupAccepted">
                <input
                    class="field"
                    type="email"
                    name="email"
                    autofocus
                    placeholder="Enter Email"
                    v-model="email"
                />
                <input class="submit" type="submit" value="Join" />
            </form>
            <div class="accept-message" v-else>
                Thank you for signing up, please check your e-mail for an invite
                link!
            </div>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, ref } from "vue";
import axios from "axios";
import { useReCaptcha, IReCaptchaComposition } from "vue-recaptcha-v3";

export default defineComponent({
    name: "SlackSignup",
});
</script>

<script setup lang="ts">
const { executeRecaptcha, recaptchaLoaded } =
    useReCaptcha() as IReCaptchaComposition;
const email = ref("");
const showAlreadyInTeamMessage = ref(false);
const signupAccepted = ref(false);
await recaptchaLoaded();
async function signup() {
    showAlreadyInTeamMessage.value = false;
    signupAccepted.value = false;
    await axios
        .post("/api/v0/signup", {
            email: email.value,
            token: await executeRecaptcha("signup"),
        })
        .then(() => {
            signupAccepted.value = true;
        })
        .catch((error) => {
            switch (error.response.data.error) {
                case "already_in_team":
                    showAlreadyInTeamMessage.value = true;
                    break;
                case "token_verification_failed":
                    alert("wat");
                    break;
            }
        });
}
</script>

<style lang="scss">
/* Reset */

body,
div,
dl,
dt,
dd,
ul,
ol,
li,
h1,
h2,
h3,
h4,
h5,
h6,
pre,
code,
form,
fieldset,
legend,
input,
textarea,
p,
blockquote,
th,
td {
    margin: 0;
    padding: 0;
}
table {
    border-collapse: collapse;
    border-spacing: 0;
}
fieldset,
img {
    border: 0;
}
address,
caption,
dfn,
th,
var {
    font-style: normal;
    font-weight: normal;
}
li {
    list-style: none;
}
caption,
th {
    text-align: left;
}
h1,
h2,
h3,
h4,
h5,
h6 {
    font-size: 100%;
    font-weight: normal;
}

html,
body {
    margin: 0;
    padding: 0;
    font-family: "Open Sans", sans-serif;
    background: #fafafa center top no-repeat;
    background-size: cover;
    height: 100%;
    width: 100%;
}

#wrapper {
    max-width: 100%;
    width: 940px;
    margin: 0 auto;
}

.main {
    max-width: 100%;
    width: 940px;
    float: left;
}

.header,
.content,
.bottom,
.sep2 {
    width: 100%;
    float: left;
}

.header {
    margin-top: 160px;
    margin-bottom: 40px;
}

h1 {
    font-family: "Open Sans", sans-serif;
    color: #232323;
    text-align: center;
    font-size: 50px;
    font-weight: 300;
    margin: 0 0 10px 0;
}

h1 strong {
    font-size: 38px;
    font-weight: 700;
}

h2 {
    text-align: center;
    font-weight: 300;
    font-size: 20px;
    color: #232323;
}

h2 strong {
    font-weight: 700;
    font-style: italic;
}

.information {
    width: 480px;
    padding-top: 35px;
    margin: 0 auto;
}

h3 {
    font-size: 28px;
    font-weight: 600;
    color: #232323;
}

.information p {
    font-size: 16px;
    color: #232323;
    display: block;
}

.accept-message {
    text-align: center;
}

.form {
    position: relative;
    width: 478px;
    margin-top: 20px;
}

.field {
    background: #fafafa;
    width: 448px;
    -webkit-border-radius: 30px;
    -moz-border-radius: 30px;
    border-radius: 30px;
    border: 1px solid;
    font-style: italic;
    font-family: "Lato", sans-serif;
    font-size: 16px;
    color: #232323;
    padding: 15px;
    margin-bottom: 15px;
    outline: none;
}
.field:focus {
    border: 1px solid #232323;
    padding: 15px;
}
@media only screen and (max-width: 480px) {
    .information {
        width: 100%;
    }
    .form {
        width: 90%;
        margin-left: 5%;
        margin-right: 5%;
    }

    .field {
        width: 90%;
    }
}

.submit {
    position: absolute;
    right: 7px;
    top: 8px;
    padding: 8px 30px;
    -webkit-border-radius: 30px;
    -moz-border-radius: 30px;
    border-radius: 30px;
    background: #fafafa;
    border: 1px solid;
    font-family: "Lato", sans-serif;
    font-size: 14px;
    font-weight: bold;
    cursor: pointer;
}

.submit:hover {
    background: #eeeeee;
}

.error {
    color: #fe7070;
    font-weight: 700;
    text-align: center;
    font-size: 1.2em;
}

a {
    color: #92bcf2;
    font-weight: 700;
}

a:visited,
a:hover {
    color: #7eb6ff;
}

.g-recaptcha,
.g-recaptcha > div {
    margin: auto;
}
</style>
