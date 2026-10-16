# Развёртывание

Draftline — набор статических файлов. Подойдёт любой хостинг, который их отдаёт; настраивать во время
работы нечего.

## GitHub Pages

`.github/workflows/pages.yml` публикует каждый push в `main` на `https://<owner>.github.io/<repo>/`.

Однократная настройка: **Settings → Pages → Build and deployment → Source: GitHub Actions**.

Что делает workflow:

1. **До** публикации заменяет `<base href="/">` в `src/Draftline.App/wwwroot/index.html` на `/<repo>/`.
   Список ресурсов service worker'а содержит хэш `index.html`, поэтому файл нужно изменить до того, как
   этот список будет сформирован. В репозитории остаётся `/` для `dotnet run` и хостинга в корне.
2. Выполняет `dotnet publish -c Release`.
3. Копирует `index.html` в `404.html`, чтобы глубокая ссылка открывала приложение, а не ошибку Pages,
   и добавляет `.nojekyll`.
4. Загружает `dist/wwwroot` и публикует его через `actions/deploy-pages`.

Чтобы проверить сборку для Pages локально, опубликуйте её с заменённым base и отдайте результат по тому
же подпути, например `http://localhost:8080/Draftline/`.

## Любой статический хостинг

```powershell
dotnet publish src/Draftline.App -c Release -o dist
```

Отдавайте `dist/wwwroot` из корня сайта. Для другого подпути замените `<base href>` в `index.html` перед
публикацией, как это делает workflow Pages. Хостинг должен:

- отдавать `.wasm` как `application/wasm`, а `.webmanifest` как `application/manifest+json`;
- отдавать предсжатые `.br`/`.gz`, если браузер их принимает (необязательно, но быстрее);
- для неизвестных путей возвращать `index.html` (или `404.html`);
- работать по HTTPS: service worker вне `localhost` требует его.

В `index.html` задан Content-Security-Policy: разрешены только собственный origin приложения и Google Fonts.

## Релиз

1. Убедитесь, что `main` зелёный (CI).
2. Поставьте тег: `git tag v0.4.0 && git push origin v0.4.0`.
3. Workflow `Release` собирает и тестирует решение, публикует статический сайт и создаёт GitHub
   Release с приложенным `draftline-v0.4.0.zip` и сгенерированными заметками.

Перед тегом обновите [CHANGELOG.md](../../CHANGELOG.md) и [CHANGELOG.ru.md](../../CHANGELOG.ru.md).
