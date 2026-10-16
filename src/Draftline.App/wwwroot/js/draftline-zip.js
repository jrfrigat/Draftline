// Stored ZIP entries: PNG is already compressed, SVG needs no extra dependency.
const encoder = new TextEncoder();
const crcTable = Uint32Array.from({length:256}, (_, value) => {
    for (let bit = 0; bit < 8; bit++) value = (value & 1) ? 0xedb88320 ^ (value >>> 1) : value >>> 1;
    return value >>> 0;
});
function crc32(bytes) {
    let crc = 0xffffffff;
    for (const byte of bytes) crc = crcTable[(crc ^ byte) & 255] ^ (crc >>> 8);
    return (crc ^ 0xffffffff) >>> 0;
}
function header(length) {
    const bytes = new Uint8Array(length), view = new DataView(bytes.buffer);
    return {bytes, u16:(offset,value)=>view.setUint16(offset,value,true), u32:(offset,value)=>view.setUint32(offset,value,true)};
}
export function buildZip(files) {
    if (files.length > 65535) throw new Error('Слишком много файлов для ZIP.');
    const chunks = [], central = [];
    let offset = 0, centralSize = 0;
    for (const file of files) {
        const name = encoder.encode(file.name.replace(/[\\/]/g, '-')), bytes = file.bytes;
        if (bytes.length > 0xffffffff || name.length > 65535) throw new Error('Файл слишком велик для ZIP.');
        const crc = crc32(bytes), local = header(30), directory = header(46);
        local.u32(0,0x04034b50); local.u16(4,20); local.u16(6,0x800); local.u16(12,33);
        local.u32(14,crc); local.u32(18,bytes.length); local.u32(22,bytes.length); local.u16(26,name.length);
        directory.u32(0,0x02014b50); directory.u16(4,20); directory.u16(6,20); directory.u16(8,0x800); directory.u16(14,33);
        directory.u32(16,crc); directory.u32(20,bytes.length); directory.u32(24,bytes.length); directory.u16(28,name.length); directory.u32(42,offset);
        chunks.push(local.bytes,name,bytes); central.push(directory.bytes,name);
        offset += local.bytes.length + name.length + bytes.length; centralSize += directory.bytes.length + name.length;
    }
    if (offset + centralSize > 0xffffffff) throw new Error('Архив слишком велик. Экспортируйте меньше экранов.');
    const end = header(22); end.u32(0,0x06054b50); end.u16(8,files.length); end.u16(10,files.length); end.u32(12,centralSize); end.u32(16,offset);
    const output = new Uint8Array(offset + centralSize + 22); let position = 0;
    for (const chunk of [...chunks,...central,end.bytes]) { output.set(chunk,position); position += chunk.length; }
    return output;
}
