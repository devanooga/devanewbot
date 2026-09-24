import { ref } from "vue";

const showNotice = ref(false);
const noticeText = ref("");
const noticeColor = ref("success");

export function useNotice() {
    function notify(text: string, color: "success" | "error" = "success") {
        noticeText.value = text;
        noticeColor.value = color;
        showNotice.value = true;
    }

    return { showNotice, noticeText, noticeColor, notify };
}
