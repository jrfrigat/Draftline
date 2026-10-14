// Files in and out of the editor.
//
// A download is a Blob behind a link the browser clicks for us, and choosing a file is an input the
// person clicks. Both are browser gestures with no equivalent on the C# side, which is why they live
// here; what the file is called and what is in it is decided on the other side of the boundary.

// Hands the text to the browser as a file, under the name it was given.
export function download(fileName, contentType, text) {
    const blob = new Blob([text], { type: contentType + ';charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = url;
    link.download = fileName;
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    link.remove();

    // The object URL holds the whole file in memory until it is revoked. The browser has the download
    // by now, so it can be given back after a moment.
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// Asks the person for a file and answers its text, or null when they changed their mind.
export function pickTextFile(accept) {
    return new Promise(resolve => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = accept;
        input.style.display = 'none';
        document.body.appendChild(input);

        let settled = false;

        const finish = value => {
            if (settled) {
                return;
            }

            settled = true;
            input.remove();
            resolve(value);
        };

        input.addEventListener('change', () => {
            const file = input.files && input.files[0];

            if (!file) {
                finish(null);
                return;
            }

            const reader = new FileReader();
            reader.onload = () => finish(typeof reader.result === 'string' ? reader.result : null);
            reader.onerror = () => finish(null);
            reader.readAsText(file);
        });

        // A cancelled picker does not always raise an event. The window coming back without a chosen
        // file is the only signal there is, and answering null then is what keeps the promise from
        // hanging for the rest of the session.
        window.addEventListener('focus', () => {
            setTimeout(() => {
                if (!input.files || input.files.length === 0) {
                    finish(null);
                }
            }, 600);
        }, { once: true });

        input.click();
    });
}
