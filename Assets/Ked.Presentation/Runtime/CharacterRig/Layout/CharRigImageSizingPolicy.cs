using UnityEngine;
using UnityEngine.UI;

public enum CharRigImageSizingMode
{
    // No sizing. Leave RectTransform as-is (sprite swap only).
    KeepLayout = 0,

    // Fit by parent height and preserve sprite aspect ratio.
    // - Sets sizeDelta.y = 0
    // - Does not touch anchors (may adjust pivot.x for left/center/right alignment)
    HeightFitPreserveAspect = 1,

    // Set RectTransform to the sprite's native size (pixelsPerUnit applied).
    // - Writes sizeDelta only
    // - Does not modify anchors / pivot / anchoredPosition
    NativeSizeNoReanchor = 2,
}

public static class CharRigImageSizingPolicy
{
    public enum HorizontalAlign { Left, Center, Right }
    
    public static void Apply(Image image, Sprite sprite, CharRigImageSizingMode mode, HorizontalAlign align = HorizontalAlign.Center)
    {
        if (image == null || sprite == null)
            return;

        image.preserveAspect = true;

        switch (mode)
        {
            case CharRigImageSizingMode.KeepLayout:
                break;

            case CharRigImageSizingMode.HeightFitPreserveAspect:
                RectTransform parent = image.rectTransform.parent as RectTransform;
                ApplyHeightFitPreserveAspect(image.rectTransform, parent, sprite, align);
                break;

            case CharRigImageSizingMode.NativeSizeNoReanchor:
                ApplyNativeSizeNoReanchor(image.rectTransform, sprite);
                break;
        }
    }
    
    private static void ApplyHeightFitPreserveAspect(RectTransform imageRect, RectTransform parent, Sprite sprite, HorizontalAlign align)
    {
        if (parent == null)
            return;
        
        float parentHeight = parent.rect.height;
        float aspect = sprite.rect.width / sprite.rect.height;
        
        float targetWidth = parentHeight * aspect;

        Vector2 sizeDelta = imageRect.sizeDelta;
        sizeDelta.x = targetWidth;
        sizeDelta.y = 0f;
        imageRect.sizeDelta = sizeDelta;

        Vector2 pivot = imageRect.pivot;
        Vector2 pos = imageRect.anchoredPosition;

        switch (align)
        {
            case HorizontalAlign.Left:
                pivot.x = 0f;
                pos.x = 0f;
                break;
            case HorizontalAlign.Right:
                pivot.x = 1f;
                pos.x = 0f;
                break;
            case HorizontalAlign.Center:
                pivot.x = 0.5f;
                pos.x = 0f;
                break;
        }

        imageRect.pivot = pivot;
        imageRect.anchoredPosition = pos;
    }

    private static void ApplyNativeSizeNoReanchor(RectTransform imgRt, Sprite sprite)
    {
        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 native = new Vector2(sprite.rect.width / pixelsPerUnit, sprite.rect.height / pixelsPerUnit);

        imgRt.sizeDelta = native;
    }
}