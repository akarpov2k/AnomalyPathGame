# Установщик AnomalyPath (setup.exe)

Один файл `Builds/AnomalyPath-Setup.exe` (~42 МБ) со встроенной игрой, постером на фоне
и полосой прогресса. Собран без сторонних инсталляторов (Inno Setup/NSIS в этой среде нет):
обычное WinForms-приложение на .NET Framework 4.8, который есть в Windows 10/11 по умолчанию.

## Состав

| Файл | Что делает |
| --- | --- |
| `build-setup.ps1` | Собирает поставку: копия `Build\Windows` → `Builds\AnomalyPath-Windows` (+`README.txt`), архив `Builds\AnomalyPath-Windows.zip`, компиляция `Uninstall.exe` и `AnomalyPath-Setup.exe`. |
| `make-icon.py` | Иконка `generated/installer/icon.ico` из постера (Pillow). |
| `app.manifest` | Манифест обоих exe: `asInvoker` (без UAC), PerMonitorV2 DPI. |
| `display-name.txt` | Название игры для окна установки и «Программ и компонентов». |
| `README.install.txt` | README, который попадает в папку установленной игры. |
| `Core/` | Общее для установщика и удалялки: тема, постер, кнопка, ярлыки, запись в реестре, ресурсы. |
| `Installer/` | `SetupForm` (окно с постером и полосой прогресса), `InstallerEngine` (распаковка), `SetupProgram` (режимы запуска). |
| `Uninstaller/` | `UninstallForm`, `DeletionEngine` (уборка), `UninstallProgram` (копия в `%TEMP%`, чтобы удалить папку целиком). |

Иконка, постер и архив вшиты в exe как ресурсы (`/resource` у Roslyn), архив сжимается
ZIP-ом оптимально: 42,2 МБ установщик против 108,4 МБ распакованной игры.

## Сборка

Сначала билд Unity, потом поставка:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath . -executeMethod AnomalyPath.Editor.BuildScript.BuildWindows -logFile Logs\Build.Windows.log

powershell -NoProfile -ExecutionPolicy Bypass -File tools\installer\build-setup.ps1
```

Быстрая пересборка без копирования игры и пересборки архива (архив берётся готовый):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\installer\build-setup.ps1 -SkipPayload
```

Компилятор — Roslyn из .NET SDK против reference assemblies .NET Framework 4.8
(`csc` из `C:\Windows\Microsoft.NET\Framework64` — это C# 5, современный код он не примет).
Поэтому скрипт сам находит `dotnet\sdk\*\Roslyn\bincore\csc.dll`.

Версия и издатель берутся из `ProjectSettings/ProjectSettings.asset`
(`bundleVersion`, `companyName`), название — из `display-name.txt`.

## Что делает setup.exe

1. Окно 600×894: постер на весь фон, внизу панель с полем папки, «Обзор…»,
   переключателем ярлыка на рабочем столе и кнопками «Установить»/«Выход».
   Папка по умолчанию — `%LocalAppData%\Programs\AnomalyPath`, можно указать любую
   (запись в `Program Files` без прав администратора не удастся — установщик скажет об этом).
2. Распаковка архива в фоне: полоса прогресса, проценты, имя текущего файла, счётчик.
   «Отмена» прерывает и убирает созданную папку.
3. `Uninstall.exe`, ярлыки (меню «Пуск», рабочий стол — по переключателю), запись в
   `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\AnomalyPath`.
4. Завершение: путь установки, переключатель «Запустить игру», «Готово».

Ошибки распаковки показываются на панели; недоступные ярлык/реестр — предупреждением
(установка при этом считается состоявшейся).

## Режимы командной строки

| Ключ | Смысл |
| --- | --- |
| `/silent /dir <папка> [/noshortcut] [/log <файл>]` | Установка без окна; в лог пишется `installDir`, `files`, `warning`, `ok`/`error`. |
| `/shot <png> [/phase welcome\|installing\|done] [/progress 0..100]` | Снимок окна для проверки вёрстки (`Logs/Screenshots/setup-*.png`). |
| `/selftest` | Проверка встроенных ресурсов без окна, отчёт в `%TEMP%\AnomalyPath-setup-selftest.log`. |

`Uninstall.exe /silent` удаляет игру без окна (окно подтверждения при обычном запуске),
`Uninstall.exe /run /dir <папка>` — служебный режим копии из `%TEMP%`.

## Осторожно: антивирус

Первый вариант удалялки Defender определил как `Trojan:Win32/Bearfoos.A!ml` — из-за связки
«маленький exe с большим вшитым ресурсом + `cmd /c ... del` для самоудаления». Самоудаление
через `cmd` убрано: копия удалялки остаётся в `%TEMP%` (её убирает следующий запуск
`Uninstall.exe`), зато `setup.exe` и `Uninstall.exe` больше не ловятся эвристикой.
При доработке удалялки **не возвращайте** запуск `cmd`/`del` и не добавляйте самоудаление:
это снова поднимет ложное срабатывание.

## Проверка

```powershell
# установка во временную папку и лог
& Builds\AnomalyPath-Setup.exe /silent /dir .build\installer\test-install /log .build\installer\install.log
# смоук игры из установленной папки
& .build\installer\test-install\AnomalyPath.exe -batchmode -nographics -logFile Logs\smoke.log
# удаление
& .build\installer\test-install\Uninstall.exe /silent
```

Скриншоты окна: `setup-welcome.png`, `setup-installing.png`, `setup-done.png` в `Logs/Screenshots/`.
Коды ошибок `HKEY_CURRENT_USER\...\Uninstall` и запись ярлыков в `%APPDATA%` в песочнице агента
недоступны — проверяются только предупреждением, на обычной машине работают.
