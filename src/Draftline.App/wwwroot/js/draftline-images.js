export async function dimensions(data) {
    const image = await load(data);
    return [image.naturalWidth, image.naturalHeight];
}
async function load(data) {
    const image = new Image();
    image.src = data;
    await image.decode();
    if (!image.naturalWidth || image.naturalWidth * image.naturalHeight > 32000000)
        throw new Error('Размер изображения превышает 32 мегапикселя.');
    return image;
}

const maximumSide = 2048, maximumBytes = 2 * 1024 * 1024;
function byteSize(data) { return Math.floor((data.length - data.indexOf(',') - 1) * 3 / 4); }
export async function prepare(data) {
    const image = await load(data);
    const scale = Math.min(1, maximumSide / Math.max(image.naturalWidth, image.naturalHeight));
    if (scale === 1 && byteSize(data) <= maximumBytes)
        return [data, String(image.naturalWidth), String(image.naturalHeight), String(byteSize(data)), 'false'];
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.round(image.naturalWidth * scale));
    canvas.height = Math.max(1, Math.round(image.naturalHeight * scale));
    const context = canvas.getContext('2d');
    if (!context) throw new Error('Браузер не поддерживает оптимизацию изображения.');
    context.drawImage(image, 0, 0, canvas.width, canvas.height);
    let encoded;
    for (const quality of [.9, .8, .65]) {
        encoded = canvas.toDataURL('image/webp', quality);
        if (byteSize(encoded) <= maximumBytes) break;
    }
    if (byteSize(encoded) > maximumBytes) throw new Error('После оптимизации файл превышает 2 МБ. Выберите меньший файл.');
    return [encoded, String(canvas.width), String(canvas.height), String(byteSize(encoded)), 'true'];
}
