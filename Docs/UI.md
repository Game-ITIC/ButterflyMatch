# UI: как тут всё устроено

Шпаргалка для тех, кто правит интерфейс. Сначала найди экран в таблице, потом прочитай правила и ловушки: они короткие и экономят часы.

## Где что лежит

Префабы лежат в `Assets/Content/Prefabs/UI/`:

| Папка | Что внутри |
|---|---|
| `Common/` | То, что используется везде: `BaseButton`, `NotificationBadge` |
| `Menu/HUD/` | Постоянные элементы меню: верхняя панель, нижняя навигация, счётчики |
| `Menu/Pages/` | Страницы свайпа: остров, главная, магазин, и их карточки |
| `Menu/Popups/` | Окна меню: Daily и задания, настройки, жизни, каталог зданий |
| `Shop/` | Карточки магазина |
| `Game/HUD/` | Панели во время уровня |
| `Game/Popups/` | Окна уровня: пауза, поражение, победа, магазины |
| `Game/Tutorial/` | Обучение |
| `Core/` | Загрузка и камера |

Имена файлов в `Game/` совпадают с именами объектов в сцене и константами в коде (см. «Что нельзя переименовывать»).

### Экран → префаб → скрипт

| Экран | Префаб | Скрипт |
|---|---|---|
| Свайп-страницы меню | `Pages Strip` в `MainMenu.unity` | `UI/MenuPanels/MenuPager.cs` |
| Нижняя навигация | `Menu/HUD/HUD Bottom Nav` | `UI/MenuPanels/MenuNavPanel.cs` |
| Верхняя панель | `Menu/HUD/HUD Top Bar` | `UI/MenuPanels/MainMenuPanel.cs` |
| Страница острова | `IslandPanel` в сцене, карточка `Menu/Pages/Region Card` | `UI/MenuPanels/Islands/IslandPanel.cs`, `RegionCardView.cs`, `Presenters/IslandSelectPresenter.cs` |
| Каталог зданий | `Menu/Popups/BuildingCatalogPanel`, `Building Card` | `UI/MenuPanels/Islands/BuildingCatalogPanel.cs`, `BuildingCardView.cs` |
| Магазин | `Shop Panel` в сцене, карточки из `Shop/` | `Views/InAppView.cs`, `Views/BoosterOfferCardView.cs`, `Presenters/ShopPresenter.cs` |
| Daily и задания | `Menu/Popups/Daily And Tasks Panel`, `Daily Card`, `Daily Card Big`, `Task Card` | `UI/Tasks/TaskPanel.cs`, `TaskView.cs`, `Views/DailyCardView.cs`, `Presenters/DailyCardsPresenter.cs`, `TaskPresenter.cs` |
| Настройки и язык | `Menu/Popups/SettingsPanel`, `Settings Row` | `UI/Settings/SettingsPanel.cs`, `SettingsToggleView.cs`, `Localization/LanguagePicker.cs` |
| Жизни | `Menu/Popups/LifePopup` | `UI/LifePopup.cs` |
| Пауза | `Game/Popups/MenuPause` | `Views/GamePauseView.cs` |
| Поражение | `Game/Popups/MenuFailed` | `Views/GameOverView.cs` |
| Победа | `Game/Popups/MenuComplete` | `Views/GameCompleteView.cs`, `JuiceFresh/Scripts/GUI/Popups/MenuCompletePopup.cs` |
| Панели уровня | `Game/HUD/TopBarPanel`, `BottomPanel` | `JuiceFresh/Scripts/GUI/BoostIcon.cs` и другие |
| Туториал в уровне | `Game/Tutorial/Tutorial Overlay` в `game.unity` | `Tutorial/LevelTutorialDirector.cs` (какие шаги и когда), `TutorialOverlay.cs` (затемнение, окно, рука), `TutorialGate.cs` (что можно трогать на поле) |

Пути скриптов указаны от `Assets/Scripts/`, если не написано иначе.

## Правила

1. **Правда в префабе.** Правь префаб, а не экземпляр в сцене. В экземпляре остаётся только то, что отличается по смыслу: иконка, текст, ссылка на объект сцены. Lint считает больше 12 переопределений ошибкой.
2. **Без масштаба на контейнерах.** Размер в инспекторе должен быть настоящим. Нужно крупнее: меняй размер и шрифт, а не `Scale`. Масштаб допустим только у объектов, которые двигает анимация.
3. **Имена по роли:** `Title`, `Label`, `Icon`, `Amount`, `Window`, `Frame`, `Close Button`. Никаких `Image (3)`, `Text (TMP)` и `GameObject`. Исключения перечислены ниже.
4. **Текст не меньше 32 px** на телефоне 1080×2160, нижняя граница автоподбора 22 px. Пиксели считай через таблицу масштабов.
5. **Зона нажатия не меньше 127 px.** Кнопка мельче: добавь дочерний `Hit Area` (см. ниже), сама кнопка может остаться маленькой.
6. **Текст влезает на всех языках.** Проверяй на `de` и `ru`, у них самые длинные строки. Для меток с разной длиной перевода включай Auto Size. Ряду одинаковых карточек задавай общий размер через `UITextFit.MatchSmallest`.

### Масштаб холстов на 1080×2160

Один и тот же шрифт выглядит по-разному на разных холстах:

| Холст | Reference | Match | Масштаб | 32 px это |
|---|---|---|---|---|
| `MainMenu` → `HUD Canvas` | 1920×1080 | 0.5 | 1.06 | 30 ед. |
| `game` → `CanvasGlobal` (окна) | 1536×2048 | 0 | 0.703 | 46 ед. |
| `game` → `Level/Canvas` (панели уровня) | 1536×2048 | 0.554 | 0.88 | 37 ед. |

У меню масштаб зависит от экрана: 1.0 на 16:9, 1.118 на 20:9, 1.146 на 21:9.

## Как сделать

**Новая страница свайпа.** Добавь слот в `Pages Strip` рядом с `Island Page`, `Main Page`, `Shop Page` и вкладку в `HUD Bottom Nav`. Страница не должна выглядеть как окно: `MenuPager` сам двигает полосу и выключает холст за экраном.

**Новое окно.** Положи префаб в `Menu/Popups/` или `Game/Popups/`. Затемнение `ClosePanel` растяни на весь экран, окно назови `Window`. Для анимации открытия добавь `UIPopupOpenTweenAnimation` и задай ему `target`.

**Новая карточка.** Бери за основу карточки магазина из `Shop/`: этот стиль и нужен в меню. Корень карточки равен её видимому размеру.

**Значок уведомления.** Положи `Common/NotificationBadge` в кнопку и вызывай `NotificationBadge.Set(button, естьЧтоЗабрать)` из презентера.

**Зона нажатия.** Дочерний объект `Hit Area` с компонентом `UIHitArea` по центру кнопки. Он ничего не рисует, но ловит нажатия.

**Переводимая метка.** Добавь строку в `Assets/Content/Localization/Source/UI_strings.txt` (12 языков), запусти `Tools/Localization/Import UI strings`, повесь на метку `LocalizeStringEvent`. Текст, который задаёт код, получай через `L10n.Get(key, fallback)`. Названия из конфигов идут через `L10n.Translate(englishName)`: он ищет английский текст в таблице и сохраняет число в конце.

## Шрифт

Во всём интерфейсе один шрифт: `Assets/Content/Fonts/GameFont SDF.asset`. Сейчас он собран из Nunito Black (лицензия OFL, файл `Nunito-OFL.txt` рядом). Этот же шрифт стоит в настройках TMP по умолчанию, поэтому новая метка получает его сама. Для наклеек с обводкой есть материал `GameFont Sticker`.

**Сменить шрифт игры.** Положи новый TTF в `Assets/Content/Fonts/`, поменяй `SourceFontPath` в `Utils/Editor/GameFontBuilder.cs` и запусти `Tools/UI/Rebuild Game Font`. Префабы и сцены трогать не нужно: их метки ссылаются на GUID ассета, а он не меняется.

**Новые символы.** Шрифт статический: атлас не дополняется во время игры. Если в `UI_strings.txt` появились новые символы, а `Check tables and glyphs` про них сообщает, запусти `Rebuild Game Font` ещё раз.

**Японский, корейский, китайский** берутся из запасных шрифтов: M PLUS Rounded, Jua, ZCOOL KuaiLe. Порядок для каждого языка задаёт `Resources/LocaleFontConfig`.

**Флажок Bold у метки ничего не делает.** Шрифт и так Black, поэтому искусственное утолщение в `GameFont SDF` выключено. Другой вес делай отдельным шрифтом, а не флажком.

**Межбуквенное расстояние и высота строки** заданы в `GameFontBuilder`: `Tracking` (−2, в сотых долях em) и `AscentEm`/`DescentEm` (строка 1.21 em). Родная высота строки Nunito, 1.36 em, не помещается в метки с режимом Truncate, и текст исчезает.

**Наклейки.** У меток `Sticker Text` собственный `characterSpacing` 4, иначе обводки букв слипаются. Тень в материале `GameFont Sticker` нельзя опускать ниже `_UnderlayOffsetY` −0.40: она начинает читать соседнюю букву в атласе, и над заголовком появляется тонкая линия.

## Инструменты

| Меню | Что делает |
|---|---|
| `Tools/UI/Lint UI Prefabs and Open Scenes` | Проверяет имена, масштаб, потерянные скрипты, мелкий текст и кнопки, толстые экземпляры. Отчёт: `Logs/UiLint.txt`. Ничего не меняет |
| `Tools/UI/Bake Building Icons` | Рендерит здания из слотов островов в `Assets/Content/UI/BuildingIcons/` и назначает их в `BuildingSO`. Запускай после смены моделей или нового региона |
| `Tools/UI/Rebuild Game Font` | Пересобирает `GameFont SDF` из исходного TTF. Запускай после смены шрифта или появления новых символов |
| `Tools/Localization/Import UI strings` | Заливает `UI_strings.txt` в таблицу `UI` |

`Utils/Editor/ShopPrefabBuilder.cs` собирает карточки магазина из кода и ищет дочерние объекты по именам. Переименовал что-то в карточке: поправь и его.

Для правок префабов скриптом есть `.ai-game-dev/snippets/UiEdit.cs`. В нём `BakeScale` переносит масштаб в настоящие размеры.

## Ловушки: префаб выглядит иначе, чем игра

- **Кнопки с `button_controller`** (`AdsButton`, `ButtonContinue`, `Close`): состояние `Normal` держит `Scale` 1.2. В игре такие кнопки на 20% больше, чем в префабе. Масштаб в инспекторе им менять бесполезно, аниматор его перезапишет.
- **`UIPopupOpenTweenAnimation`** в конце открытия ставит окну `endScale`, обычно 1. Значение `Scale` в префабе окна на результат не влияет.
- **Атлас `UIAtlas` без Tight Packing, и это важно.** С ним в прямоугольник звезды попадал кусок соседнего спрайта: тот самый белый угол. `Image` рисует весь прямоугольник спрайта.
- **`IslandPanel` и `Shop Panel`** распакованы в `MainMenu.unity`, правь их в сцене. Одноимённые префабы устарели, поэтому находки `UiLint` в них не про то, что видит игрок.
- **Ряды `Hlg*` в магазине** не настоящие. `ShopLayout.Clear` удаляет их при запуске и строит ряды из конфига, а внешний вид задают префабы в `Shop/`.
- **Модели `.blend`** импортируются только там, где Blender связан с этим типом файлов в Windows. Иначе импорт падает с ошибкой «Blender could not be found», и у детали нет меша: остров Корея остаётся без пагоды, моста и фонарей, хотя каталог пишет «Построено». При установке Blender через Steam такой связи нет.
- **`Daily Button`** презентер ищет по имени.
- **В редакторе баннер рекламы и межстраничная реклама ненастоящие.** Заглушка межстраничной рекламы не закрывается, поэтому экран результата после неё придётся перезапускать через Play. Положение настоящего баннера проверяй на устройстве.

## Что нельзя переименовывать

Эти имена ищет код. Хочешь переименовать: сначала поправь код.

| Кто ищет | Имена |
|---|---|
| `GameSceneUi` | `CanvasGlobal`, `Level/Canvas`, `MenuComplete`, `MenuFailed`, `MenuPause`, `PreFailed`, `BoostShop`, `BoostInfo`, `GemsShop`, `NoMoreMatches`, `PreCompleteBanner` |
| `InitScript` | `MenuPlay`, `GemsShop`, `LiveShop`, `Levels` |
| Панели уровня | `Canvas/Panel/TopBarPanel`, `Canvas/PrePlay`, `Panel/Panel` |
| `BoostIcon` | `Indicator/Count/Count`, `Indicator/Count/Check` |
| `BoostShop` | `Image/Buttons/BuyButton/Count`, `.../Price` |
| `MenuCompletePopup` | `Window/Star{n}/Star` |
| `TaskPanel` | `DailyRewardsPanel` |
| `DailyCardView` | `ItemImage`, `Amount`, `DayLabel`, `GlowBGImage` |
| `DailyCardsPresenter` | `Daily Button` |
| `GameOverState` | `Score`, `ButtonReplay`, `ButtonQuit` |
| `ContinueOffers` | `AdsButton`, `ButtonContinue`, `Cost`, `Icon` |
| `GameScreenButtons` | `Price` |
| `AudioToggles` | `Music`, `Window/Sound/SoundOff` и `Window/Music/MusicOff` в `MenuPause`, `Image/Sound/SoundOff` и `Image/Music/MusicOff` в `Settings` |
| `CollectableTargetUIBuilder` | `Image` и счётчики в `TargetIngr` |
| `UIPreCompleteBannerTweenAnimation` | `Image`, `Image (1)`, `Image (5)`, `Image (6)` |
| `ShopPrefabBuilder` | `Title`, `Icon`, `Amount`, `Rewards`, `Badge`, `Bonus`, `Button`, `Label` |
| `UIPopupOpenTweenAnimation` | `Image`, если `target` не задан |

Перед переименованием проверь поиском: `Find("`, `GameObject.Find`, UnityEvent и пути в анимациях (`AnimationUtility.GetCurveBindings`).

## Как проверять

1. Запускай Play со сцены `Bootstrap`, Game View 1080×2160.
2. Перед Play сохрани прогресс: `reg export "HKCU\Software\Unity\UnityEditor\ITIC Games\ButterflyMatch" файл /y`. После Stop верни его через `reg import`.
3. Проверь 16:9 и 20:9 с вырезом, языки `de` и `ru`.
4. Запусти Lint. Сравнивай отчёт с прошлым: новых замечаний быть не должно.
