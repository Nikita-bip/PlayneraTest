using UnityEngine;
using UnityEngine.UI;

public class CharacterMakeupView : MonoBehaviour
{
    [Header("Base Doll")]
    [SerializeField] private Image baseDollImage;
    [SerializeField] private Sprite baseDollSprite;

    [Header("Acne Overlay")]
    [SerializeField] private Image acneOverlayImage;
    [SerializeField] private Sprite acneSprite;

    [Header("Eyeshadow Overlay")]
    [SerializeField] private Image eyeshadowOverlayImage;
    [SerializeField] private Sprite[] eyeshadowSprites;

    [Header("Lipstick Overlay")]
    [SerializeField] private Image lipstickOverlayImage;
    [SerializeField] private Sprite[] lipstickSprites;

    [Header("Blush Overlay")]
    [SerializeField] private Image blushOverlayImage;
    [SerializeField] private Sprite[] blushSprites;

    private bool _creamApplied;
    private int _eyeshadowIndex = -1;
    private int _lipstickIndex = -1;
    private int _blushIndex = -1;

    private void Awake()
    {
        RefreshView();
    }

    public void SetCreamApplied(bool applied)
    {
        _creamApplied = applied;
        RefreshView();
    }

    public void ApplyEyeshadow(int index)
    {
        _eyeshadowIndex = index;
        RefreshView();
    }

    public void ApplyLipstick(int index)
    {
        _lipstickIndex = index;
        RefreshView();
    }

    public void ApplyBlush(int index)
    {
        _blushIndex = index;
        RefreshView();
    }

    public void ClearDecorativeMakeup()
    {
        _eyeshadowIndex = -1;
        _lipstickIndex = -1;
        _blushIndex = -1;
        RefreshView();
    }

    private void RefreshView()
    {
        if (baseDollImage != null)
            baseDollImage.sprite = baseDollSprite;

        if (acneOverlayImage != null)
        {
            acneOverlayImage.sprite = acneSprite;
            acneOverlayImage.enabled = !_creamApplied;
        }

        ApplyOverlay(eyeshadowOverlayImage, eyeshadowSprites, _eyeshadowIndex);
        ApplyOverlay(lipstickOverlayImage, lipstickSprites, _lipstickIndex);
        ApplyOverlay(blushOverlayImage, blushSprites, _blushIndex);
    }

    private void ApplyOverlay(Image target, Sprite[] sprites, int index)
    {
        if (target == null)
            return;

        bool isValid = sprites != null &&
                       index >= 0 &&
                       index < sprites.Length &&
                       sprites[index] != null;

        target.enabled = isValid;

        if (isValid)
            target.sprite = sprites[index];
    }
}