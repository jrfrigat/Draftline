let reference, dirty = false;
function save() { if (dirty) reference?.invokeMethodAsync('SaveBeforeHide').catch(() => {}); }
function visibility() { if (document.visibilityState === 'hidden') save(); }
function unload(event) {
    if (!dirty) return;
    event.preventDefault();
    event.returnValue = '';
}
export function attach(value) {
    detach(); reference = value;
    document.addEventListener('visibilitychange', visibility);
    window.addEventListener('pagehide', save);
    window.addEventListener('beforeunload', unload);
}
export function setDirty(value) { dirty = value; }
export function detach() {
    document.removeEventListener('visibilitychange', visibility);
    window.removeEventListener('pagehide', save);
    window.removeEventListener('beforeunload', unload);
    reference = null; dirty = false;
}
