using UnityEngine;
using UnityEngine.EventSystems;

public class MakeupToolButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private MakeupGameController controller;
    [SerializeField] private MakeupToolType toolType;
    [SerializeField] private int variantIndex = 0;
    [SerializeField] private Color previewColor = Color.white;
    [SerializeField] private RectTransform clickPointOverride;
    [SerializeField] private Sprite heldToolSprite;
    [SerializeField] private GameObject sourceVisualToHide;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller == null)
            return;

        RectTransform clickPoint = clickPointOverride != null
            ? clickPointOverride
            : transform as RectTransform;

        controller.TryStartTool(
            toolType,
            variantIndex,
            previewColor,
            heldToolSprite,
            sourceVisualToHide,
            clickPoint);
    }
}