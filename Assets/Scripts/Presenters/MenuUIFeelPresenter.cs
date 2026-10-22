using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Itic.Services;
using UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Presenters
{
    /// <summary>
    /// Main menu UI feel: press feedback on HUD buttons, a breathing Play button and a short HUD
    /// intro that starts when the loading screen goes away.
    /// </summary>
    public class MenuUIFeelPresenter : IInitializable, IDisposable
    {
        private const float LoadingScreenWaitTimeout = 5f;
        private const float IntroDuration = 0.45f;
        private const float IntroFadeDuration = 0.3f;

        private readonly MainMenuPanel _mainMenu;
        private readonly ScreenService _screenService;
        private readonly CancellationTokenSource _cancellation = new();

        public MenuUIFeelPresenter(MainMenuPanel mainMenu, IObjectResolver resolver)
        {
            _mainMenu = mainMenu;
            _screenService = resolver.ResolveOrDefault<ScreenService>();
        }

        public void Initialize()
        {
            if(_mainMenu == null)
            {
                return;
            }

            UIButtonFeel.InstallInChildren(_mainMenu);

            var actionPanel = _mainMenu.MenuActionPanel;
            var playButton = actionPanel != null && actionPanel.PlayButton != null ? actionPanel.PlayButton.Button : null;
            if(playButton != null)
            {
                UIFeel.SetBreathing(playButton, true, 0.035f, 1.6f);
            }

            PlayIntroAsync(_cancellation.Token).Forget();
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
        }

        private async UniTaskVoid PlayIntroAsync(CancellationToken token)
        {
            var topBar = PrepareSlide(_mainMenu.TopBarPanel, new Vector2(0f, 160f));
            var navBar = PrepareSlide(_mainMenu.MenuNavPanel, new Vector2(0f, -160f));
            var actionPanel = PrepareSlide(_mainMenu.MenuActionPanel, new Vector2(0f, -120f));

            try
            {
                var deadline = Time.realtimeSinceStartup + LoadingScreenWaitTimeout;
                while(_screenService != null
                      && _screenService.IsLoadingScreenVisible
                      && Time.realtimeSinceStartup < deadline)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch(OperationCanceledException)
            {
                return;
            }

            PlaySlide(topBar, 0f);
            PlaySlide(navBar, 0.06f);
            PlaySlide(actionPanel, 0.12f);
            PopActionButtons();
        }

        private void PopActionButtons()
        {
            var actionPanel = _mainMenu.MenuActionPanel;
            if(actionPanel == null)
            {
                return;
            }

            if(actionPanel.BuildButton != null)
            {
                UIFeel.PopIn(actionPanel.BuildButton, 0.18f, 0.6f, 0.4f);
            }

            if(actionPanel.PlayButton != null && actionPanel.PlayButton != actionPanel.BuildButton)
            {
                UIFeel.PopIn(actionPanel.PlayButton, 0.24f, 0.6f, 0.4f);
            }
        }

        private static SlideState PrepareSlide(Component target, Vector2 offset)
        {
            if(target == null || !(target.transform is RectTransform rectTransform))
            {
                return default;
            }

            if(!target.TryGetComponent(out CanvasGroup group))
            {
                group = target.gameObject.AddComponent<CanvasGroup>();
            }

            var state = new SlideState(rectTransform, group, rectTransform.anchoredPosition);
            rectTransform.DOKill();
            group.DOKill();
            rectTransform.anchoredPosition = state.RestPosition + offset;
            group.alpha = 0f;
            return state;
        }

        private static void PlaySlide(SlideState state, float delay)
        {
            if(state.Rect == null)
            {
                return;
            }

            state.Rect.DOAnchorPos(state.RestPosition, IntroDuration)
                .SetDelay(delay)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(state.Rect.gameObject, LinkBehaviour.CompleteAndKillOnDisable);

            state.Group.DOFade(1f, IntroFadeDuration)
                .SetDelay(delay)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetLink(state.Group.gameObject, LinkBehaviour.CompleteAndKillOnDisable);
        }

        private readonly struct SlideState
        {
            public readonly RectTransform Rect;
            public readonly CanvasGroup Group;
            public readonly Vector2 RestPosition;

            public SlideState(RectTransform rect, CanvasGroup group, Vector2 restPosition)
            {
                Rect = rect;
                Group = group;
                RestPosition = restPosition;
            }
        }
    }
}
