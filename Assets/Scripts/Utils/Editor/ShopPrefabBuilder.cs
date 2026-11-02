using System.Collections.Generic;
using Configs;
using Gley.EasyIAP;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Views;

namespace Utils.Editor
{
    /// <summary>
    /// Keeps the original shop card design (Assets/Content/Prefabs/UI/Old: Small / Medium / Large cards in rows)
    /// and wires it to real data: picture, title, contents (rewards row), price on the button, badge and bonus.
    /// Also fills InAppConfig (sections, card sizes, badges, bonuses, icons, GDD 11.2 contents). Idempotent.
    /// </summary>
    public static class ShopPrefabBuilder
    {
        private const string OldFolder = "Assets/Content/Prefabs/UI/Old/";
        private const string SmallPath = OldFolder + "InAppProductCardSmall1.prefab";
        private const string MediumPath = OldFolder + "InAppProductCardMedium2.prefab";
        private const string LargePath = OldFolder + "InAppProductAppLarge3.prefab";
        private const string RowPath = OldFolder + "InAppHLG .prefab";
        private const string AtlasPath = "Assets/Content/UI/Shop/shopatlas.png";
        private const string ElementsPath = "Assets/Content/UI/Shop/Elements/";
        private const string ConfigPath = "Assets/Content/Configs/InAppConfig.asset";

        private static readonly Color Ink = Hex("400F9E");
        private static readonly Color InkSoft = Hex("6E4BC2");
        private static readonly Color BonusTint = new(0.78f, 0.72f, 1f);

        private static Dictionary<string, Sprite> _atlas;
        private static Sprite _pill;
        private static TMP_FontAsset _font;

        [MenuItem("Tools/Shop/Rebuild Shop Prefabs And Config")]
        public static void Build()
        {
            LoadArt();

            EditPrefab(SmallPath, WireSmall);
            EditPrefab(MediumPath, WireMedium);
            EditPrefab(LargePath, WireLarge);

            var config = AssetDatabase.LoadAssetAtPath<InAppConfig>(ConfigPath);
            config.ProductViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmallPath).GetComponent<InAppProductView>();
            config.ProductViewMediumPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MediumPath).GetComponent<InAppProductView>();
            config.ProductViewLargePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LargePath).GetComponent<InAppProductView>();
            config.HLGParentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPath).transform;
            config.GemIcon = A(13);
            config.BombIcon = A(15);
            config.ShovelIcon = A(14);
            config.ExtraMovesIcon = A(16);
            config.LivesIcon = A(11);
            config.UnlimitedLivesIcon = A(11);
            ConfigureProducts(config);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("[ShopPrefabBuilder] Shop cards and InAppConfig updated.");
        }

        #region Cards

        // Small 250x280: title on top, picture, contents row, price button.
        private static void WireSmall(GameObject root)
        {
            var t = root.transform;
            var title = Text(t, "Hearts Pack Title");
            Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -38), new Vector2(236, 58));
            title.textWrappingMode = TextWrappingModes.Normal;
            title.lineSpacing = -22;
            AutoSize(title, 30, 22);

            var icon = Find(t, "Hearts Pack Icon").GetComponent<Image>();
            Place(icon.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -115), new Vector2(96, 88));
            Hide(t, "Hearts Pack Amount");

            var rewards = Row(t, "Rewards", new Vector2(0.5f, 0), new Vector2(0, 94), new Vector2(240, 44), 8, TextAnchor.MiddleCenter);
            var item = RewardItem(rewards, 40, 32, Ink);

            var (button, price) = PriceButton(t);
            Place((RectTransform)button.transform, new Vector2(0.5f, 0), new Vector2(0, 42), new Vector2(206, 60));
            var (badge, badgeText) = Pill(t, "Badge", new Vector2(0.5f, 1), new Vector2(0, 8), new Vector2(200, 48), 28);
            var (bonus, bonusText) = Pill(t, "Bonus", new Vector2(0, 1), new Vector2(52, -100), new Vector2(104, 46), 28);
            bonus.GetComponent<Image>().color = BonusTint;
            bonus.transform.localEulerAngles = new Vector3(0, 0, 10);

            SetRefs(root.GetComponent<InAppProductView>(),
                ("buyButton", button), ("image", icon), ("_label", title), ("price", price), ("_amount", null),
                ("_description", null), ("_rewardsParent", rewards), ("_rewardItemPrefab", item),
                ("_badgeRoot", badge), ("_badgeText", badgeText), ("_bonusRoot", bonus), ("_bonusText", bonusText));
        }

        // Medium 500x280: picture left; title, contents / description and price button on the right.
        private static void WireMedium(GameObject root)
        {
            var t = root.transform;
            var icon = Find(t, "Hearts Pack Icon").GetComponent<Image>();
            Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-120, 6), new Vector2(220, 190));

            var title = Text(t, "Hearts Pack Title");
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(124, 88), new Vector2(240, 56));
            title.alignment = TextAlignmentOptions.Center;
            AutoSize(title, 40, 26);

            var desc = Text(t, "Hearts Pack Amount");
            desc.gameObject.SetActive(true);
            Place(desc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(124, 18), new Vector2(244, 84));
            desc.alignment = TextAlignmentOptions.Center;
            desc.color = InkSoft;
            desc.textWrappingMode = TextWrappingModes.Normal;
            desc.lineSpacing = -10;
            AutoSize(desc, 30, 22);

            var rewards = Row(t, "Rewards", new Vector2(0.5f, 0.5f), new Vector2(124, 24), new Vector2(244, 50), 10, TextAnchor.MiddleCenter);
            var item = RewardItem(rewards, 46, 36, Ink);

            var (button, price) = PriceButton(t);
            Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(124, -72), new Vector2(214, 66));
            var (badge, badgeText) = Pill(t, "Badge", new Vector2(1, 1), new Vector2(-110, 8), new Vector2(200, 48), 28);

            SetRefs(root.GetComponent<InAppProductView>(),
                ("buyButton", button), ("image", icon), ("_label", title), ("price", price), ("_amount", null),
                ("_description", desc), ("_rewardsParent", rewards), ("_rewardItemPrefab", item),
                ("_badgeRoot", badge), ("_badgeText", badgeText), ("_bonusRoot", null), ("_bonusText", null));
        }

        // Large 750x280 (purple bundle card): picture left, title + contents grid on the right, price button.
        private static void WireLarge(GameObject root)
        {
            var t = root.transform;
            var icon = Find(t, "Bundle Chest Icon").GetComponent<Image>();
            var title = Text(t, "Starter Bundle Label");
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(196, 100), new Vector2(316, 58));
            AutoSize(title, 44, 28);

            foreach (var old in new[] { "1200 Reward Icon", "1200 Reward Text", "30m Reward Icon", "30m Reward Text" })
            {
                Hide(t, old);
            }

            var rewardsRt = Child(t, "Rewards");
            Place(rewardsRt, new Vector2(0.5f, 0.5f), new Vector2(196, 0), new Vector2(316, 140));
            var grid = rewardsRt.GetComponent<GridLayoutGroup>();
            if(grid == null)
            {
                grid = rewardsRt.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.cellSize = new Vector2(154, 44);
            grid.spacing = new Vector2(8, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperLeft;
            var item = RewardItem(rewardsRt, 44, 36, Color.white);

            var (button, price) = PriceButton(t);
            Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(244, -92), new Vector2(230, 62));
            var (badge, badgeText) = Pill(t, "Badge", new Vector2(0.5f, 1), new Vector2(0, 4), new Vector2(300, 52), 30);

            SetRefs(root.GetComponent<InAppProductView>(),
                ("buyButton", button), ("image", icon), ("_label", title), ("price", price), ("_amount", null),
                ("_description", null), ("_rewardsParent", rewardsRt), ("_rewardItemPrefab", item),
                ("_badgeRoot", badge), ("_badgeText", badgeText), ("_bonusRoot", null), ("_bonusText", null));

            // The card root had its own Button that swallowed taps meant for the buy button.
            var rootButton = root.GetComponent<Button>();
            if(rootButton != null)
            {
                Object.DestroyImmediate(rootButton);
            }
        }

        /// <summary>Old button had "Buy" + a "$1.99/" line; now it shows only the real price.</summary>
        private static (Button, TMP_Text) PriceButton(Transform card)
        {
            var buttonTr = Find(card, "Button");
            var button = buttonTr.GetComponent<Button>();
            Hide(buttonTr, "Count");
            var price = Text(buttonTr, "Text (TMP)");
            price.rectTransform.anchorMin = Vector2.zero;
            price.rectTransform.anchorMax = Vector2.one;
            price.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            price.rectTransform.offsetMin = new Vector2(10, 4);
            price.rectTransform.offsetMax = new Vector2(-10, -2);
            price.margin = Vector4.zero;
            price.textWrappingMode = TextWrappingModes.NoWrap;
            AutoSize(price, 40, 24);
            return (button, price);
        }

        #endregion

        #region Products

        private static void ConfigureProducts(InAppConfig config)
        {
            var gemsIcon = E("IconGems");
            var chestIcon = E("IconBundleChest");
            var products = config.InAppProducts;

            var hasRemoveAds = false;
            for (var i = 0; i < products.Count; i++)
            {
                var p = products[i];
                p.badge = ShopBadge.None;
                p.bonusPercent = 0;
                p.columnWidth = ProductColumnWidth.Small;

                switch (p.product)
                {
                    case ShopProductNames.RemoveAds:
                        hasRemoveAds = true;
                        p.section = ShopSection.NoAds; p.icon = A(18); p.productName = "No Ads"; p.priceLabel = "$2.99";
                        p.columnWidth = ProductColumnWidth.Medium;
                        break;
                    case ShopProductNames.CrystalsStarter:
                        Gems(ref p, 300, 0, A(13));
                        break;
                    case ShopProductNames.CrystalsSmall:
                        Gems(ref p, 1000, 10, A(13));
                        break;
                    case ShopProductNames.CrystalsMedium:
                        Gems(ref p, 1800, 20, gemsIcon); p.badge = ShopBadge.Popular;
                        break;
                    case ShopProductNames.CrystalsBig:
                        Gems(ref p, 4000, 35, gemsIcon);
                        break;
                    case ShopProductNames.CrystalsHuge:
                        Gems(ref p, 9000, 50, chestIcon);
                        break;
                    case ShopProductNames.CrystalsRoyal:
                        Gems(ref p, 25000, 65, chestIcon); p.badge = ShopBadge.BestValue;
                        break;
                    case ShopProductNames.StarterBundle:
                        Bundle(ref p, ShopSection.Featured, A(19), 500, 2, 60);
                        p.oneTimePurchase = true; p.badge = ShopBadge.OneTime;
                        break;
                    case ShopProductNames.BeginnerBundle:
                        Bundle(ref p, ShopSection.Bundles, gemsIcon, 1200, 3, 120);
                        break;
                    case ShopProductNames.BuilderBundle:
                        Bundle(ref p, ShopSection.Bundles, chestIcon, 1500, 3, 180);
                        break;
                    case ShopProductNames.MasterBundle:
                        Bundle(ref p, ShopSection.Bundles, A(19), 3000, 5, 360); p.badge = ShopBadge.Popular;
                        break;
                    case ShopProductNames.NeedMovesOffer:
                        p.section = ShopSection.Hidden; p.icon = A(16);
                        break;
                    case ShopProductNames.OutOfLivesOffer:
                        p.section = ShopSection.Hidden; p.icon = A(11);
                        break;
                    case ShopProductNames.HardLevelOffer:
                        p.section = ShopSection.Hidden; p.icon = A(15);
                        break;
                    case ShopProductNames.DailyGemsDiscount:
                        Daily(ref p, A(13));
                        break;
                    case ShopProductNames.DailyBoostersOffer:
                        Daily(ref p, A(15));
                        break;
                    case ShopProductNames.DailyUnlimitedLivesOffer:
                        Daily(ref p, A(11));
                        break;
                    case ShopProductNames.DailyMovesOffer:
                        Daily(ref p, A(16));
                        break;
                }

                products[i] = p;
            }

            if(!hasRemoveAds)
            {
                products.Insert(0, new InAppProduct
                {
                    product = ShopProductNames.RemoveAds,
                    productName = "No Ads",
                    priceLabel = "$2.99",
                    icon = A(18),
                    section = ShopSection.NoAds,
                    columnWidth = ProductColumnWidth.Medium
                });
            }
        }

        private static void Gems(ref InAppProduct p, int gems, int bonus, Sprite icon)
        {
            p.section = ShopSection.Gems;
            p.gems = gems;
            p.bonusPercent = bonus;
            p.icon = icon;
        }

        private static void Bundle(ref InAppProduct p, ShopSection section, Sprite icon, int gems, int eachBooster, int unlimitedMinutes)
        {
            p.section = section;
            p.columnWidth = ProductColumnWidth.Large;
            p.icon = icon;
            p.gems = gems;
            p.bombs = eachBooster;
            p.butterflies = eachBooster;
            p.extraMoves = eachBooster;
            p.unlimitedLivesMinutes = unlimitedMinutes;
        }

        private static void Daily(ref InAppProduct p, Sprite icon)
        {
            p.section = ShopSection.Daily;
            p.icon = icon;
            p.badge = ShopBadge.Sale;
        }

        #endregion

        #region Helpers

        private static void EditPrefab(string path, System.Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void LoadArt()
        {
            _atlas = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AtlasPath))
            {
                if(asset is Sprite sprite)
                {
                    _atlas[sprite.name] = sprite;
                }
            }

            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Content/Fonts/FredokaSDF Thick.asset");

            // Pill was trimmed to its visible bounds; its rounded ends need wide horizontal borders.
            var pillPath = ElementsPath + "ShopBadgePill.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(pillPath);
            var border = new Vector4(138, 40, 138, 40);
            if(importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }

            _pill = AssetDatabase.LoadAssetAtPath<Sprite>(pillPath);
        }

        private static Sprite A(int index) => _atlas.TryGetValue("shopatlas_" + index, out var sprite) ? sprite : null;

        private static Sprite E(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ElementsPath + name + ".png");

        private static Transform Find(Transform parent, string name)
        {
            var child = parent.Find(name);
            if(child == null)
            {
                throw new System.InvalidOperationException($"[ShopPrefabBuilder] '{name}' not found under '{parent.name}'.");
            }

            return child;
        }

        private static TMP_Text Text(Transform parent, string name) => Find(parent, name).GetComponent<TMP_Text>();

        private static void Hide(Transform parent, string name)
        {
            var child = parent.Find(name);
            if(child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        /// <summary>Find-or-create a child RectTransform (keeps re-runs idempotent).</summary>
        private static RectTransform Child(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if(existing != null)
            {
                return (RectTransform)existing;
            }

            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static RectTransform Row(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, float spacing, TextAnchor align)
        {
            var rt = Child(parent, name);
            Place(rt, anchor, pos, size);
            var row = rt.GetComponent<HorizontalLayoutGroup>();
            if(row == null)
            {
                row = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            row.spacing = spacing;
            row.childAlignment = align;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return rt;
        }

        private static IconWtihTextView RewardItem(Transform parent, float iconSize, float fontSize, Color color)
        {
            var existing = parent.Find("RewardItem");
            if(existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var root = Child(parent, "RewardItem");
            Place(root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, iconSize));
            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 3;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var iconRt = Child(root, "Icon");
            var image = iconRt.gameObject.AddComponent<Image>();
            image.sprite = A(13);
            image.preserveAspect = true;
            image.raycastTarget = false;
            var le = iconRt.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = iconSize;
            le.minHeight = le.preferredHeight = iconSize;

            var textRt = Child(root, "Text");
            var text = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.text = "x3";
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;

            var view = root.gameObject.AddComponent<IconWtihTextView>();
            SetRefs(view, ("_image", image), ("_text", text));
            return view;
        }

        private static (GameObject, TMP_Text) Pill(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize)
        {
            var existing = parent.Find(name);
            if(existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var rt = Child(parent, name);
            Place(rt, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = _pill;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = _pill.rect.height / size.y;
            image.raycastTarget = false;

            var textRt = Child(rt, "Text");
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(14, 2);
            textRt.offsetMax = new Vector2(-14, -1);
            var text = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.text = "Popular";
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            AutoSize(text, fontSize, fontSize * 0.7f);

            rt.gameObject.SetActive(false);
            return (rt.gameObject, text);
        }

        private static void AutoSize(TMP_Text text, float max, float min)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = max;
            text.fontSizeMin = min;
        }

        private static void SetRefs(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var property = so.FindProperty(field);
                if(property == null)
                {
                    UnityEngine.Debug.LogError($"[ShopPrefabBuilder] {target.GetType().Name}.{field} not found.");
                    continue;
                }

                property.objectReferenceValue = value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        #endregion
    }
}
