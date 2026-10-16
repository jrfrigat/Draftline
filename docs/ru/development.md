# Разработка

## Требования

- .NET SDK **10.0.400** или более новый feature band 10.0 (см. `global.json`).
- Node.js нужен только для проверки синтаксиса JavaScript; сборка npm не использует.

## Сборка и тесты

```powershell
dotnet restore Draftline.slnx
dotnet build Draftline.slnx -c Release
dotnet test --solution Draftline.slnx -c Release
```

Сборка не должна давать предупреждений (CI собирает с `-warnaserror`), все тесты должны проходить.
Тесты написаны на xUnit v3 и запускаются через Microsoft.Testing.Platform; `global.json` выбирает этот
runner для `dotnet test`.

JavaScript редактора — обычные модули, которые никто не компилирует, поэтому перед коммитом проверьте
их синтаксис:

```powershell
Get-ChildItem src/Draftline.App/wwwroot -Recurse -Filter *.js | ForEach-Object {
    node --check $_.FullName
    if ($LASTEXITCODE -ne 0) { throw 'Ошибка синтаксиса JavaScript' }
}
```

## Пакеты Flare

Flare берётся с nuget.org; `nuget.config` называет только этот источник, поэтому CI и свежий клон
восстанавливают пакеты одинаково. Чтобы попробовать пакеты из локальной сборки Flare, добавьте её папку
`artifacts` как пользовательский источник, а не правьте `nuget.config`:

```powershell
dotnet nuget add source <путь-к-Flare>/artifacts -n flare-artifacts
```

Все пакеты Flare закреплены на одной версии: расхождение двух из них — сломанная тема.

## Структура решения

`Draftline.slnx` объединяет `src/Draftline.App`, `src/Flare.Theme.OneC` и `tests/Draftline.App.Tests`.
Папки внутри приложения описаны в [Архитектуре](architecture.md).

## Конвенции

- **Команды** над документом проходят через `DocumentSession`, чтобы попасть в историю и автосохранение.
  Компонент никогда не меняет модель в обход сессии.
- **Формат** меняется только вместе с `CurrentSchemaVersion`, миграцией старых файлов и тестом, который
  читает старый файл.
- **Компоненты и токены** берутся из Flare. Собственные CSS (`css/draftline.css`) и JS держатся
  минимальными.
- **Тексты интерфейса** — на русском. **Комментарии** объясняют «почему», а не пересказывают код.
- **Тесты** проверяют поведение: история, миграции схем, повреждённый JSON, геометрия, экспорт,
  каталог, импорт, автосохранение, жесты и варианты.

## Коммиты и ветки

`main` защищён CI. Изменения приходят через pull request: сборка без предупреждений, зелёные тесты и
понятное описание. Подробнее — в [CONTRIBUTING.md](../../CONTRIBUTING.md).
