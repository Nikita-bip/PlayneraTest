using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HandView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private RectTransform handRoot;

    [Header("Contact Points")]
    [SerializeField] private RectTransform handCenterPoint;
    [SerializeField] private RectTransform brushTipContactPoint;
    [SerializeField] private RectTransform blushTipContactPoint;
    [SerializeField] private RectTransform lipstickTipContactPoint;

    [Header("Tool Visuals")]
    [SerializeField] private GameObject creamVisual;
    [SerializeField] private GameObject brushVisual;
    [SerializeField] private Image brushTip;

    [SerializeField] private GameObject lipstickVisual;
    [SerializeField] private Image lipstickVisualImage;
    [SerializeField] private Image lipstickHead;

    [SerializeField] private GameObject blushVisual;
    [SerializeField] private Image blushPreview;

    [Header("Drag")]
    [SerializeField] private float dragSmooth = 20f;

    private bool _isPlayerControl;
    private Vector2 _dragTarget;

    public Vector2 CurrentPosition => handRoot != null ? handRoot.anchoredPosition : Vector2.zero;

    private void Update()
    {
        if (!_isPlayerControl || handRoot == null)
            return;

        float t = 1f - Mathf.Exp(-dragSmooth * Time.unscaledDeltaTime);
        handRoot.anchoredPosition = Vector2.Lerp(handRoot.anchoredPosition, _dragTarget, t);
    }

    public void SnapTo(Vector2 anchoredPosition)
    {
        if (handRoot == null)
            return;

        _isPlayerControl = false;
        handRoot.anchoredPosition = anchoredPosition;
    }

    public void SetBlushPreviewColor(Color color)
    {
        SetImageColor(blushPreview, color);
    }

    public void BeginPlayerControl(Vector2 startTarget)
    {
        _isPlayerControl = true;
        _dragTarget = startTarget;
    }

    public void UpdateDrag(Vector2 anchoredPosition)
    {
        _dragTarget = anchoredPosition;
    }

    public void EndPlayerControl()
    {
        _isPlayerControl = false;
    }

    public void ShowTool(MakeupToolType toolType, Color previewColor, Sprite heldToolSprite = null)
    {
        HideAll();

        switch (toolType)
        {
            case MakeupToolType.Cream:
                if (creamVisual != null)
                    creamVisual.SetActive(true);
                break;

            case MakeupToolType.Eyeshadow:
                if (brushVisual != null)
                    brushVisual.SetActive(true);

                SetImageColor(brushTip, previewColor);
                break;

            case MakeupToolType.Lipstick:
                if (lipstickVisual != null)
                    lipstickVisual.SetActive(true);

                if (lipstickVisualImage != null && heldToolSprite != null)
                    lipstickVisualImage.sprite = heldToolSprite;

                if (lipstickHead != null)
                    lipstickHead.gameObject.SetActive(heldToolSprite == null);

                if (heldToolSprite == null)
                    SetImageColor(lipstickHead, previewColor);

                break;

            case MakeupToolType.Blush:
                if (blushVisual != null)
                    blushVisual.SetActive(true);

                SetImageColor(blushPreview, previewColor);
                break;
        }
    }

    public void SetBrushTipColor(Color color)
    {
        SetImageColor(brushTip, color);
    }

    public void ClearTool()
    {
        HideAll();

        if (lipstickHead != null)
            lipstickHead.gameObject.SetActive(true);
    }

    public IEnumerator MoveTo(Vector2 targetAnchoredPos, float duration, float? targetZ = null)
    {
        if (handRoot == null)
            yield break;

        _isPlayerControl = false;

        Vector2 startPos = handRoot.anchoredPosition;
        float startZ = handRoot.localEulerAngles.z;
        float endZ = targetZ ?? startZ;

        if (duration <= 0f)
        {
            handRoot.anchoredPosition = targetAnchoredPos;
            handRoot.localRotation = Quaternion.Euler(0f, 0f, endZ);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            handRoot.anchoredPosition = Vector2.LerpUnclamped(startPos, targetAnchoredPos, t);
            handRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(startZ, endZ, t));

            yield return null;
        }

        handRoot.anchoredPosition = targetAnchoredPos;
        handRoot.localRotation = Quaternion.Euler(0f, 0f, endZ);
    }

    public Vector2 GetTargetHandPositionForToolPoint(
        RectTransform movementArea,
        Camera uiCamera,
        MakeupToolType toolType,
        Vector2 desiredLocalPoint)
    {
        if (handRoot == null || movementArea == null)
            return desiredLocalPoint;

        RectTransform toolPoint = GetToolContactPoint(toolType);
        if (toolPoint == null)
            return desiredLocalPoint;

        Vector2 toolPointLocal = WorldToLocalInRect(toolPoint.position, movementArea, uiCamera);
        Vector2 handLocal = handRoot.anchoredPosition;

        Vector2 offset = toolPointLocal - handLocal;
        return desiredLocalPoint - offset;
    }

    public bool IsToolOrHandOverZone(RectTransform zone, Camera uiCamera, MakeupToolType toolType)
    {
        if (zone == null)
            return false;

        bool toolInside = false;
        RectTransform toolPoint = GetToolContactPoint(toolType);
        if (toolPoint != null)
        {
            Vector2 toolScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, toolPoint.position);
            toolInside = RectTransformUtility.RectangleContainsScreenPoint(zone, toolScreen, uiCamera);
        }

        RectTransform handPoint = handCenterPoint != null ? handCenterPoint : handRoot;
        bool handInside = false;

        if (handPoint != null)
        {
            Vector2 handScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, handPoint.position);
            handInside = RectTransformUtility.RectangleContainsScreenPoint(zone, handScreen, uiCamera);
        }

        return toolInside || handInside;
    }

    private RectTransform GetToolContactPoint(MakeupToolType toolType)
    {
        switch (toolType)
        {
            case MakeupToolType.Eyeshadow:
                return brushTipContactPoint != null ? brushTipContactPoint : handRoot;

            case MakeupToolType.Blush:
                return blushTipContactPoint != null ? blushTipContactPoint : handRoot;

            case MakeupToolType.Lipstick:
                return lipstickTipContactPoint != null ? lipstickTipContactPoint : handRoot;

            default:
                return handRoot;
        }
    }

    private Vector2 WorldToLocalInRect(Vector3 worldPosition, RectTransform targetRect, Camera uiCamera)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect,
            screenPoint,
            uiCamera,
            out Vector2 localPoint);

        return localPoint;
    }

    private void HideAll()
    {
        if (creamVisual != null) creamVisual.SetActive(false);
        if (brushVisual != null) brushVisual.SetActive(false);
        if (lipstickVisual != null) lipstickVisual.SetActive(false);
        if (blushVisual != null) blushVisual.SetActive(false);
    }

    private void SetImageColor(Image image, Color color)
    {
        if (image != null)
            image.color = color;
    }
}