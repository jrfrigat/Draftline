let media, listener;
export function attach(reference) {
    detach();
    media = window.matchMedia('(prefers-color-scheme: dark)');
    listener = event => reference.invokeMethodAsync('SystemChanged', event.matches);
    media.addEventListener('change', listener);
    return media.matches;
}
export function detach() {
    if (media && listener) media.removeEventListener('change', listener);
    media = listener = null;
}
