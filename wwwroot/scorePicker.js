let handler = null;

export function attach(dotnetRef) {
    detach(); // safety: remove previous
    handler = (e) => {
        // ignore if user is typing in an input/textarea/select
        const tag = (e.target && e.target.tagName) ? e.target.tagName.toLowerCase() : "";
        if (tag === "input" || tag === "textarea" || tag === "select" || e.isComposing) return;

        // send only keys we care about: digits, enter, backspace, escape
        const k = e.key;
        if (/^[0-9]$/.test(k) || k === "Enter" || k === "Backspace" || k === "Escape") {
            dotnetRef.invokeMethodAsync("HandleKeyFromJs", k);
        }
    };
    window.addEventListener("keydown", handler, true);
}

export function detach() {
    if (handler) {
        window.removeEventListener("keydown", handler, true);
        handler = null;
    }
}
