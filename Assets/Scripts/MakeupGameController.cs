using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MakeupGameController : MonoBehaviour
{
    private enum GameState
    {
        Idle,
        AutoMove,
        Dragging,
        Applying,
        Returning
    }

    [Header("UI Space")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform movementArea;
    [SerializeField] private RectTransform faceZone;
    [SerializeField] private RectTransform lipsZone;

    [Header("Views")]
    [SerializeField] private HandView hand;
    [SerializeField] private CharacterMakeupView character;

    [Header("Static Points")]
    [SerializeField] private RectTransform handDefaultPoint;
    [SerializeField] private RectTransform creamHoldPoint;
    [SerializeField] private RectTransform chestHoldPoint;
    [SerializeField] private RectTransform brushPickupPoint;
    [SerializeField] private RectTransform blushPickupPoint;

    [Header("Apply Points")]
    [SerializeField] private RectTransform creamFacePoint;
    [SerializeField] private RectTransform creamForeheadPoint;
    [SerializeField] private RectTransform creamChinPoint;

    [SerializeField] private RectTransform leftEyePoint;
    [SerializeField] private RectTransform rightEyePoint;
    [SerializeField] private RectTransform lipsPoint;
    [SerializeField] private RectTransform leftCheekPoint;
    [SerializeField] private RectTransform rightCheekPoint;

    [Header("Timings")]
    [SerializeField] private float pickDuration = 0.22f;
    [SerializeField] private float moveDuration = 0.22f;
    [SerializeField] private float applyStepDuration = 0.08f;
    [SerializeField] private float shortPause = 0.04f;

    [Header("Cream Polish")]
    [SerializeField] private float creamApproachDuration = 0.10f;
    [SerializeField] private float creamStrokeDuration = 0.09f;
    [SerializeField] private float creamFinishPause = 0.06f;
    [SerializeField] private float creamApplyZ = -12f;

    [Header("Rotations")]
    [SerializeField] private float defaultZ = 0f;
    [SerializeField] private float creamHoldZ = -10f;
    [SerializeField] private float chestHoldZ = 0f;
    [SerializeField] private float brushApplyZ = -8f;
    [SerializeField] private float lipstickApplyZ = -15f;
    [SerializeField] private float blushApplyZ = -10f;

    private GameState _state = GameState.Idle;
    private Coroutine _flowRoutine;

    private MakeupToolType _activeTool = MakeupToolType.None;
    private int _activeVariantIndex = -1;
    private Color _activePreviewColor = Color.white;
    private Sprite _activeHeldToolSprite;
    private RectTransform _activeSourcePoint;
    private GameObject _activeSourceVisual;

    private Camera UICamera =>
        canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

    private void Start()
    {
        if (hand != null && handDefaultPoint != null)
            hand.SnapTo(ToLocalPoint(handDefaultPoint));

        if (hand != null)
            hand.ClearTool();
    }

    public void TryStartTool(
        MakeupToolType tool,
        int variantIndex,
        Color previewColor,
        Sprite heldToolSprite,
        GameObject sourceVisualToHide,
        RectTransform sourcePoint)
    {
        if (_state != GameState.Idle)
            return;

        if (sourcePoint == null || hand == null || character == null || movementArea == null || faceZone == null)
            return;

        if (_flowRoutine != null)
            StopCoroutine(_flowRoutine);

        switch (tool)
        {
            case MakeupToolType.Cream:
                _flowRoutine = StartCoroutine(BeginCreamRoutine(sourcePoint, sourceVisualToHide));
                break;

            case MakeupToolType.Eyeshadow:
                _flowRoutine = StartCoroutine(BeginEyeshadowRoutine(
                    variantIndex,
                    previewColor,
                    heldToolSprite,
                    sourceVisualToHide,
                    sourcePoint));
                break;

            case MakeupToolType.Lipstick:
                _flowRoutine = StartCoroutine(BeginLipstickRoutine(
                    variantIndex,
                    previewColor,
                    heldToolSprite,
                    sourceVisualToHide,
                    sourcePoint));
                break;

            case MakeupToolType.Blush:
                _flowRoutine = StartCoroutine(BeginBlushRoutine(
                    variantIndex,
                    previewColor,
                    heldToolSprite,
                    sourceVisualToHide,
                    sourcePoint));
                break;
        }
    }

    public void ClearMakeupBySponge()
    {
        if (character == null || hand == null)
            return;

        character.ClearDecorativeMakeup();

        if (_flowRoutine != null)
        {
            StopCoroutine(_flowRoutine);
            _flowRoutine = null;
        }

        SetActiveSourceVisual(true);

        hand.EndPlayerControl();
        hand.ClearTool();

        if (handDefaultPoint != null)
            hand.SnapTo(ToLocalPoint(handDefaultPoint));

        ResetActiveTool();
        _state = GameState.Idle;
    }

    public void HandleBeginDrag(PointerEventData eventData)
    {
        if (_state != GameState.Dragging)
            return;

        UpdateDragPosition(eventData.position);
    }

    public void HandleDrag(PointerEventData eventData)
    {
        if (_state != GameState.Dragging)
            return;

        UpdateDragPosition(eventData.position);
    }

    public void HandleEndDrag(PointerEventData eventData)
    {
        if (_state != GameState.Dragging)
            return;

        RectTransform dropZone = GetDropZoneForCurrentTool();
        bool droppedOnValidZone;

        if (_activeTool == MakeupToolType.Eyeshadow ||
            _activeTool == MakeupToolType.Blush ||
            _activeTool == MakeupToolType.Lipstick)
        {
            droppedOnValidZone =
                hand != null && hand.IsToolOrHandOverZone(dropZone, UICamera, _activeTool);

            if (!droppedOnValidZone)
                droppedOnValidZone = RectTransformUtility.RectangleContainsScreenPoint(dropZone, eventData.position, UICamera);
        }
        else
        {
            droppedOnValidZone = RectTransformUtility.RectangleContainsScreenPoint(dropZone, eventData.position, UICamera);
        }

        if (!droppedOnValidZone)
            return;

        if (_flowRoutine != null)
            StopCoroutine(_flowRoutine);

        _flowRoutine = StartCoroutine(ApplyCurrentToolRoutine());
    }

    private IEnumerator BeginCreamRoutine(RectTransform creamPoint, GameObject sourceVisualToHide)
    {
        _state = GameState.AutoMove;
        _activeTool = MakeupToolType.Cream;
        _activeVariantIndex = -1;
        _activePreviewColor = Color.white;
        _activeHeldToolSprite = null;
        _activeSourcePoint = creamPoint;
        _activeSourceVisual = sourceVisualToHide;

        hand.ClearTool();

        // Рука подходит к крему пустой
        yield return hand.MoveTo(ToLocalPoint(creamPoint), pickDuration, defaultZ);

        // "Берёт" крем
        SetActiveSourceVisual(false);
        hand.ShowTool(MakeupToolType.Cream, Color.white, null);

        yield return new WaitForSecondsRealtime(shortPause);
        yield return hand.MoveTo(ToLocalPoint(creamHoldPoint), moveDuration, creamHoldZ);

        _state = GameState.Dragging;
        hand.BeginPlayerControl(hand.CurrentPosition);
    }

    private IEnumerator BeginEyeshadowRoutine(
        int variantIndex,
        Color previewColor,
        Sprite heldToolSprite,
        GameObject sourceVisualToHide,
        RectTransform palettePoint)
    {
        _state = GameState.AutoMove;
        _activeTool = MakeupToolType.Eyeshadow;
        _activeVariantIndex = variantIndex;
        _activePreviewColor = previewColor;
        _activeHeldToolSprite = heldToolSprite;
        _activeSourcePoint = brushPickupPoint;
        _activeSourceVisual = sourceVisualToHide;

        hand.ClearTool();

        // Рука подходит к кисточке пустой
        yield return hand.MoveTo(ToLocalPoint(brushPickupPoint), pickDuration, defaultZ);

        // "Берёт" кисточку, кисточка исчезает из книги
        SetActiveSourceVisual(false);
        hand.ShowTool(MakeupToolType.Eyeshadow, Color.white, heldToolSprite);

        yield return new WaitForSecondsRealtime(shortPause);

        // Кончик кисточки касается цвета
        yield return MoveToolPointTo(
            MakeupToolType.Eyeshadow,
            ToLocalPoint(palettePoint),
            moveDuration,
            defaultZ);

        hand.SetBrushTipColor(previewColor);
        yield return new WaitForSecondsRealtime(shortPause);

        // Рука уходит в точку ожидания
        yield return hand.MoveTo(ToLocalPoint(chestHoldPoint), moveDuration, chestHoldZ);

        _state = GameState.Dragging;
        hand.BeginPlayerControl(hand.CurrentPosition);
    }

    private IEnumerator BeginLipstickRoutine(
        int variantIndex,
        Color previewColor,
        Sprite heldToolSprite,
        GameObject sourceVisualToHide,
        RectTransform lipstickPoint)
    {
        _state = GameState.AutoMove;
        _activeTool = MakeupToolType.Lipstick;
        _activeVariantIndex = variantIndex;
        _activePreviewColor = previewColor;
        _activeHeldToolSprite = heldToolSprite;
        _activeSourcePoint = lipstickPoint;
        _activeSourceVisual = sourceVisualToHide;

        hand.ClearTool();

        // Рука подходит к выбранной помаде пустой
        yield return hand.MoveTo(ToLocalPoint(lipstickPoint), pickDuration, defaultZ);

        // "Берёт" выбранную помаду
        SetActiveSourceVisual(false);
        hand.ShowTool(MakeupToolType.Lipstick, previewColor, heldToolSprite);

        yield return new WaitForSecondsRealtime(shortPause);
        yield return hand.MoveTo(ToLocalPoint(chestHoldPoint), moveDuration, chestHoldZ);

        _state = GameState.Dragging;
        hand.BeginPlayerControl(hand.CurrentPosition);
    }

    private IEnumerator BeginBlushRoutine(
        int variantIndex,
        Color previewColor,
        Sprite heldToolSprite,
        GameObject sourceVisualToHide,
        RectTransform blushColorPoint)
    {
        _state = GameState.AutoMove;
        _activeTool = MakeupToolType.Blush;
        _activeVariantIndex = variantIndex;
        _activePreviewColor = previewColor;
        _activeHeldToolSprite = heldToolSprite;
        _activeSourcePoint = blushPickupPoint;
        _activeSourceVisual = sourceVisualToHide;

        hand.ClearTool();

        // Рука подходит к кисти для румян пустой
        yield return hand.MoveTo(ToLocalPoint(blushPickupPoint), pickDuration, defaultZ);

        // "Берёт" кисть
        SetActiveSourceVisual(false);
        hand.ShowTool(MakeupToolType.Blush, Color.white, heldToolSprite);

        yield return new WaitForSecondsRealtime(shortPause);

        // Кончик кисти касается выбранного цвета
        yield return MoveToolPointTo(
            MakeupToolType.Blush,
            ToLocalPoint(blushColorPoint),
            moveDuration,
            defaultZ);

        hand.SetBlushPreviewColor(previewColor);
        yield return new WaitForSecondsRealtime(shortPause);

        // Рука уходит в точку ожидания
        yield return hand.MoveTo(ToLocalPoint(chestHoldPoint), moveDuration, chestHoldZ);

        _state = GameState.Dragging;
        hand.BeginPlayerControl(hand.CurrentPosition);
    }

    private IEnumerator ApplyCurrentToolRoutine()
    {
        _state = GameState.Applying;
        hand.EndPlayerControl();

        switch (_activeTool)
        {
            case MakeupToolType.Cream:
                yield return ApplyCreamRoutine();
                break;

            case MakeupToolType.Eyeshadow:
                yield return ApplyEyeshadowRoutine();
                break;

            case MakeupToolType.Lipstick:
                yield return ApplyLipstickRoutine();
                break;

            case MakeupToolType.Blush:
                yield return ApplyBlushRoutine();
                break;
        }

        yield return ReturnToolRoutine();
    }

    private IEnumerator ApplyCreamRoutine()
    {
        Vector2 center = ToLocalPoint(creamFacePoint);
        Vector2 forehead = ToLocalPoint(creamForeheadPoint);
        Vector2 chin = ToLocalPoint(creamChinPoint);
        Vector2 leftCheek = ToLocalPoint(leftCheekPoint);
        Vector2 rightCheek = ToLocalPoint(rightCheekPoint);

        yield return hand.MoveTo(leftCheek + new Vector2(-10f, 6f), creamApproachDuration, creamApplyZ);
        yield return hand.MoveTo(leftCheek + new Vector2(10f, -4f), creamStrokeDuration, creamApplyZ);
        yield return hand.MoveTo(forehead + new Vector2(0f, 8f), creamStrokeDuration, creamApplyZ);
        yield return hand.MoveTo(rightCheek + new Vector2(-10f, -4f), creamStrokeDuration, creamApplyZ);
        yield return hand.MoveTo(chin + new Vector2(0f, 4f), creamStrokeDuration, creamApplyZ);
        yield return hand.MoveTo(center, creamStrokeDuration, creamApplyZ);

        character.SetCreamApplied(true);

        yield return new WaitForSecondsRealtime(creamFinishPause);
    }

    private IEnumerator ApplyEyeshadowRoutine()
    {
        Vector2 left = ToLocalPoint(leftEyePoint);
        Vector2 right = ToLocalPoint(rightEyePoint);

        yield return MoveToolPointTo(
            MakeupToolType.Eyeshadow,
            left + new Vector2(-10f, 0f),
            applyStepDuration,
            brushApplyZ);

        yield return MoveToolPointTo(
            MakeupToolType.Eyeshadow,
            left + new Vector2(10f, 4f),
            applyStepDuration,
            brushApplyZ);

        yield return MoveToolPointTo(
            MakeupToolType.Eyeshadow,
            right + new Vector2(-10f, 0f),
            applyStepDuration,
            brushApplyZ);

        yield return MoveToolPointTo(
            MakeupToolType.Eyeshadow,
            right + new Vector2(10f, 4f),
            applyStepDuration,
            brushApplyZ);

        character.ApplyEyeshadow(_activeVariantIndex);
    }

    private IEnumerator ApplyLipstickRoutine()
    {
        Vector2 lips = ToLocalPoint(lipsPoint);

        yield return MoveToolPointTo(
            MakeupToolType.Lipstick,
            lips + new Vector2(-14f, 0f),
            applyStepDuration,
            lipstickApplyZ);

        yield return MoveToolPointTo(
            MakeupToolType.Lipstick,
            lips + new Vector2(14f, 0f),
            applyStepDuration,
            lipstickApplyZ);

        yield return MoveToolPointTo(
            MakeupToolType.Lipstick,
            lips + new Vector2(2f, -3f),
            applyStepDuration,
            lipstickApplyZ);

        character.ApplyLipstick(_activeVariantIndex);
    }

    private IEnumerator ApplyBlushRoutine()
    {
        Vector2 left = ToLocalPoint(leftCheekPoint);
        Vector2 right = ToLocalPoint(rightCheekPoint);

        yield return MoveToolPointTo(
            MakeupToolType.Blush,
            left + new Vector2(-8f, 8f),
            applyStepDuration,
            blushApplyZ);

        yield return MoveToolPointTo(
            MakeupToolType.Blush,
            left + new Vector2(8f, -8f),
            applyStepDuration,
            blushApplyZ);

        yield return MoveToolPointTo(
            MakeupToolType.Blush,
            right + new Vector2(-8f, 8f),
            applyStepDuration,
            blushApplyZ);

        yield return MoveToolPointTo(
            MakeupToolType.Blush,
            right + new Vector2(8f, -8f),
            applyStepDuration,
            blushApplyZ);

        character.ApplyBlush(_activeVariantIndex);
    }

    private IEnumerator ReturnToolRoutine()
    {
        _state = GameState.Returning;

        RectTransform returnPoint = GetReturnPointForCurrentTool();

        if (returnPoint != null)
            yield return hand.MoveTo(ToLocalPoint(returnPoint), moveDuration, defaultZ);

        // Возвращаем предмет в источник
        SetActiveSourceVisual(true);

        hand.ClearTool();

        if (handDefaultPoint != null)
            yield return hand.MoveTo(ToLocalPoint(handDefaultPoint), moveDuration, defaultZ);

        ResetActiveTool();
        _state = GameState.Idle;
        _flowRoutine = null;
    }

    private IEnumerator MoveToolPointTo(MakeupToolType toolType, Vector2 targetLocalPoint, float duration, float z)
    {
        Vector2 handTarget = hand.GetTargetHandPositionForToolPoint(
            movementArea,
            UICamera,
            toolType,
            targetLocalPoint);

        yield return hand.MoveTo(handTarget, duration, z);
    }

    private RectTransform GetDropZoneForCurrentTool()
    {
        switch (_activeTool)
        {
            case MakeupToolType.Lipstick:
                return lipsZone != null ? lipsZone : faceZone;

            default:
                return faceZone;
        }
    }

    private RectTransform GetReturnPointForCurrentTool()
    {
        switch (_activeTool)
        {
            case MakeupToolType.Eyeshadow:
                return brushPickupPoint;

            case MakeupToolType.Blush:
                return blushPickupPoint;

            default:
                return _activeSourcePoint;
        }
    }

    private void SetActiveSourceVisual(bool visible)
    {
        if (_activeSourceVisual != null)
            _activeSourceVisual.SetActive(visible);
    }

    private void ResetActiveTool()
    {
        _activeTool = MakeupToolType.None;
        _activeVariantIndex = -1;
        _activePreviewColor = Color.white;
        _activeHeldToolSprite = null;
        _activeSourcePoint = null;
        _activeSourceVisual = null;
    }

    private void UpdateDragPosition(Vector2 screenPosition)
    {
        Vector2 localPoint = ScreenToMovementLocal(screenPosition);
        localPoint = ClampToMovementArea(localPoint);
        hand.UpdateDrag(localPoint);
    }

    private Vector2 ScreenToMovementLocal(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            movementArea,
            screenPosition,
            UICamera,
            out Vector2 localPoint);

        return localPoint;
    }

    private Vector2 ToLocalPoint(RectTransform target)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(UICamera, target.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            movementArea,
            screenPoint,
            UICamera,
            out Vector2 localPoint);

        return localPoint;
    }

    private Vector2 ClampToMovementArea(Vector2 localPoint)
    {
        Rect rect = movementArea.rect;

        localPoint.x = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax);
        localPoint.y = Mathf.Clamp(localPoint.y, rect.yMin, rect.yMax);

        return localPoint;
    }
}