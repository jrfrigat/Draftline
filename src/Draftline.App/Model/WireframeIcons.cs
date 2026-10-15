namespace Draftline.App.Model;

public sealed record WireframeIcon(string Key,string Name,string Path);
/// <summary>Small offline wireframe symbols in a common 24px coordinate system.</summary>
public static class WireframeIcons
{
    public static readonly IReadOnlyList<WireframeIcon> All = new WireframeIcon[]
    {
        new("search","Поиск search","M10 3a7 7 0 1 0 0 14a7 7 0 1 0 0-14 M15 15l6 6"),
        new("home","Дом home","M3 11l9-8 9 8 M5 10v11h14V10 M9 21v-8h6v8"),
        new("user","Пользователь user","M12 3a4 4 0 1 0 0 8a4 4 0 1 0 0-8 M4 21v-3a8 6 0 0 1 16 0v3"),
        new("mail","Почта mail","M3 5h18v14H3z M3 5l9 8 9-8"),
        new("phone","Телефон phone","M5 3h4l2 5-3 2a15 15 0 0 0 6 6l2-3 5 2v4c-9 5-23-9-16-16z"),
        new("check","Готово check","M3 12l6 6L21 5"),
        new("close","Закрыть close","M4 4l16 16 M20 4L4 20"),
        new("plus","Добавить plus","M12 3v18 M3 12h18"),
        new("minus","Убрать minus","M3 12h18"),
        new("menu","Меню menu","M3 5h18 M3 12h18 M3 19h18"),
        new("arrow-left","Назад arrow left","M21 12H3 M10 5l-7 7 7 7"),
        new("arrow-right","Вперёд arrow right","M3 12h18 M14 5l7 7-7 7"),
        new("arrow-down","Вниз arrow down","M12 3v18 M5 14l7 7 7-7"),
        new("arrow-up","Вверх arrow up","M12 21V3 M5 10l7-7 7 7"),
        new("edit","Редактировать edit pencil","M3 21l2-7L16 3l5 5L10 19z M14 5l5 5"),
        new("trash","Удалить trash","M4 6h16 M9 6V3h6v3 M6 6l1 15h10l1-15 M10 10v7 M14 10v7"),
        new("folder","Папка folder","M3 6h7l2 3h9v12H3z"),
        new("file","Файл file","M5 3h9l5 5v13H5z M14 3v6h5 M8 13h8 M8 17h8"),
        new("download","Скачать download","M12 3v12 M7 10l5 5 5-5 M3 17v4h18v-4"),
        new("upload","Загрузить upload","M12 16V3 M7 8l5-5 5 5 M3 17v4h18v-4"),
        new("lock","Замок lock","M5 10h14v11H5z M8 10V7a4 4 0 0 1 8 0v3 M12 14v3"),
        new("star","Звезда star","M12 2l3 7 7 1-5 5 1 7-6-4-6 4 1-7-5-5 7-1z"),
        new("heart","Сердце heart","M12 21L3 12C-2 4 8-1 12 6C16-1 26 4 21 12z"),
        new("calendar","Календарь calendar","M3 5h18v16H3z M7 2v6 M17 2v6 M3 10h18 M7 14h3 M14 14h3 M7 18h3"),
        new("clock","Часы clock","M12 2a10 10 0 1 0 0 20a10 10 0 1 0 0-20 M12 6v7l4 3"),
        new("bell","Уведомления bell","M4 17h16l-2-4V9a6 6 0 0 0-12 0v4z M9 20a3 3 0 0 0 6 0"),
        new("cart","Корзина cart","M2 3h3l3 13h11l3-9H6 M9 19a1 1 0 1 0 0 2a1 1 0 1 0 0-2 M18 19a1 1 0 1 0 0 2a1 1 0 1 0 0-2"),
        new("image","Картинка image","M3 3h18v18H3z M3 18l6-7 5 5 3-4 4 6 M16 6a2 2 0 1 0 0 4a2 2 0 1 0 0-4"),
        new("play","Воспроизвести play","M6 3l15 9-15 9z"),
        new("info","Информация info","M12 2a10 10 0 1 0 0 20a10 10 0 1 0 0-20 M12 10v7 M12 6v1")
    };
    public static WireframeIcon? Find(string? key)=>All.FirstOrDefault(i=>i.Key==key);
    public static bool Supports(WireframeKind kind)=>kind is WireframeKind.Icon or WireframeKind.Button or WireframeKind.Text or WireframeKind.Heading or WireframeKind.Link;
}
