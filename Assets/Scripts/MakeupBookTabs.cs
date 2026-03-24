using UnityEngine;
using UnityEngine.UI;

public class MakeupBookTabs : MonoBehaviour
{
    [System.Serializable]
    public class TabEntry
    {
        public Button button;
        public Image iconImage;
        public Sprite normalSprite;
        public Sprite activeSprite;
        public GameObject pageRoot;
        public GameObject previewRoot;
    }

    [SerializeField] private TabEntry[] tabs;
    [SerializeField] private int startTabIndex = 0;

    private int _currentTabIndex = -1;

    private void Awake()
    {
        BindButtons();
    }

    private void Start()
    {
        OpenTab(startTabIndex);
    }

    private void BindButtons()
    {
        if (tabs == null)
            return;

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i].button == null)
                continue;

            int index = i;
            tabs[i].button.onClick.RemoveAllListeners();
            tabs[i].button.onClick.AddListener(() => OpenTab(index));
        }
    }

    public void OpenTab(int index)
    {
        if (tabs == null || index < 0 || index >= tabs.Length)
            return;

        _currentTabIndex = index;

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isActive = i == _currentTabIndex;

            if (tabs[i].pageRoot != null)
                tabs[i].pageRoot.SetActive(isActive);

            if (tabs[i].previewRoot != null)
                tabs[i].previewRoot.SetActive(isActive);

            if (tabs[i].iconImage != null)
                tabs[i].iconImage.sprite = isActive ? tabs[i].activeSprite : tabs[i].normalSprite;
        }
    }
}