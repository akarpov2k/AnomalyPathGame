# Публикация AnomalyPath: лицензия, ассеты, билд

Порядок работ перед выкладкой билда и исходников. Лицензия — [PolyForm Noncommercial 1.0.0](../LICENSE),
уведомления и состав — [NOTICE.md](../NOTICE.md).

## 0. Решения

- Правообладатель в копирайте: `akarpov2k` (псевдоним, ст. 1265 ГК РФ).
- Билд бесплатный; донаты (Boosty) добровольные и не привязаны к доступу к игре.
- Исходники публикуются **в новом репозитории** с чистой историей; старый `Game1` архивируется
  или удаляется. Причина: история уже публичная и содержит чужие ассеты.
- **Новый репозиторий создан** (2026-11-12): `D:\Projects\AnomalyPathGame`, ветка `main`,
  `origin` — `git@github.com:akarpov2k/AnomalyPathGame.git`. **Полный снимок дерева перенесён
  2026-11-12** — источник `d7c4e56` («полировка»), копия без единого изменения файла: `Assets`,
  `Packages`, `ProjectSettings`, `src`, `tests`, `tools`, `docs`, `AnomalyPath.sln`,
  `Directory.Build.props`, `.gitignore`, `.vsconfig`, `LICENSE`, `NOTICE.md`, `README.md` —
  3352 файла, 546 МБ. **Initial commit сделан 2026-11-12** (перед ним из дерева убраны остатки
  прогонов: `Assets/InitTestScene*.unity` и шаблонная `Assets/Scenes/SampleScene.unity`; добавлен
  префаб экрана-заглушки). Осталось: `git push -u origin main` — делает пользователь.
- **Версия 0.1 demo** (решение пользователя, 2026-11-12): `bundleVersion` = `0.1`, в главном меню
  подпись из каталога строк `ui.mainmenu_version` = `{0} Alpha Demo`, то есть `0.1 Alpha Demo`.
  Номер тега при публикации — `v0.1`.
- **Распространение билда отложено** (2026-11-12): площадка раздачи не выбрана, поэтому §4 п. 4
  и пункты §7 про хеш и тег ждут решения.
- **Android-релиз не делается** (2026-11-12): APK остаётся инструментом разработки, подпись,
  AAB и политика магазина из плана сняты.
- **`generated/`** — публиковать ли `generated/prompts/` (и как это записать в `NOTICE.md`),
  решается после полировки.

## 1. Чужие ассеты: статус

Статус по каждому блоку; источники и подробности — в [NOTICE.md](../NOTICE.md).

| Что | Файлы | Статус / как заменить |
| --- | --- | --- |
| Фон окна инвентаря | `Assets/Art/Backdrops/inventory.png`, `inventory_trader.png` | **заменено 2026-11-12** сгенерированной панелью (Polza, Gemini; старая текстура референсом не использовалась), исходники — `generated/inventory-panel/`, промпт — `generated/prompts/inventory-panel.md`. Имя файла, GUID и имя спрайта те же, поэтому каталог спрайтов пересобирать не обязательно; проверить кадр окна инвентаря |
| Иконки NLC/GAMMA | броня, артефакты, трофеи, травмы, таланты, `itm_atp_flash`, `itm_backpack_small` | **снято**: заменены генерацией 2026-11-04 (`generated/items-b/final`) и 2026-11-05 (`generated/icon-sheets`); сторож — совпадение файлов в `Assets/Art/Icons` с этими папками |
| Музыка | `Assets/Audio/Music/*.mp3` (2) | генерация подтверждена C2PA-манифестом Google; права — как у остальных сгенерированных ассетов, замена не нужна |
| Эмбиент | `Assets/Audio/Ambience/*.mp3` (4) | **заменено 2026-11-12**: `suno/sounds` через Polza, 2 ₽ × 4 = 8 ₽, лупы 13–21 с; исходники — `generated/ambience-new/`, промпты — `generated/prompts/amb-*.txt`. Имена файлов и id те же, ссылки каталога не менялись; при желании — `Tools/AnomalyPath/Собрать каталог звуков` |

После замены `icon` в JSON не меняются (имена файлов те же). Проверка: `.prototypes/dss`,
`.prototypes/refs`, `art-archive`, `backup`, `archive` в новый репозиторий не попадают;
поиск по дереву `dss|rykzack|NLC` пуст.

## 2. Что не входит в публичный репозиторий

Уже игнорируются: `Library/`, `Temp/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`,
`.agents/`, `.dsh/`, `AGENTS.md`, `.build/`, `.dotnet/`, `.dotnet-home/`, `_probe/`,
`*.csproj` (кроме `src/**`, `tests/**` и `tools/**` — они нужны для `dotnet build`/`dotnet run`)
и `Game1.slnx`; `AnomalyPath.sln` публикуется, на него ссылается `README.md`. Дополнительно не добавлять:

- `.prototypes/` (прототипы Penpot, DDS, референсы);
- `archive/`, `art-archive/`, `backup/`;
- `generated/` — промежуточные генерации; публиковать ли `generated/prompts/`, решается после
  полировки (см. §0);
- `Builds/*.zip`, `*.apk`, `*.unitypackage`.

## 3. Новый репозиторий

Старый `akarpov2k/Game1` не переписывать: его история уже видна. Новый репозиторий создан
(2026-11-12) локально — `D:\Projects\AnomalyPathGame`, ветка `main`, `origin` —
`git@github.com:akarpov2k/AnomalyPathGame.git`, коммитов нет. На форме GitHub его заполнять
файлами было нельзя: README, лицензия и `.gitignore` уже лежат в дереве, шаблон создал бы
вторую копию.

**Перенос выполнен 2026-11-12** (источник — `d7c4e56` «полировка»): скопировано дерево
`Assets`, `Packages`, `ProjectSettings`, `src`, `tests`, `tools`, `docs` и корневые
`.gitignore`, `.vsconfig`, `Directory.Build.props`, `AnomalyPath.sln`, `LICENSE`, `NOTICE.md`,
`README.md`. Файлы не пересохранялись и не переформатировались — побайтная копия, `robocopy /E`;
`docs/` перекопирован свежим, потому что после первого переноса успел измениться.

Проверка копии: 3352 файла видны git (3353 на диске — лишний `tools/AnomalyPath.RouteViz/*.csproj`,
он под правилом `*.csproj` из `.gitignore`), 543,5 МБ; `dotnet build AnomalyPath.sln` —
0 ошибок, 0 предупреждений, `dotnet run --project tests\AnomalyPath.Core.Tests -- Assets\Data` —
1348/1348; исключённых каталогов (`.prototypes`, `archive`, `art-archive`, `backup`, `generated`,
`Game1.slnx`, `Build`, `Builds`, `Library`, `Temp`, `Logs`) в копии нет; в `Assets/Art` нет
имён с `dss`/`rykzack`/`nlc`.

**Найдено при переносе:** каталог `src/AnomalyPath.Core/Artifacts/` попадал под шаблонное
правило `artifacts/` в `.gitignore` и **никогда не версионировался** — ни в старом, ни в новом
репозитории; публичная копия Core без этих 9 файлов не собирается. В `.gitignore` добавлено
исключение `!src/AnomalyPath.Core/Artifacts/` (+`/**`) рядом с исключением `Assets/Editor/Build/`.
Эти 18 файлов (9 `.cs` и 9 `.meta`) нужно добавить в git и в старом репозитории.

**Соответствие ветке `combat` (проверено 2026-11-12):** старый репозиторий стоит на ветке
`combat` (`d7c4e56`, на 3 коммита впереди `main`, 1066 изменённых файлов), снимок снят с её
рабочего дерева. Сверка по SHA-256: 3353 файла в обоих деревьях, «только в старом» — 0,
«только в новом» — 0, расхождений по содержимому — 0. Другие ветки переносить не нужно:
`main` и `new-style` — предок снимка (`23ad94d`, 2026-11-07), `feature/unity_proj` (2026-10-21,
скелет проекта), `feature/scenes` (2026-10-26; 198 файлов, из них 117 картинок и 50 скриптов,
которых в `combat` уже нет), `ассеты` (2026-11-04; 769 файлов, 597 из них — `generated/location-catalog`).
Это перекрытая история и промежуточные генерации, которые по §2 в публикацию не идут; при
архивировании старого репозитория они останутся в нём.

Сделано 2026-11-12: файлы добавлены в индекс и зафиксированы initial-коммитом
(`AnomalyPath: initial public source snapshot`). Осталось (делает пользователь):

```powershell
cd D:\Projects\AnomalyPathGame
git log --oneline -1   # проверить коммит
git push -u origin main
```

`AnomalyPath.sln` в списке обязателен: на него ссылается `README.md` (`dotnet build AnomalyPath.sln`).
`Game1.slnx` — старое решение, оно описывает только сгенерированные `.csproj` (те игнорируются),
в публикацию не идёт.

**Открыто:** ~~`tools/AnomalyPath.RouteViz/AnomalyPath.RouteViz.csproj` под правилом `*.csproj`
не публикуется~~ — **решено 2026-11-12: публикуется.** В `.gitignore` добавлено исключение
`!tools/**/*.csproj`, проектный файл утилиты входит в снимок, `dotnet run` в этой папке работает
из клона (ссылается на `src/AnomalyPath.Core/AnomalyPath.Core.csproj`, он тоже публикуется).

Старый репозиторий: **Settings → General → Danger Zone → Archive** (или Delete). Форки и кеши
GitHub у прежней версии могут остаться — поэтому чужие ассеты из неё всё равно убирать.

## 4. Билд

1. **Player Settings → Other Settings** (уже выставлено в `ProjectSettings.asset`):
   - Scripting Backend: **IL2CPP** для Standalone (Android был IL2CPP и раньше);
   - IL2CPP Compiler Configuration: **Release**;
   - Managed Stripping Level: **Medium**; Api Compatibility Level: **.NET Standard 2.1**.
   Модуль **Windows Build Support (IL2CPP)** **установлен 2026-11-12** командой
   `unity install-modules -e 6000.6.6f1 -m windows-il2cpp --accept-eula -y` (Unity CLI из Hub,
   271 МБ загрузки / 1,12 ГБ на диске; установщик один раз запрашивает UAC). Проверка:
   в `Editor/Data/PlaybackEngines/windowsstandalonesupport/Variations` появились
   `win64_player_nondevelopment_il2cpp` (там `WindowsPlayer.exe`, `UnityPlayer.dll`,
   `UnityPlayer_Win64_player_il2cpp_x64.pdb`), `win32_*_il2cpp` и `win_arm64_*_il2cpp`.
   Ни одной Windows-сборки с IL2CPP до 2026-11-12 не было: `Build/Windows` и раздаточный zip —
   сборки на Mono (см. §7). Для IL2CPP нужен C++-тулчейн на машине: **2026-11-12 установлен
   workload «Desktop development with C++»** у Visual Studio Community 2026
   (`setup.exe modify --add Microsoft.VisualStudio.Workload.NativeDesktop --includeRecommended`,
   MSVC 14.51, Windows SDK 10.0.26100). Без него сборка падает с
   `ToolchainNotFoundException: IL2CPP C++ code builder is unable to build C++ code`.
2. Обфускация (по желанию, после IL2CPP): Obfuscar (бесплатный) или платные решения;
   без IL2CPP она бесполезна.
3. Новый проект **импортирован** в batch-mode 2026-11-12 (`Assets`, 543 МБ → `Library` 1,66 ГБ,
   `Logs/Import.log`): 0 ошибок компиляции, `dotnet build AnomalyPath.sln` там же — 0 ошибок,
   `dotnet run --project tests\AnomalyPath.Core.Tests -- Assets\Data` — 1348/1348. При импорте
   Unity перезаписал `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` (очистил
   служебный список `m_RuntimeSettings`) — файл восстановлен побайтно из источника; после
   открытия проекта в редакторе Unity перепишет его снова, это локальный шум, а не изменение.
   Ещё Unity создаёт в корне `AnomalyPathGame.slnx`; расширение `*.slnx` добавлено в `.gitignore`.
4. Сборка:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe" -batchmode -quit -projectPath . `
  -executeMethod AnomalyPath.Editor.BuildScript.BuildWindows -logFile Logs\Build.log
```

Меню: `Tools/AnomalyPath/Собрать Windows`. Сборка кладёт exe в `Build/Windows` и копирует
`Assets/Data` в `StreamingAssets`. **Сборка выполнена 2026-11-12** (`Logs/Build2.log`, версия
0.1 demo): `Build Finished, Result: Success`, `Build/Windows/AnomalyPath.exe` +
`GameAssembly.dll` (27,8 МБ, IL2CPP), `MonoBleedingEdge/` нет, данные — 205 файлов в
`Build/Windows/AnomalyPath_Data/StreamingAssets/Data`. Размер каталога 1,2 ГБ, из них
757 МБ — `AnomalyPath_BackUpThisFolder_ButDontShipItWithYourGame` (символы, не идёт в раздачу);
к раздаче ≈445 МБ (`sharedassets0.assets.resS` — 350 МБ).
5. Публикация билда — **отложено** (2026-11-12, площадка раздачи не выбрана). Когда дойдёт:
   заархивировать `Build/Windows` (без `*_BackUpThisFolder_ButDontShipItWithYourGame`) в
   `Builds/AnomalyPath-0.1-Windows.zip`, выложить архив + `sha256`. Старый архив от 2026-11-05
   в раздачу не годится: он собран на Mono и старше правок ноября. В git артефакт не кладётся:
   `Build/` и `Builds/` в `.gitignore`, GitHub отклоняет файлы больше 100 МБ.

```powershell
Get-FileHash Builds\AnomalyPath-Windows.zip -Algorithm SHA256
```

5. Unity Personal: сплэш-экран обязателен, порог бесплатной лицензии по выручке — проверять
   в актуальной редакции Unity Terms (донаты порог не повышают, но при росте выручки
   понадобится Plus/Pro).

## 5. Доказательства авторства

Публикация исходников не заменяет доказательств — они нужны, если придётся спорить.

1. **Подписанные коммиты** (SSH или GPG) и тег релиза — фиксируют автора и дату:
   `git tag -s v0.1 -m "первая публичная версия"` (или `-a` с SSH-подписью); номер тега
   согласовать с `bundleVersion` в `ProjectSettings.asset` (сейчас там `0.1`, в меню
   отображается `0.1 Alpha Demo`).
2. **Депонирование** исходников (РАО, n'RIS, IPChain и аналоги) — фиксирует дату и хеш
   конкретной версии; дешевле и быстрее регистрации.
3. **Регистрация программы для ЭВМ** в Роспатенте (ст. 1262 ГК РФ) — свидетельство,
   заявка рассматривается около двух месяцев, пошлина зависит от объёма.
4. **Товарный знак «AnomalyPath»** (ст. 1477, 1482 ГК РФ), классы 9 (ПО и игры) и 41
   (развлечения) — защищает название, которое копируют чаще кода.

## 6. Если скопировали

1. Зафиксировать факт: ссылка, дата, скриншот, `sha256` файлов — нотариальный осмотр
   страницы или архив через web.archive.org.
2. GitHub: **DMCA / Report content** — указать `LICENSE`, строку `Required Notice`
   и что использование коммерческое (нарушение п. «Noncommercial Purposes»).
3. Если копия продаётся в РФ: досудебная претензия, затем ст. 1301 ГК РФ —
   компенсация от 10 тыс. до 5 млн ₽ либо двукратная стоимость права использования.
4. Публичный репозиторий GitHub ToS разрешает форки независимо от лицензии; лицензия
   ограничивает не факт копии, а её использование — это и есть основание для жалобы.

## 7. Чек-лист перед публикацией

Состояние на 2026-11-12.

- [x] чужие ассеты заменены: фон инвентаря и иконки числятся заменёнными в `NOTICE.md`,
      в `Assets/Art` файлов из NLC/GAMMA-атласов нет (проверка по именам файлов);
- [x] эмбиенты заменены генерацией 2026-11-12 (`generated/ambience-new/`), чужих аудиофайлов нет;
- [x] `LICENSE` и `NOTICE.md` лежат в корне, `README.md` ссылается на них; перенесены в новый
      репозиторий вместе с `docs/`;
- [x] в публикуемом дереве нет `.prototypes/`, `archive/`, `art-archive/`, `backup/`, `_probe/`,
      `generated/`, `Game1.slnx` — снимок 2026-11-12 их не содержит; `src/AnomalyPath.Core/Artifacts/`
      добавлен исключением в `.gitignore` (иначе исходники Core не попадали в git); остатки
      прогонов (`Assets/InitTestScene*.unity`) и шаблонная `Assets/Scenes/SampleScene.unity`
      удалены, `Assets/InitTestScene*` добавлен в `.gitignore`;
- [x] тесты зелёные: `dotnet` 1348/1348, Unity EditMode 1/1, полный PlayMode 317 — 314 прошло,
      0 падений, 3 ручных `[Explicit]` (`Logs/TestResults.PlayMode.xpbar.xml`); битые к этому
      моменту тесты (стаб, версия меню, имя NPC, Esc в наградах, три walk-проверки) исправлены;
- [x] билд собран IL2CPP: 2026-11-12, `Logs/Build6.log` — `Build Finished, Result: Success`,
      `Build/Windows/GameAssembly.dll` есть, `MonoBleedingEdge/` нет. Локальный прогон
      `AnomalyPath.exe` (12 с): окно открылось, `Проверка данных пройдена: ошибок нет`, исключений
      нет. Первый прогон нашёл IL2CPP-дефект: `BorderlessWindow.WindowProcImpl` без
      `[MonoPInvokeCallback]` — на Mono работал, на IL2CPP падал в `Marshal.GetFunctionPointerForDelegate`;
      исправлено, предупреждение ушло. Известный шум сборки: URP пишет
      `Hidden/Universal Render Pipeline/GaussianDepthOfField ... has been stripped`, потому что
      2D-рендерер ссылается на 3D-`PostProcessData`; `Volume` в сцене Main нет, пост-обработка
      не используется, на игру не влияет. Проверка **на чистой машине** ещё не делалась — нужен
      отдельный прогон на другой машине;
- [x] интерфейс билда на 2K: найдено 2026-11-12 на прогоне при 2560×1440 — окно без рамки
      заходит низом под панель задач (клиент 2560×1440 при рабочей области 2560×1392), и полоса
      опыта с нижним рядом кнопок оказывались не видны. Размер окна больше не переставляется
      вручную (это ломало координаты мыши — «нажатия уезжали»), вместо этого `SafeAreaRoot`
      вписывает интерфейс в видимую часть экрана: `ScreenVisibleArea` считает пересечение
      клиентской области с рабочей областью монитора, разметка встаёт выше панели задач;
      полоса опыта и подсказка дополнительно выводятся поверх соседних слоёв
      (`PlayScreen.RefreshXp`). Закреплено тестом `WalkMode_XpBar_DrawsAboveLane`;
- [x] донаты не привязаны к доступу к билду: билд бесплатный (`README.md`, `NOTICE.md`);
- [ ] тег подписан, хеш билда опубликован — отложено вместе с площадкой раздачи;
- [ ] старый репозиторий архивирован или удалён.
