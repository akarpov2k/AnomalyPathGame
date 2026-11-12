# AnomalyPath — Аномальное путешествие

2D-игра на Unity: картинка-локация, кнопочные действия, пошаговый бой, лут, репутация.
Сеттинг — постапокалипсис с аномальными зонами.

- Движок: **Unity 6000.6.6f1**, шаблон Universal 2D (URP 17.6, Renderer2D).
- Геймплей и правила: [`docs/GAME_MANIFEST.md`](docs/GAME_MANIFEST.md) — источник истины.
- Схемы данных: [`docs/DATA_SCHEMAS.md`](docs/DATA_SCHEMAS.md).
- План работ: [`docs/ROADMAP.md`](docs/ROADMAP.md) — что сделано, что в работе, что дальше.
- Лицензия и состав прав: [`LICENSE`](LICENSE), [`NOTICE.md`](NOTICE.md);
  порядок публикации и защиты прав: [`docs/PUBLISHING.md`](docs/PUBLISHING.md).

## Лицензия

Исходники, данные и документация — [PolyForm Noncommercial 1.0.0](LICENSE)
(`SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0`), копирайт `© 2026 akarpov2k`.

Коротко:

- **можно** читать, изучать, запускать и менять для себя, делиться игрой и форками
  **без коммерческой цели** — при условии, что вместе с ними идут `LICENSE` и строка
  `Required Notice: Copyright © 2026 akarpov2k`;
- **нельзя** продавать игру, её части и производные, брать плату за доступ, включать код
  и ассеты в коммерческие продукты и сервисы, использовать в рекламе и обучении моделей
  за плату.

Билд бесплатный. Донаты (Boosty) — добровольная поддержка разработки, а не оплата игры:
они не дают прав сверх лицензии. Сторонние компоненты и их лицензии — в [NOTICE.md](NOTICE.md).

## Структура

| Путь | Что там |
| --- | --- |
| `src/AnomalyPath.Core/` | локальный UPM-пакет `com.anomalypath.core`: правила, данные, репозитории, сервисы, DI — без UnityEngine |
| `src/AnomalyPath.Tests/` | пакет `com.anomalypath.tests`: общий `TestSuite` и EditMode-обёртка |
| `tests/AnomalyPath.Core.Tests/` | консольный раннер того же набора тестов |
| `Assets/Scripts/Runtime/` | `GameBootstrap`: preflight перед стартом |
| `Assets/Data/` | контент: JSON-сущности, коллекции, тексты |

## Unity

Пакеты подключены в `Packages/manifest.json` ссылками `file:../src/...` (пути считаются от папки `Packages`),
поэтому Unity компилирует те же исходники, что и `dotnet`, без копий DLL. На загрузочной сцене
`GameBootstrap` вызывает `Preflight.Run` и при ошибках не даёт игре стартовать.

```csharp
var preflight = Preflight.Run(dataRoot);   // каждый файл читается один раз
if (!preflight.CanStart)
{
    ShowErrors(preflight.Report.Errors);   // список ошибок, игра не стартует
    return;
}

var services = preflight.Services;         // готовый DI-контейнер
```

## Сборка и тесты вне Unity

```powershell
dotnet build AnomalyPath.sln -m:1 -nodeReuse:false -p:UseSharedCompilation=false
dotnet run --project tests\AnomalyPath.Core.Tests -p:UseSharedCompilation=false -- Assets\Data
```

Код возврата `0` — все тесты прошли, `1` — есть падения. Артефакты сборки лежат в `.build/`.

## Тесты в Unity EditMode

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe" -batchmode -projectPath . `
  -runTests -testPlatform EditMode -testResults Logs\TestResults.EditMode.xml
```

Или в редакторе: **Window → General → Test Runner → EditMode**.

## Сборка Windows

Билд собирается тем же редактором в IL2CPP (Release, Managed Stripping Medium, .NET Standard 2.1).

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe" -batchmode -quit -projectPath . `
  -executeMethod AnomalyPath.Editor.BuildScript.BuildWindows -logFile Logs\Build.log
```

Или в редакторе: **Tools → AnomalyPath → Собрать Windows**. Результат — `Build/Windows/AnomalyPath.exe`;
данные игры (`Assets/Data`) копируются в `StreamingAssets` рядом с исполняемым файлом.

Версия игры — `0.1` (`bundleVersion` в `ProjectSettings.asset`); в главном меню подпись собирается
из каталога строк (`ui.mainmenu_version` = `{0} Alpha Demo`), то есть отображается `0.1 Alpha Demo`.

