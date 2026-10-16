# Архитектура

Draftline — клиентское приложение Blazor WebAssembly: без сервера и без API. Статическая сборка —
это весь продукт.

## Проекты

| Проект | Роль |
| :-- | :-- |
| `Draftline.App` | Редактор, публикуется как PWA |
| `Flare.Theme.OneC` | Неофициальная тема Flare в стиле 1С (.NET 8, публичный API Flare), собирается в отдельный пакет |
| `Draftline.App.Tests` | Спеки xUnit v3 на Microsoft.Testing.Platform |

## Внутри `Draftline.App`

| Папка | Содержимое |
| :-- | :-- |
| `Model` | Домен: `DraftDocument`, `DraftLayer` (экран), `WireframeElement` (блок), варианты, компоненты, геометрия, направляющие, состояния элементов |
| `Services` | `DocumentSession` (открытый документ и все команды над ним), `DocumentHistory`, `AutosaveService`, `ProjectCatalog`, адаптеры хранилища, `DraftlineJson`, `DocumentFileService` и `SvgExporter` |
| `Components`, `Pages` | Интерфейс: холст, палитра, инспекторы, панели. `Pages/Home.razor` — страница редактора |
| `Theming` | `ThemeCoordinator` и `MockupThemeResolver`: конструктор и макет оформляются независимо |
| `wwwroot` | `index.html`, service worker'ы, манифест, иконки, `css/draftline.css` и JS-модули (`js/draftline-*.js`) |

## Поток данных

1. `Home.razor` получает у `ProjectCatalog` последний проект и открывает его в `DocumentSession`.
2. Каждая правка проходит через `DocumentSession`: она попадает в `DocumentHistory` (один жест,
   например перетаскивание, — один шаг отмены), а сессия поднимает `Changed`.
3. `AutosaveService` с задержкой записывает изменённый документ через `ProjectCatalog` в
   `IBrowserStorage` и дописывает его перед сменой проекта или уходом со страницы.
4. `IBrowserStorage` — это `FallbackBrowserStorage` поверх `IndexedDbBrowserStorage`: IndexedDB, когда
   браузер разрешает, и память, когда нет.
5. Холст рисует блоки через `WireframeElementView`; `ElementFragmentCache` переиспользует фрагмент блока,
   который только сдвинулся, поэтому перетаскивание не перерисовывает его содержимое.
6. Экспорт рисует каждый экран в SVG через `SvgExporter`. PNG и PDF растеризуются из этого SVG в
   браузере модулем `js/draftline-export.js`, ZIP-архивы собирает локально `js/draftline-zip.js`.

## Импорт

`DocumentFileService` передаёт выбранный файл в .NET потоком (`IJSStreamReference`), и тот никогда не
становится строкой JavaScript. `DraftlineJson.ReadAsync` читает его частями с прогрессом по байтам,
проверяет заголовок через `Utf8JsonReader`, десериализует один раз и нормализует экраны пачками.
`ProjectCatalog.PrepareOwnedImportAsync` принимает разобранную модель без копирования, а
`CommitImportAsync` сохраняет её атомарно. Пока идёт импорт, редактор недоступен для ввода.

## Flare

Оболочка (лента, закрепляемые панели, строка состояния) — `Flare.Components.IDE`. Компоненты и токены
берутся из Flare 0.44 с nuget.org. Правки самой библиотеки живут в
[репозитории Flare](https://github.com/jrfrigat/Flare), а не здесь.
