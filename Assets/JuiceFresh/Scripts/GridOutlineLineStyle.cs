using UnityEngine;

/// <summary>How a board outline LineRenderer is drawn: local-space closed loop above the cells, transparent material.</summary>
public static class GridOutlineLineStyle
{
    private const int SortingOrder = 50;
    private const int TransparentQueue = 3000;
    private const float MinWidth = 0.24f;

    /// <summary>
    /// Sets up the line; returns the per-line transparent material copy (the caller owns and destroys it),
    /// or null when the prefab line has no material.
    /// </summary>
    public static Material Apply(LineRenderer lineRenderer, int pointCount)
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = pointCount;
        lineRenderer.numCornerVertices = Mathf.Max(lineRenderer.numCornerVertices, 6);
        lineRenderer.numCapVertices = Mathf.Max(lineRenderer.numCapVertices, 4);
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.sortingLayerName = "Default";
        lineRenderer.sortingOrder = SortingOrder;
        lineRenderer.widthMultiplier = Mathf.Max(lineRenderer.widthMultiplier, MinWidth);

        Material sharedMaterial = lineRenderer.sharedMaterial;
        if(sharedMaterial == null)
            return null;

        Material runtimeMaterial = CreateTransparentCopy(sharedMaterial);
        lineRenderer.sharedMaterial = runtimeMaterial;
        return runtimeMaterial;
    }

    // URP Lit/Unlit switched to the transparent surface with alpha blending and no depth write.
    private static Material CreateTransparentCopy(Material source)
    {
        Material material = new Material(source)
        {
            name = source.name + " (Grid Outline Instance)",
            renderQueue = TransparentQueue
        };

        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        if(material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if(material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if(material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", 5f);
        if(material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", 10f);
        if(material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        return material;
    }
}
