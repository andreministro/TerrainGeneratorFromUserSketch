using UnityEngine;
using UnityEngine.UI;

internal sealed class SketchLayout
{
    private static readonly Color Panel = new Color32(27, 36, 45, 255);
    private static readonly Color ButtonColor = new Color32(45, 59, 69, 255);
    private static readonly Color Selected = new Color32(58, 91, 94, 255);
    private static readonly Color TextColor = new Color32(238, 245, 244, 255);
    private static readonly Color Muted = new Color32(167, 188, 191, 255);
    private static readonly Color Accent = new Color32(145, 225, 189, 255);

    // Ascending by typical surface elevation in the default terrain presets.
    private static readonly (string Name, Biome Biome)[] Palette =
    {
        ("ButtonBBlue", Biome.Water),
        ("ButtonBRed", Biome.Savannah),
        ("ButtonBYellow", Biome.HotDesert),
        ("ButtonBWhite", Biome.ColdDesert),
        ("ButtonBGreen", Biome.TemperateForest),
        ("ButtonBBlack", Biome.Taiga),
        ("ButtonBCyan", Biome.TropicalForest),
        ("ButtonBMagenta", Biome.AlpineTundra)
    };
    private static readonly string[] BrushNames = { "ButtonTiny", "ButtonSmall", "ButtonMedium", "ButtonBig" };
    private readonly RectTransform root;
    private readonly RectTransform painter;
    private readonly RectTransform sizeCard;
    private readonly RectTransform sizeSlider;
    private readonly RectTransform openButton;
    private readonly RectTransform sizeTitle;
    private readonly RectTransform sizeValue;
    private readonly RectTransform minimumSize;
    private readonly RectTransform maximumSize;
    private readonly RectTransform tools;
    private readonly RectTransform outline;
    private readonly Text biomeLabel;
    private readonly Text brushLabel;
    private readonly Button[] biomes = new Button[Palette.Length];
    private readonly Button[] brushes = new Button[BrushNames.Length];
    private readonly Button eraser;
    private readonly Button undo;
    private readonly InputField seed;
    private readonly Button save;
    private readonly Text status;
    private Vector2 previousSize;
    private Button selectedBiome;
    private Button selectedBrush;

    public RawImage Preview { get; }

    public SketchLayout(RectTransform root, GameObject sizePanel, GameObject painterControls, Sketch sketch,
        InputField seed, Button save, Text status)
    {
        this.root = root;
        this.seed = seed;
        this.save = save;
        this.status = status;
        painter = (RectTransform)painterControls.transform;
        Stretch(painter, 0);

        var sizePanelRect = (RectTransform)sizePanel.transform;
        Stretch(sizePanelRect, 0);
        sizePanel.GetComponent<Image>().color = new Color32(16, 23, 30, 255);
        sizeCard = (RectTransform)sizePanelRect.Find("Size Card");
        sizeCard.GetComponent<Image>().color = Panel;
        var open = sizeCard.Find("Start Drawing").GetComponent<Button>();
        openButton = (RectTransform)open.transform;
        sizeSlider = (RectTransform)sizeCard.Find("Canvas Size Slider");
        sizeTitle = (RectTransform)sizeCard.Find("Canvas size");
        sizeValue = (RectTransform)sizeCard.Find("Selected Size");
        minimumSize = (RectTransform)sizeCard.Find("Minimum Size");
        maximumSize = (RectTransform)sizeCard.Find("Maximum Size");
        sizeValue.GetComponent<Text>().color = Accent;
        sizeTitle.GetComponent<Text>().color = TextColor;
        minimumSize.GetComponent<Text>().color = Muted;
        maximumSize.GetComponent<Text>().color = Muted;
        StyleButton(open, Accent);
        open.GetComponentInChildren<Text>().color = Panel;
        var font = open.GetComponentInChildren<Text>().font;
        sizeTitle.GetComponent<Text>().fontStyle = FontStyle.Bold;

        outline = Create<RectTransform>("Drawing Area", painter);
        Preview = Create<RawImage>("Sketch Preview", outline);
        Preview.raycastTarget = false;
        Stretch(Preview.rectTransform, 1.5f);
        AddOutline();

        var toolPanel = Create<Image>("Tools", painter);
        toolPanel.color = Panel;
        tools = toolPanel.rectTransform;
        biomeLabel = MakeLabel("Biomes", tools, font, 17);
        brushLabel = MakeLabel("Brush size", tools, font, 15);

        for (int i = 0; i < biomes.Length; i++)
        {
            var (name, biome) = Palette[i];
            var button = FindButton(name);
            biomes[i] = button;
            StyleButton(button, ButtonColor, TextAnchor.MiddleLeft, 30);
            var swatch = Create<Image>("Biome color", button.transform);
            swatch.color = BiomePalette.GetColor(biome);
            swatch.raycastTarget = false;
            var rect = swatch.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(16, 0);
            rect.sizeDelta = new Vector2(12, 12);
            button.onClick.AddListener(() => Select(ref selectedBiome, button));
        }

        for (int i = 0; i < brushes.Length; i++)
        {
            var button = FindButton(BrushNames[i]);
            brushes[i] = button;
            StyleButton(button, ButtonColor);
            button.GetComponentInChildren<Text>().text = new[] { "Tiny", "Small", "Medium", "Big" }[i];
            button.onClick.AddListener(() => Select(ref selectedBrush, button));
        }
        Select(ref selectedBrush, brushes[1]);

        undo = FindButton("ButtonUndo");
        StyleButton(undo, ButtonColor);
        eraser = Object.Instantiate(undo, tools, false);
        eraser.name = "Eraser";
        eraser.GetComponentInChildren<Text>().text = "Eraser";
        eraser.onClick = new Button.ButtonClickedEvent();
        eraser.onClick.AddListener(sketch.Erase);
        eraser.onClick.AddListener(() => Select(ref selectedBiome, eraser));

        seed.transform.SetParent(tools, false);
        seed.image.sprite = null;
        seed.image.color = ButtonColor;
        seed.textComponent.color = TextColor;
        if (seed.placeholder is Text placeholder)
        {
            placeholder.text = "Seed (optional)";
            placeholder.color = Muted;
            placeholder.fontStyle = FontStyle.Normal;
        }
        save.transform.SetParent(tools, false);
        StyleButton(save, Accent);
        save.GetComponentInChildren<Text>().color = Panel;
        save.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
        status.transform.SetParent(tools, false);
        status.color = TextColor;
        status.alignment = TextAnchor.MiddleLeft;
        status.raycastTarget = false;
    }

    public void Refresh()
    {
        Vector2 size = root.rect.size;
        if (size.x <= 0 || size.y <= 0 || size == previousSize)
            return;
        previousSize = size;

        float cardWidth = Mathf.Min(460, size.x - 32);
        float cardHeight = Mathf.Min(232, size.y - 32);
        Place(sizeCard, (size.x - cardWidth) * 0.5f, (size.y - cardHeight) * 0.5f, cardWidth, cardHeight);
        float contentWidth = cardWidth - 56;
        Place(sizeTitle, 28, cardHeight * 0.07f, contentWidth, cardHeight * 0.16f);
        Place(sizeValue, 28, cardHeight * 0.22f, contentWidth, cardHeight * 0.20f);
        Place(sizeSlider, 28, cardHeight * 0.45f, contentWidth, cardHeight * 0.11f);
        Place(minimumSize, 23, cardHeight * 0.57f, 55, cardHeight * 0.12f);
        Place(maximumSize, cardWidth - 78, cardHeight * 0.57f, 55, cardHeight * 0.12f);
        Place(openButton, 28, cardHeight * 0.74f, contentWidth, cardHeight * 0.17f);

        bool sideBySide = size.x >= 700 && size.x / size.y >= 1.26f;
        if (sideBySide)
        {
            float side = Mathf.Min(size.y - 40, size.x - 318);
            float toolsWidth = Mathf.Min(300, size.x - side - 58);
            float groupLeft = (size.x - side - toolsWidth - 18) * 0.5f;
            float toolsHeight = Mathf.Min(size.y - 40, 594);
            Place(tools, groupLeft, (size.y - toolsHeight) * 0.5f, toolsWidth, toolsHeight);
            Place(outline, groupLeft + toolsWidth + 18, (size.y - side) * 0.5f, side, side);
            PlaceTools(toolsWidth, toolsHeight, 1);
        }
        else
        {
            float width = Mathf.Min(size.x - 28, 648);
            int columns = width >= 620 ? 4 : 2;
            float height = Mathf.Min(columns == 4 ? 257 : 329, size.y * 0.57f);
            float side = Mathf.Min(width, size.y - height - 44);
            float top = (size.y - height - side - 12) * 0.5f;
            Place(outline, (size.x - side) * 0.5f, top, side, side);
            Place(tools, (size.x - width) * 0.5f, top + side + 12, width, height);
            PlaceTools(width, height, columns);
        }
    }

    private void PlaceTools(float width, float height, int columns)
    {
        const float padding = 12;
        const float gap = 5;
        float inner = width - 2 * padding;
        int biomeRows = biomes.Length / columns;
        float row = Mathf.Min(36, (height - 2 * padding - 43 - gap * (biomeRows + 4)) / (biomeRows + 4));
        float y = padding;
        Place(biomeLabel.rectTransform, padding, y, inner, 22);
        y += 22 + gap;
        float biomeWidth = (inner - gap * (columns - 1)) / columns;
        for (int i = 0; i < biomes.Length; i++)
            Place((RectTransform)biomes[i].transform, padding + i % columns * (biomeWidth + gap),
                y + i / columns * (row + gap), biomeWidth, row);
        y += biomeRows * (row + gap);
        Place(brushLabel.rectTransform, padding, y, inner, 21);
        y += 21 + gap;
        float brushWidth = (inner - gap * 3) / 4;
        for (int i = 0; i < brushes.Length; i++)
            Place((RectTransform)brushes[i].transform, padding + i * (brushWidth + gap), y, brushWidth, row);
        y += row + gap;
        float half = (inner - gap) * 0.5f;
        Place((RectTransform)eraser.transform, padding, y, half, row);
        Place((RectTransform)undo.transform, padding + half + gap, y, half, row);
        y += row + gap;
        Place((RectTransform)seed.transform, padding, y, half, row);
        Place((RectTransform)save.transform, padding + half + gap, y, half, row);
        Place(status.rectTransform, padding, y + row, inner, row + gap);
    }

    private Button FindButton(string name)
    {
        var button = painter.Find(name).GetComponent<Button>();
        button.transform.SetParent(tools, false);
        return button;
    }

    private void AddOutline()
    {
        for (int i = 0; i < 4; i++)
        {
            var edge = Create<Image>("Canvas Border", outline);
            edge.raycastTarget = false;
            edge.color = Muted;
            bool horizontal = i < 2;
            int side = i % 2;
            var rect = edge.rectTransform;
            rect.anchorMin = horizontal ? new Vector2(0, side) : new Vector2(side, 0);
            rect.anchorMax = horizontal ? new Vector2(1, side) : new Vector2(side, 1);
            rect.pivot = horizontal ? new Vector2(0.5f, side) : new Vector2(side, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = horizontal ? new Vector2(0, 1.5f) : new Vector2(1.5f, 0);
        }
    }

    private static void StyleButton(Button button, Color fill, TextAnchor align = TextAnchor.MiddleCenter, float left = 4)
    {
        button.image.sprite = null;
        button.image.color = fill;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1);
        colors.selectedColor = colors.normalColor;
        colors.pressedColor = new Color(0.84f, 0.88f, 0.88f, 1);
        button.colors = colors;
        var label = button.GetComponentInChildren<Text>();
        label.color = TextColor;
        label.alignment = align;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 8;
        label.resizeTextMaxSize = 16;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        Stretch(label.rectTransform, 3);
        label.rectTransform.offsetMin = new Vector2(left, 1);
    }

    private static void Select(ref Button current, Button next)
    {
        if (current != null)
            current.image.color = ButtonColor;
        current = next;
        current.image.color = Selected;
    }

    private static Text MakeLabel(string title, Transform parent, Font font, int size)
    {
        var label = Create<Text>(title, parent);
        label.text = title;
        label.font = font;
        label.fontSize = size;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Muted;
        label.raycastTarget = false;
        return label;
    }

    private static T Create<T>(string name, Transform parent) where T : Component
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);
        return typeof(T) == typeof(RectTransform) ? obj.GetComponent<T>() : obj.AddComponent<T>();
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = -Vector2.one * inset;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
