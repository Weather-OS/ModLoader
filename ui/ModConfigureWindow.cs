using NeoModLoader.api;
using NeoModLoader.General;
using NeoModLoader.General.ui.prefabs;
using UnityEngine;
using UnityEngine.UI;

namespace NeoModLoader.ui;

public class ModConfigureWindow : AbstractWindow<ModConfigureWindow>
{
    class ModConfigGrid : MonoBehaviour
    {
        private Text title;
        private Transform grid;
        private void OnEnable()
        {
            title = transform.Find("Title").GetComponent<Text>();
            grid = transform.Find("Grid");
        }

        public void Setup(string id, Dictionary<string, ModConfigItem> items)
        {
            title.text = LM.Get(id);
            foreach (var item in items)
            {
                ModConfigListItem list_item = ModConfigureWindow._itemPool.getNext();
                list_item.transform.SetParent(grid);
                list_item.transform.localScale = Vector3.one;
                list_item.Setup(item.Value);
            }
        }
    }
    class ModConfigListItem : MonoBehaviour
    {
        public GameObject switch_area;
        public GameObject slider_area;
        public GameObject text_area;
        public GameObject select_area;
        public void Setup(ModConfigItem pItem)
        {
            switch_area.SetActive(false);
            slider_area.SetActive(false);
            text_area.SetActive(false);
            select_area.SetActive(false);
            switch (pItem.Type)
            {
                case ConfigItemType.SWITCH:
                    setup_switch(pItem);
                    break;
                case ConfigItemType.SLIDER:
                    break;
                case ConfigItemType.TEXT:
                    break;
                case ConfigItemType.SELECT:
                    break;
            }
        }

        private void setup_switch(ModConfigItem pItem)
        {
            switch_area.SetActive(true);
            switch_area.transform.Find("Button").GetComponent<SwitchButton>().Setup(pItem.BoolVal, ()=>pItem.SetValue(!pItem.BoolVal));
            switch_area.transform.Find("Text").GetComponent<Text>().text = LM.Get(pItem.Id);
            if (string.IsNullOrEmpty(pItem.IconPath))
            {
                switch_area.transform.Find("Icon").gameObject.SetActive(false);
            }
            else
            {
                Image icon = switch_area.transform.Find("Icon").GetComponent<Image>();
                icon.gameObject.SetActive(true);
                icon.sprite = SpriteTextureLoader.getSprite(pItem.IconPath);
            }
        }
    }
    private ModConfig _config;
    private static ModConfigGrid _gridPrefab;
    private static ModConfigListItem _itemPrefab;
    private static ObjectPoolGenericMono<ModConfigGrid> _gridPool;
    private static ObjectPoolGenericMono<ModConfigListItem> _itemPool;
    protected override void Init()
    {
        VerticalLayoutGroup layout = ContentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.padding = new RectOffset(32, 32, 0, 0);
        
        ContentSizeFitter fitter = ContentTransform.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _createGridPrefab();
        _createItemPrefab();

        _gridPool = new ObjectPoolGenericMono<ModConfigGrid>(_gridPrefab, ContentTransform);
        _itemPool = new ObjectPoolGenericMono<ModConfigListItem>(_itemPrefab, null);
    }

    private static void _createItemPrefab()
    {
        GameObject config_item = new GameObject("ConfigItem", typeof(Image), typeof(VerticalLayoutGroup));
        VerticalLayoutGroup layout = config_item.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.padding = new(4, 4, 3, 3);
        #region SWITCH
        GameObject switch_area = new GameObject("SwitchArea", typeof(HorizontalLayoutGroup));
        HorizontalLayoutGroup switch_layout = switch_area.GetComponent<HorizontalLayoutGroup>();
        switch_layout.childControlWidth = false;
        switch_layout.childControlHeight = false;
        switch_layout.childAlignment = TextAnchor.MiddleLeft;
        switch_area.transform.SetParent(config_item.transform);
        switch_area.transform.localScale = Vector3.one;
        SwitchButton switch_button = Instantiate(SwitchButton.Prefab, switch_area.transform);
        switch_button.transform.localScale = Vector3.one;
        switch_button.name = "Button";
        GameObject switch_config_icon = new GameObject("Icon", typeof(Image));
        switch_config_icon.transform.SetParent(switch_area.transform);
        switch_config_icon.transform.localScale = Vector3.one;
        switch_config_icon.GetComponent<RectTransform>().sizeDelta = new(16, 16);
        GameObject switch_config_text = new GameObject("Text", typeof(Text));
        switch_config_text.transform.SetParent(switch_area.transform);
        switch_config_text.transform.localScale = Vector3.one;
        switch_config_text.GetComponent<RectTransform>().sizeDelta = new(100, 16);
        Text switch_text = switch_config_text.GetComponent<Text>();
        OT.InitializeCommonText(switch_text);
        switch_text.alignment = TextAnchor.MiddleLeft;
        switch_text.resizeTextForBestFit = true;
        #endregion
        #region SLIDER
        GameObject slider_area = new GameObject("SliderArea", typeof(RectTransform));
        slider_area.transform.SetParent(config_item.transform);
        slider_area.transform.localScale = Vector3.one;
        #endregion
        #region TEXT
        GameObject text_area = new GameObject("TextArea", typeof(RectTransform));
        text_area.transform.SetParent(config_item.transform);
        text_area.transform.localScale = Vector3.one;
        #endregion
        #region SELECT
        GameObject select_area = new GameObject("SelectArea", typeof(RectTransform));
        select_area.transform.SetParent(config_item.transform);
        select_area.transform.localScale = Vector3.one;
        #endregion
        config_item.GetComponent<Image>().sprite = SpriteTextureLoader.getSprite("ui/special/windowInnerSliced");
        config_item.GetComponent<Image>().type = Image.Type.Sliced;
        config_item.transform.SetParent(WorldBoxMod.Transform);
        _itemPrefab = config_item.AddComponent<ModConfigListItem>();
        _itemPrefab.switch_area = switch_area;
        _itemPrefab.slider_area = slider_area;
        _itemPrefab.text_area = text_area;
        _itemPrefab.select_area = select_area;
    }

    private static void _createGridPrefab()
    {
        GameObject config_grid = new GameObject("ConfigGrid", typeof(VerticalLayoutGroup));
        
        VerticalLayoutGroup layout = config_grid.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.UpperCenter;
        /*
        ContentSizeFitter fitter = config_grid.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        */
        GameObject grid_title = new GameObject("Title", typeof(Text));
        grid_title.transform.SetParent(config_grid.transform);
        grid_title.transform.localScale = Vector3.one;
        Text title = grid_title.GetComponent<Text>();
        title.text = "Mod Config";
        title.font = LocalizedTextManager.currentFont;
        title.fontSize = 10;
        title.alignment = TextAnchor.MiddleCenter;
        
        
        GameObject grid = new GameObject("Grid", typeof(Image), typeof(VerticalLayoutGroup));
        grid.transform.SetParent(config_grid.transform);
        grid.transform.localScale = Vector3.one;
        layout = grid.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.padding = new RectOffset(4, 4, 5, 5);
        /*
        fitter = grid.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        */
        grid.GetComponent<Image>().sprite = SpriteTextureLoader.getSprite("ui/special/windowInnerSliced");
        grid.GetComponent<Image>().type = Image.Type.Sliced;
        
        config_grid.transform.SetParent(WorldBoxMod.Transform);
        _gridPrefab = config_grid.AddComponent<ModConfigGrid>();
    }

    public static void ShowWindow(ModConfig pConfig)
    {
        if(pConfig == null) return;
        Instance._config = pConfig;
        ScrollWindow.showWindow(WindowId);
    }

    public override void OnNormalEnable()
    {
        _gridPool.clear();
        _itemPool.clear();
        foreach (var group in _config._config)
        {
            ModConfigGrid grid = _gridPool.getNext();
            grid.Setup(group.Key, group.Value);
        }
    }
}