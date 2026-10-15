// Offline image/PDF export. The authored SVG is the only drawing source.
function save(name, blob) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a'); a.href = url; a.download = name;
    document.body.appendChild(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

async function raster(page, type) {
    if (!(page.width > 0 && page.height > 0)) throw new Error('Некорректный размер экрана');
    const scale = Math.min(2, 8192 / page.width, 8192 / page.height, Math.sqrt(32000000 / page.width / page.height));
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.round(page.width * scale)); canvas.height = Math.max(1, Math.round(page.height * scale));
    const url = URL.createObjectURL(new Blob([page.svg], {type:'image/svg+xml;charset=utf-8'}));
    try {
        const image = new Image();
        await new Promise((resolve, reject) => { image.onload = resolve; image.onerror = () => reject(new Error('Не удалось прочитать рисунок экрана')); image.src = url; });
        const context = canvas.getContext('2d');
        if (!context) throw new Error('Не удалось подготовить изображение');
        context.fillStyle = '#fff'; context.fillRect(0, 0, canvas.width, canvas.height);
        context.drawImage(image, 0, 0, canvas.width, canvas.height);
        const blob = await new Promise(resolve => canvas.toBlob(resolve, type, 0.95));
        if (!blob) throw new Error('Не удалось создать изображение');
        return {bytes:new Uint8Array(await blob.arrayBuffer()), width:canvas.width, height:canvas.height};
    } finally { URL.revokeObjectURL(url); canvas.width = canvas.height = 0; }
}

export async function exportPng(pages) {
    for (const page of pages) { const result = await raster(page, 'image/png'); save(page.fileName, new Blob([result.bytes], {type:'image/png'})); }
}

const encoder = new TextEncoder();
const ascii = text => encoder.encode(text);
const concat = chunks => { const out = new Uint8Array(chunks.reduce((sum, item) => sum + item.length, 0)); let offset = 0; for (const item of chunks) { out.set(item, offset); offset += item.length; } return out; };
const n = value => Number(value.toFixed(4)).toString();
const pdfString = value => '(' + value.replaceAll('\\', '\\\\').replaceAll('(', '\\(').replaceAll(')', '\\)').replace(/[\r\n]/g, '') + ')';

// Kept separate from canvas so xref, page order and annotations can be verified independently.
export function buildPdf(pages, images) {
    if (pages.length === 0 || pages.length !== images.length) throw new Error('Нет экранов для экспорта');
    const objects = [null, null];
    const add = bytes => { objects.push(typeof bytes === 'string' ? ascii(bytes) : bytes); return objects.length; };
    const pageIds = pages.map(() => add(''));
    const byId = new Map(pages.map((page, i) => [page.id, pageIds[i]]));
    for (let i = 0; i < pages.length; i++) {
        const page = pages[i], image = images[i], w = page.width * .75, h = page.height * .75;
        const imageId = add(concat([ascii(`<< /Type /XObject /Subtype /Image /Width ${image.width} /Height ${image.height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length ${image.bytes.length} >>\nstream\n`),image.bytes,ascii('\nendstream')]));
        const command = ascii(`q ${n(w)} 0 0 ${n(h)} 0 0 cm /Im Do Q`);
        const contentId = add(concat([ascii(`<< /Length ${command.length} >>\nstream\n`),command,ascii('\nendstream')]));
        const annotations = [];
        for (const link of page.links || []) {
            const x1 = Math.max(0, link.x) * .75, y1 = Math.max(0, page.height - link.y - link.height) * .75;
            const x2 = Math.min(page.width, link.x + link.width) * .75, y2 = Math.min(page.height, page.height - link.y) * .75;
            if (x2 <= x1 || y2 <= y1) continue;
            const target = byId.get(link.targetId);
            let action = target ? `/Dest [${target} 0 R /Fit]` : /^https?:\/\//i.test(link.url || '') ? `/A << /S /URI /URI ${pdfString(link.url)} >>` : '';
            if (!action) continue;
            annotations.push(add(`<< /Type /Annot /Subtype /Link /Rect [${n(x1)} ${n(y1)} ${n(x2)} ${n(y2)}] /Border [0 0 0] ${action} >>`));
        }
        objects[pageIds[i]-1] = ascii(`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 ${n(w)} ${n(h)}] /Resources << /XObject << /Im ${imageId} 0 R >> >> /Contents ${contentId} 0 R /Annots [${annotations.map(id=>id+' 0 R').join(' ')}] >>`);
    }
    objects[0] = ascii('<< /Type /Catalog /Pages 2 0 R >>');
    objects[1] = ascii(`<< /Type /Pages /Count ${pages.length} /Kids [${pageIds.map(id=>id+' 0 R').join(' ')}] >>`);
    const chunks = [ascii('%PDF-1.4\n%Draftline\n')], offsets = [0]; let length = chunks[0].length;
    objects.forEach((body, i) => { offsets.push(length); const obj = concat([ascii(`${i+1} 0 obj\n`),body,ascii('\nendobj\n')]); chunks.push(obj); length += obj.length; });
    const xref = length;
    chunks.push(ascii(`xref\n0 ${objects.length+1}\n0000000000 65535 f \n` + offsets.slice(1).map(offset=>offset.toString().padStart(10,'0')+' 00000 n \n').join('') + `trailer\n<< /Size ${objects.length+1} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF\n`));
    return concat(chunks);
}

export async function exportPdf(fileName, pages) {
    const images = [];
    for (const page of pages) images.push(await raster(page, 'image/jpeg'));
    save(fileName, new Blob([buildPdf(pages, images)], {type:'application/pdf'}));
}
