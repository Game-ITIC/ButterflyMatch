using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Invisible raycast target. Put it on a child of a small button and size it past the art,
    /// so the tap zone reaches the minimum (see Docs/UI.md) without scaling the art or adding an alpha-0 Image.
    /// On a tab root it also serves as the tab's whole clickable surface.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIHitArea : Graphic
    {
        public override void SetMaterialDirty()
        {
        }

        public override void SetVerticesDirty()
        {
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
