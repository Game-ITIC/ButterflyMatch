# Dead-code audit (2026-11-05, branch refactor/solid; section 1 is already deleted in cee8e5b9)

Scope: 397 `.cs` files under `Assets/Scripts` and `Assets/JuiceFresh/Scripts` (Editor folders excluded).
71 look dead. The other 326 are alive.

## Method

The audit is done by a script, `deadcode_audit.js` in the scratchpad. Raw results are in `deadcode.json`.

**Asset reachability.** The script follows GUID references transitively through YAML assets: scenes, prefabs, assets, materials, controllers and so on. It starts from three sets of roots:
- enabled build scenes that are not under `OLD/`: Bootstrap, Services, JuiceFresh/main, game, MainMenu;
- all files under `Resources/` folders (377 files);
- Addressables group entries, with folders expanded (25 files).

`PlayerSettings.preloadedAssets` is empty.

**Code references.** The script matches the file's top-level type names, plus extension-method names, as identifiers in any other `.cs` file. Comments and strings are stripped first.

**Fixpoint.** A file stays alive if any of these holds:
- it is referenced from a reachable asset;
- it boots itself (`RuntimeInitializeOnLoadMethod` / `InitializeOnLoad`);
- a non-Editor file that is itself alive references it.

So a reference from dead code, or from Editor-only code, does not keep a file alive.

**Classification.** Usage that comes only from enabled `OLD/*` scenes (Park, Islands, AfricaRegion) is reported as "OLD-only". Usage from scenes outside the build list is reported separately.

**Caveats.**
- The check is conservative toward "alive": a type name that also appears as an unrelated identifier keeps the file alive.
- Reflection and string-based `AddComponent` / `GetType` are not detected. I found none in the dead list.
- Everything below should be confirmed in Unity (missing-script check) before deleting.

## 1. Fully commented-out files (27): zero compiled code

These are the dead integrations. Each file is entirely `//`-commented, including its `#if` guards (`//#if FACEBOOK`, `//#if PLAYFAB`, `//#if GAMESPARKS`, `//#if UNITY_INAPPS`, `//#if GOOGLE_MOBILE_ADS`).

The define symbols are not on in any case. The Android defines are: `VS_ATT; gameanalytics_*; LEVELPLAY_DEPENDENCIES_INSTALLED; GLEY_IAP_GOOGLEPLAY; ODIN_*; DOTWEEN; GRIFFIN*; POSEIDON*; JUPITER; TEXTURE_GRAPH; TG_SEARCHER; MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED; URP_COMPATIBILITY_MODE; SORTIFY; UNITY_MCP_*`. None of FACEBOOK, PLAYFAB, GAMESPARKS, UNITY_INAPPS or GOOGLE_MOBILE_ADS is among them.

| File | Note |
|---|---|
| JuiceFresh/Scripts/Integrations/FacebookManager.cs | |
| JuiceFresh/Scripts/Integrations/FBTest.cs | |
| JuiceFresh/Scripts/Integrations/AdsController.cs | |
| JuiceFresh/Scripts/Integrations/GameSparksIntegration.cs | |
| JuiceFresh/Scripts/Integrations/UserManager.cs | |
| JuiceFresh/Scripts/Integrations/UnityInAppsIntegration.cs | missing script in `JuiceFresh/Scenes/game_test.unity` |
| JuiceFresh/Scripts/Integrations/Network/NetworkManager.cs | missing script in `game_test.unity` |
| JuiceFresh/Scripts/Integrations/Network/NetworkCurrencyManager.cs, NetworkDataManager.cs, NetworkFriendsManager.cs | |
| JuiceFresh/Scripts/Integrations/Network/IFriendsManager.cs, ILoginManager.cs | |
| JuiceFresh/Scripts/Integrations/Network/Gamesparks/GamesparksCurrencyManager.cs, GamesparksDataManager.cs, GameSparksFriendsManager.cs, GamesparksLogin.cs | |
| JuiceFresh/Scripts/Integrations/PlayFab/PlayFabCurrencyManager.cs, PlayFabDataManager.cs, PlayFabFriendsManager.cs, PlayFabManager.cs | |
| JuiceFresh/Scripts/Leadboard/LeadboardManager.cs | missing script in `game_test.unity` |
| JuiceFresh/Scripts/System/RewAdmobManager.cs | missing script in `game_test.unity` |
| JuiceFresh/Scripts/System/AdsEvents.cs, InternetChecker.cs | the live `InternetChecker` is a different type elsewhere |
| JuiceFresh/Scripts/GUI/Avatar/AvatarManager.cs, IAvatarLoader.cs | |
| JuiceFresh/Scripts/GUI/Avatar/PlayerAvatar.cs | missing script in `JuiceFresh/SmartLevelsMap/Demo/demo.unity` |

The surviving interfaces are listed in section 4.

## 2. OLD-only (12): used only by enabled `Assets/_Scenes/OLD/*` scenes

| File | Used by |
|---|---|
| JuiceFresh/Scripts/ButtonManager.cs | OLD/Park |
| JuiceFresh/Scripts/StarDisplay.cs | ButtonManager |
| Scripts/Scopes/IslandLifetimeScope.cs | OLD/Islands |
| Scripts/Initializers/IslandInitializer.cs | IslandLifetimeScope |
| Scripts/Providers/CameraProvider.cs | OLD scene and IslandInitializer |
| Scripts/Providers/IslandProvider.cs | OLD scene and IslandInitializer |
| Scripts/Views/IslandView.cs | OLD scene |
| Scripts/Monobehaviours/Animations/CameraTransition.cs | OLD scene and IslandInitializer |
| Scripts/Presenters/DailyRewardsPresenter.cs | registered only in IslandLifetimeScope; the live menu uses DailyCardsPresenter |
| Scripts/Views/ShopItemView.cs | OLD scene |
| Scripts/Monobehaviours/Buildings/ShopItem.cs | ShopItemView |
| Scripts/Meta/Quests/Views/DailyQuestView.cs | OLD scene and the dead DailyQuestPresenter |

### Build-settings findings

**OLD scenes ship with the game.** Park, Islands and AfricaRegion are `enabled: 1` in EditorBuildSettings, so they are built into the APK.

**A live button loads OLD/Park.** `Assets/_Scenes/game.unity` has a Button whose persistent onClick calls `GUIEvents.ParkSceneLoader`, which runs `SceneManager.LoadScene("Park")`. Before taking OLD scenes out of the build, check in Unity whether that button is reachable or hidden. Removing the scene would make the button throw.

**Scene loads by build index point at the wrong scenes.** `SceneLoader` loads some scenes by index:
- `LoadMapAsync` → index 5, which is OLD/Islands;
- `LoadAsiaRegionAsync` → index 6, which is OLD/AfricaRegion, because AsiaRegion is disabled and enabled indices shift;
- `LoadEgyptRegionAsync` → index 7, which is MainMenu.

Only the dead IslandInitializer calls these, through `LoadRegionScene`, as does `LoadLastSceneAsync`. The live calls are `LoadGameAsync()` (index 3, game) and `LoadMenuAsync()` (by name). Removing OLD scenes from the build keeps `game` at index 3, so it is safe.

**`AnimationManager.BackToMapWithoutDie` loads "RegionSandbox".** That scene is not in build settings. Nothing calls the method, from code or from any persistent onClick in a scene or prefab. It is dead, but it would throw if anything wired it up.

## 3. Used only by scenes outside the build (10)

| File | Scene |
|---|---|
| Scripts/Monobehaviours/Animations/ScaleAnimation.cs | vendor demo `Plugins/ThirdParty/Uber Stylized Water/Demo/Uber Stylized Water.unity` |
| Scripts/Monobehaviours/Animations/ScaleEffectGUI.cs | same vendor demo |
| Scripts/Monobehaviours/Animations/FloatingIslandTouchRotate.cs | same vendor demo |
| Scripts/Monobehaviours/Animations/FloatingIslandAnimation.cs | same vendor demo |
| Scripts/Monobehaviours/Animations/ClickBreathingIdleLoop.cs | same vendor demo; also referenced by the dead EnvironmentAppearSequencer and ScaleAnimation |
| JuiceFresh/Scripts/GUI/Background.cs | `JuiceFresh/Scenes/game_test.unity`; its other ref is the Editor-only OdinLocalizationGUI |
| JuiceFresh/Scripts/GUI/GameFieldAnim.cs | `game_test.unity` |
| JuiceFresh/Scripts/PrivacyPolicy.cs | `JuiceFresh/Scenes/main_test.unity` |
| Scripts/Testing/Test.cs | `OLD/AnimationSandbox.unity`, `OLD/LevelPlayAdsTest.unity` (both not in build) |
| Scripts/UnityPurchasing/IAPTest.cs | `OLD/IAPTest.unity` (not in build) |

## 4. Not referenced anywhere (19 + 3)

These have no scene or asset use and no code reference.

| File | Note |
|---|---|
| JuiceFresh/Scripts/Animation/Drawing.cs | |
| JuiceFresh/Scripts/GUI/MovesLabel.cs | only a string in the Editor L10nEditorTools |
| JuiceFresh/Scripts/GUI/Offlineleadboard.cs | |
| JuiceFresh/Scripts/GUI/SelectLevels.cs | |
| JuiceFresh/Scripts/GUI/TargetHolder.cs | |
| JuiceFresh/Scripts/System/WaitWhile.cs | |
| JuiceFresh/Scripts/Integrations/Network/ICurrencyManager.cs, IDataManager.cs, IManager.cs | interfaces of the commented-out network layer |
| Scripts/Configs/CurrencyIconsSO.cs | `Content/Configs/CurrencyIconsSO.asset` exists but nothing reachable references it |
| Scripts/Extensions/ZLinq/ZLinqExtensions.cs | the `AsEnumerable(this ValueEnumerable…)` extension is not used anywhere |
| Scripts/Initializers/AsiaInitializer.cs | its scope is AsiaLifetimeScope |
| Scripts/Scopes/Country/AsiaLifetimeScope.cs | not even in the disabled AsiaRegion.unity |
| Scripts/Scopes/ProjectLifetimeScope.cs | `Content/Configs/ProjectLifetimeScope.prefab` is referenced by nothing; no VContainerSettings asset, preloadedAssets is empty |
| Scripts/Meta/Quests/Presenters/DailyQuestPresenter.cs | |
| Scripts/Meta/Quests/Models/QuestObjective.cs | |
| Scripts/Monobehaviours/Animations/EnvironmentAppearSequencer.cs | |
| Scripts/Monobehaviours/SpawnAnimationScript.cs | |
| Scripts/Views/IslandMenuView.cs | only in commented-out lines of IslandInitializer |

These three are referenced only by dead code:

| File | Referenced by |
|---|---|
| Scripts/Configs/Butterflies.cs | QuestObjective |
| Scripts/Meta/Quests/Models/ReactiveDailyQuest.cs | DailyQuestPresenter |
| Scripts/Test.cs | the other Test.cs; possibly a type-name clash, check before deleting |

## Already removed or known

- MainMenuPanelController is already deleted by you.
- ButtonManager and StarDisplay are in section 2. ScaleAnimation, EnvironmentAppearSequencer, ScaleEffectGUI and FloatingIslandTouchRotate are in sections 3 and 4.

## Suggested safe order, if the user approves deletions

1. Section 1: zero compiled code. First remove the missing-script components from `game_test.unity`, `main_test.unity` and `SmartLevelsMap/Demo/demo.unity`, or delete those test scenes.
2. Sections 3 and 4 together with their demo/test scenes.
3. Section 2 only after deciding about the `ParkSceneLoader` button in game.unity and taking OLD scenes out of the build.
4. Then run `graphify update .` and refresh Unity.
