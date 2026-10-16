// Draftline documents live in IndexedDB; localStorage is only the migration source.
const databaseName = 'draftline';
const storeName = 'entries';
let database;
function open() {
    if (!database) database = new Promise((resolve, reject) => {
        const request = indexedDB.open(databaseName, 1);
        request.onupgradeneeded = () => request.result.createObjectStore(storeName);
        request.onsuccess = () => {
            const db = request.result;
            db.onversionchange = () => { db.close(); database = null; };
            resolve(db);
        };
        request.onerror = () => { database = null; reject(request.error); };
        request.onblocked = () => { database = null; reject(new Error('Закройте другие вкладки Draftline и повторите.')); };
    });
    return database;
}
async function transaction(mode, action) {
    const db = await open();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, mode);
        let result;
        tx.oncomplete = () => resolve(result);
        tx.onerror = tx.onabort = () => reject(tx.error || new Error('Запись в хранилище не завершена.'));
        action(tx.objectStore(storeName), value => { result = value; });
    });
}
async function migrate() {
    const old = [];
    try {
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key?.startsWith('draftline.') && key !== 'draftline.probe') old.push([key, localStorage.getItem(key)]);
        }
    } catch { return; }
    if (!old.length) return;
    await transaction('readwrite', store => {
        for (const [key, value] of old) {
            const request = store.get(key);
            request.onsuccess = () => { if (request.result === undefined) store.put(value, key); };
        }
    });
    // Keep originals until every migrated entry is committed. A failed migration removes nothing.
    for (const [key, value] of old) {
        try { if (localStorage.getItem(key) === value) localStorage.removeItem(key); } catch { }
    }
}
export async function isAvailable() {
    try { await open(); } catch { return false; }
    try { await migrate(); } catch { /* Originals remain readable; a later write reports quota errors. */ }
    return true;
}
export async function get(key) {
    const value = await transaction('readonly', (store, done) => {
        const request = store.get(key); request.onsuccess = () => done(request.result ?? null);
    });
    if (value !== null) return value;
    try { return key.startsWith('draftline.') ? localStorage.getItem(key) : null; } catch { return null; }
}
export async function set(key, value) {
    try { await transaction('readwrite', store => store.put(value, key)); return null; }
    catch (error) {
        return error?.name === 'QuotaExceededError' ? 'в браузере закончилось место для макетов; выгрузите JSON' :
            'браузер не разрешил запись в хранилище: ' + (error?.message || 'неизвестная ошибка');
    }
}
export async function setMany(entries) {
    try {
        await transaction('readwrite', store => {
            for (const [key, value] of Object.entries(entries)) store.put(value, key);
        });
        return null;
    } catch (error) {
        return error?.name === 'QuotaExceededError' ? 'в браузере закончилось место для макетов; выгрузите JSON' :
            'браузер не разрешил запись в хранилище: ' + (error?.message || 'неизвестная ошибка');
    }
}
export async function remove(key) {
    await transaction('readwrite', store => store.delete(key));
    try { localStorage.removeItem(key); } catch { }
}
export async function keys() {
    const result = await transaction('readonly', (store, done) => {
        const request = store.getAllKeys(); request.onsuccess = () => done(request.result);
    });
    try {
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key?.startsWith('draftline.')) result.push(key);
        }
    } catch { }
    return [...new Set(result)];
}
