// The app's storage, as the browser keeps it: localStorage behind four small functions.
//
// The functions answer instead of throwing wherever they can. A browser in a private window, a
// storage area the settings forbid and a write that does not fit the quota are all ordinary events in
// a web application, and each of them has an answer a person can read - so the layer above deals with
// a message rather than with an exception it cannot explain.

const probeKey = 'draftline.probe';

// True when there is somewhere to write at all. Asked once, at startup, so the editor can say that
// this session will not be remembered instead of quietly losing the work at the first reload.
export function isAvailable() {
    try {
        window.localStorage.setItem(probeKey, '1');
        window.localStorage.removeItem(probeKey);
        return true;
    } catch (error) {
        return false;
    }
}

// The value under a key, or null when there is none - or when the storage cannot be read.
export function get(key) {
    try {
        return window.localStorage.getItem(key);
    } catch (error) {
        return null;
    }
}

// Writes a value, and answers null on success or the sentence to show on failure.
export function set(key, value) {
    try {
        window.localStorage.setItem(key, value);
        return null;
    } catch (error) {
        if (error && (error.name === 'QuotaExceededError' || error.name === 'NS_ERROR_DOM_QUOTA_REACHED')) {
            return 'в браузере закончилось место для макетов';
        }

        return 'браузер не разрешил запись в своё хранилище';
    }
}

export function remove(key) {
    try {
        window.localStorage.removeItem(key);
    } catch (error) {
        // Nothing to do: the key stays, and the next write will try again.
    }
}

// Every key the page can see.
export function keys() {
    const result = [];

    try {
        for (let index = 0; index < window.localStorage.length; index++) {
            const key = window.localStorage.key(index);
            if (key) {
                result.push(key);
            }
        }
    } catch (error) {
        // A storage that cannot be listed has no keys as far as the app is concerned.
    }

    return result;
}
