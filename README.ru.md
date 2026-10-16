<p align="center"><a href="README.md">English</a> - <b>Русский</b></p>

<p align="center">
  <a href="https://github.com/jrfrigat/Draftline/actions/workflows/ci.yml"><img src="https://github.com/jrfrigat/Draftline/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/jrfrigat/Draftline/actions/workflows/pages.yml"><img src="https://github.com/jrfrigat/Draftline/actions/workflows/pages.yml/badge.svg" alt="Pages"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License: MIT"></a>
  <img src="https://img.shields.io/badge/.NET-10-512BD4.svg" alt=".NET 10">
</p>

# Draftline

**Draftline** — автономный редактор low-fi вайрфреймов на Blazor WebAssembly (.NET 10) и
[Flare](https://github.com/jrfrigat/Flare). Каркас экрана собирается из схематичных блоков без
визуального шума, экраны связываются переходами, а результат можно прокликать. Приложение работает
целиком в браузере: без backend и аккаунтов, документы остаются на вашем компьютере.

**Демо:** <https://jrfrigat.github.io/Draftline/>. Это устанавливаемое PWA, после первого открытия
оно работает и без сети.

## Возможности

- **Блоки**: 57 видов (формы, таблицы, деревья, вкладки, меню, диаграммы, рамки устройств, медиа,
  заметки и фигуры) с редактируемыми текстом, данными и состояниями.
- **Экраны**: несколько экранов в макете, переходы и диалоги, независимые варианты экранов,
  интерактивный просмотр с временными состояниями.
- **Редактирование**: группы, буфер блоков между экранами и проектами, блокировка, скрытие,
  направляющие, отступы и поворот, история отмены и поиск по всему макету.
- **Повторное использование**: компоненты с экземплярами, которые сохраняют свои правки;
  локальные изображения и офлайн-иконки.
- **Темы**: конструктор и макет оформляются независимо. Доступны Fluent UI 2, Material Design 3
  Expressive, неофициальная тема в стиле 1С `Flare.Theme.OneC` и черновой вид макета.
- **Холст**: масштаб, вписывание по ширине, режим «Рука», выбор и перемещение блоков с клавиатуры.
- **Файлы**: переносимый JSON, SVG, PNG и PDF (растровые страницы с кликабельными переходами),
  ZIP-архивы сразу со всеми экранами.
- **Большие документы**: импорт JSON читает файл потоком и показывает прогресс, поэтому макет
  из сотен экранов открывается без нехватки памяти.

## Быстрый старт

### В браузере

Откройте <https://jrfrigat.github.io/Draftline/>. Проекты хранятся в IndexedDB браузера. Чтобы
перенести макет в другой браузер или на другой компьютер, выгрузите JSON.

### Локально

```powershell
dotnet run --project src/Draftline.App
```

Откройте HTTP-адрес из вывода команды. Подробнее — в разделе [Начало работы](docs/ru/getting-started.md).

## Установка из релиза

К каждому релизу (опубликованный тег `v*`) прикладывается `draftline-vX.Y.Z.zip` — статический сайт
для любого веб-сервера, который отдаёт его из корня сайта. См. [Развёртывание](docs/ru/deployment.md).

## Состав решения

| Проект | Назначение |
| :-- | :-- |
| `src/Draftline.App` | Редактор: доменная модель, сессия документа, хранение, экспорт и интерфейс |
| `src/Flare.Theme.OneC` | Неофициальная тема Flare в стиле 1С, отдельный пакет |
| `tests/Draftline.App.Tests` | Регрессионные спеки xUnit v3: модель, хранение, импорт и экспорт |

## Документация

Полная документация — в **[docs/](docs/README.md)** (английский и русский):

| Документ | Описание |
| :-- | :-- |
| [Начало работы](docs/ru/getting-started.md) | Запуск, первый макет, просмотр и файлы |
| [Формат файла](docs/ru/file-format.md) | JSON-документ, версии схемы, хранение и офлайн |
| [Архитектура](docs/ru/architecture.md) | Проекты, слои, сессия документа и поток данных |
| [Развёртывание](docs/ru/deployment.md) | GitHub Pages, статический хостинг, релизы |
| [Разработка](docs/ru/development.md) | Сборка, тесты, конвенции |

## Сборка и тесты

```sh
dotnet build Draftline.slnx -c Release
dotnet test --solution Draftline.slnx -c Release
```

Требуется **.NET 10** (SDK 10.0.400 или новее, см. `global.json`).

## Участие и изменения

- [CONTRIBUTING.md](CONTRIBUTING.md) — как внести вклад.
- [CHANGELOG.ru.md](CHANGELOG.ru.md) — история изменений.
- [SECURITY.md](SECURITY.md) — как сообщить об уязвимости.

Интерфейс построен на [Flare](https://github.com/jrfrigat/Flare).

## Лицензия

[MIT](LICENSE) © 2026 FrigaT
