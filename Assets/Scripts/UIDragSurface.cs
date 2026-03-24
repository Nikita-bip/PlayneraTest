using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragSurface : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private MakeupGameController controller;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.HandleBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.HandleDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (controller != null)
            controller.HandleEndDrag(eventData);
    }
}