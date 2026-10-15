export async function dimensions(data) {
    const image = new Image();
    image.src = data;
    await image.decode();
    if (!image.naturalWidth || image.naturalWidth * image.naturalHeight > 32000000)
        throw new Error('Размер изображения превышает 32 мегапикселя.');
    return [image.naturalWidth, image.naturalHeight];
}
