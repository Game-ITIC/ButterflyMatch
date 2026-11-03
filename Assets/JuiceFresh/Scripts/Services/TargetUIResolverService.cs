using UnityEngine;

namespace JuiceFresh.Scripts
{
    public class TargetUIResolverService
    {
        private readonly LevelManager _levelManager;

        public TargetUIResolverService(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public void Resolve()
        {
            if (_levelManager.Level == null)
                return;

            Transform topBarTransform = _levelManager.Level.transform.Find("Canvas/Panel/TopBarPanel");
            if (topBarTransform != null)
            {
                // Goals live in their own HUD card (TopBarImage/GoalsCard/Mission); the old path stays as a fallback.
                AssignIfMissing(ref _levelManager.ingrObject, topBarTransform, "TopBarImage/GoalsCard/Mission/TargetIngr", "Mission/TargetIngr", "TargetIngr");
                AssignIfMissing(ref _levelManager.blocksObject, topBarTransform, "TopBarImage/GoalsCard/Mission/TargetBlocks", "Mission/TargetBlocks", "TargetBlocks");
                AssignIfMissing(ref _levelManager.scoreTargetObject, topBarTransform, "TopBarImage/GoalsCard/Mission/TargetScore", "Mission/TargetScore", "TargetScore");
                AssignIfMissing(ref _levelManager.cageTargetObject, topBarTransform, "TopBarImage/GoalsCard/Mission/TargetCages", "Mission/TargetCages", "TargetCages");
                AssignIfMissing(ref _levelManager.bombTargetObject, topBarTransform, "TopBarImage/GoalsCard/Mission/TargetBombs", "Mission/TargetBombs", "TargetBombs");
            }

            Transform prePlayTransform = _levelManager.Level.transform.Find("Canvas/PrePlay");
            if (prePlayTransform != null && prePlayTransform.TryGetComponent(out PrePlay prePlay))
            {
                if (_levelManager.ingrObject == null)
                    _levelManager.ingrObject = prePlay.ingrObject;
                if (_levelManager.blocksObject == null)
                    _levelManager.blocksObject = prePlay.blocksObject;
                if (_levelManager.scoreTargetObject == null)
                    _levelManager.scoreTargetObject = prePlay.scoreTargetObject;
                if (_levelManager.cageTargetObject == null)
                    _levelManager.cageTargetObject = prePlay.cage;
                if (_levelManager.bombTargetObject == null)
                    _levelManager.bombTargetObject = prePlay.bomb;
            }
        }

        static void AssignIfMissing(ref GameObject target, Transform root, params string[] paths)
        {
            if (target != null)
                return;

            foreach (string path in paths)
            {
                Transform child = root.Find(path);
                if (child == null)
                    continue;

                target = child.gameObject;
                return;
            }
        }
    }
}
