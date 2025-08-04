export function set(key, value) {
    try { localStorage.setItem(key, value); } catch (_) { }
}
export function get(key) {
    try { return localStorage.getItem(key); } catch (_) { return null; }
}
export function remove(key) {
    try { localStorage.removeItem(key); } catch (_) { }
}