using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.AppUI.Core;
using Unity.Muse.AppUI.UI;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace Unity.Muse.Common
{
    class ReferenceOperatorView : ExVisualElement
    {
        // TODO: Replace this base64 images by actual UUIDs when the backend will support it.

        static string[] k_ProvidedPatternsBase64Encoded =
        {
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape01).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape02).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape03).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape04).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape05).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape06).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape07).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape08).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape09).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape10).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape11).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape12).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape13).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape14).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape15).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape16).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape17).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape18).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape19).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape20).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape21).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape22).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape23).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape24).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape25).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape26).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape27).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape28).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape29).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape30).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape31).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape32).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape33).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape34).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape35).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape36).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape37).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape38).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape39).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape40).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape41).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape42).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape43).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape44).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape45).EncodeToPNG()),
            Convert.ToBase64String(ResourceManager.Load<Texture2D>(PackageResources.patternShape46).EncodeToPNG()),
        };

        static readonly Texture2D[] k_PatternTextures = k_ProvidedPatternsBase64Encoded.Select(guid =>
            new Texture2D(2, 2) { name = guid, hideFlags = HideFlags.HideAndDontSave }).ToArray();

        // TODO: Source items for patterns should be simple image artifacts based on the UUID list
        //static readonly SimpleImageArtifact[] k_PatternArtifacts =
        //    k_ProvidedPatternsGuid.Select(guid => new SimpleImageArtifact(guid, 0)).ToArray();

        internal event Action dataChanged;

        ReferenceOperator.Mode m_Mode = ReferenceOperator.Mode.Color;

        VisualElement m_CommonToolbar;

        ActionGroup m_ModeGroup;

        TouchSliderInt m_StrengthSlider;

        Image m_PreviewImage;

        VisualElement m_DropZoneHelper;

        ActionButton m_PatternsButton;

        AppUI.UI.GridView m_PatternsView;

        Popover m_PatternsPopover;

        Texture2DDropManipulator m_DropManipulator;

        VisualElement m_DropZone;

        VisualElement m_DropzoneContextMenuAnchor;

        ActionButton m_ClearButton;

        string m_Guid;

        readonly Model m_Model;

        Texture2D m_ColorImage;

        Texture2D m_ShapeImage;

        Text m_DropzoneMessage;

        ActionButton m_PickColorButton, m_ShapeColorButton;

        private Color?[] m_PickedColor; 

        Texture2D m_PickingTex;

        Popover m_PickerPopup;

        ColorPicker m_Picker;

        static Color s_LastPickedColor = Color.black;
        
        const int k_MaxTextureSize = 512;
        
        NullableColorField m_ColorField;

        [Preserve]
        static ReferenceOperatorView()
        {
            // load textures
            for (var i = 0; i < k_ProvidedPatternsBase64Encoded.Length; i++)
            {
                var guid = k_ProvidedPatternsBase64Encoded[i];
                var img = k_PatternTextures[i];
                img.LoadImage(Convert.FromBase64String(guid));
            }
        }

        public ReferenceOperatorView(Model model)
        {
            m_PickedColor = new Color?[Enum.GetValues(typeof(ReferenceOperator.Mode)).Length];

            m_Model = model;

            CreateGUI();

            SetModeWithoutNotify(ReferenceOperator.Mode.Color);
            SetColorImageWithoutNotify(null);
        }

        void CreateGUI()
        {
            passMask = Passes.Clear | Passes.OutsetShadows | Passes.BackgroundColor;

            AddToClassList("muse-node");
            AddToClassList("appui-elevation-8");
            name = "input-image-node";

            var text = new Text();
            text.text = "Input Image";
            text.AddToClassList("muse-node__title");
            text.AddToClassList("bottom-gap");
            Add(text);

            var row = new VisualElement();
            row.AddToClassList("row");
            row.AddToClassList("bottom-gap");
            Add(row);

            m_ModeGroup = new ActionGroup
            {
                selectionType = SelectionType.Single,
                compact = true
            };
            m_ModeGroup.selectionChanged += OnModeChanged;
            row.Add(m_ModeGroup);

            var colorModeButton = new ActionButton
            {
                label = TextContent.color
            };
            m_ModeGroup.Add(colorModeButton);

            var shapeModeButton = new ActionButton
            {
                label = TextContent.shape
            };
            m_ModeGroup.Add(shapeModeButton);

            var spacer = new VisualElement();
            spacer.AddToClassList("muse-spacer"); 
            row.Add(spacer);

            m_PatternsButton = new ActionButton
            {
                label = TextContent.patterns
            };
            m_PatternsButton.AddToClassList("right-gap");

            m_PatternsButton.label = "Pattern";

            m_PatternsButton.clicked += OnPatternsButtonClicked;
            row.Add(m_PatternsButton);
            
            m_PickColorButton = new ActionButton();
            m_PickColorButton.icon = "paint-bucket";
            m_PickColorButton.AddToClassList("right-gap");
            m_PickColorButton.clicked += OnPickColorButtonClicked;
#if !USE_APPUI_COLORPICKER
            m_PickColorButton.SetEnabled(Application.isEditor);
#endif
            row.Add(m_PickColorButton);

            m_ClearButton = new ActionButton();
            m_ClearButton.icon = "delete";
            m_ClearButton.clicked += OnClearButtonClicked;
            row.Add(m_ClearButton);

            m_DropZone = new VisualElement
            {
                pickingMode = PickingMode.Position,
                focusable = true,
            };
            m_DropZone.AddToClassList("muse-dropzone");
            m_DropZone.AddToClassList("bottom-gap");
            m_DropZone.name = "muse-dropzone";
            Add(m_DropZone);
            m_DropZone.RegisterCallback<GeometryChangedEvent>(ResizeDropZone);

            m_DropzoneContextMenuAnchor = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    width = 0,
                    height = 0,
                }
            };
            m_DropZone.Add(m_DropzoneContextMenuAnchor);
            m_DropZone.RegisterCallback<PointerDownEvent>(OnDropZonePointerDown);
            m_DropZone.RegisterCallback<KeyDownEvent>(OnDropZoneKeyDown);
            m_DropManipulator = new Texture2DDropManipulator(m_Model);
            m_DropManipulator.onDragStart += OnDragStart;
            m_DropManipulator.onDragEnd += OnDragEnd;
            m_DropManipulator.onDrop += OnDrop;
            m_DropZone.AddManipulator(m_DropManipulator);

            m_PreviewImage = new Image { pickingMode = PickingMode.Ignore };
            m_PreviewImage.AddToClassList("muse-dropzone__image");
            m_DropZone.Add(m_PreviewImage);

            m_DropZoneHelper = new VisualElement { pickingMode = PickingMode.Ignore };
            m_DropZoneHelper.AddToClassList("muse-dropzone__helper");
            m_DropZone.Add(m_DropZoneHelper);

            m_DropzoneMessage = new Text { pickingMode = PickingMode.Position, enableRichText = true };
            m_DropzoneMessage.AddToClassList("muse-dropzone__message");
            m_DropzoneMessage.AddToClassList("bottom-gap");
            m_DropZoneHelper.Add(m_DropzoneMessage);

            var dropzoneButton = new ActionButton { label = TextContent.import };
            dropzoneButton.AddToClassList("muse-dropzone__button");
            m_DropZoneHelper.Add(dropzoneButton);
            dropzoneButton.clicked += OnImportButtonClicked;

            m_CommonToolbar = new VisualElement();
            Add(m_CommonToolbar);

            m_StrengthSlider = new TouchSliderInt
            {
                label = TextContent.strength,
                lowValue = 10,
                highValue = 90,
                value = 20
            };
            m_StrengthSlider.AddToClassList("bottom-gap");
            m_StrengthSlider.RegisterValueChangedCallback(OnIntValueChanged);
            m_CommonToolbar.Add(m_StrengthSlider);

            m_ColorField = new NullableColorField
            {
                clickable = new Pressable(OnPickColorButtonClicked)
            };
            m_ColorField.OnClear += () => OnClearColorButtonClicked();
            
            Add(m_ColorField);

            m_PatternsView = new AppUI.UI.GridView
            {
                itemHeight = 100,
                selectionType = SelectionType.Single,
                columnCount = 2
            };
            m_PatternsView.AddToClassList("muse-patterns-view");

            m_PatternsView.makeItem = MakePatternItemView;
            m_PatternsView.bindItem = BindPatternItemView;
            m_PatternsView.itemsSource = k_PatternTextures;
            m_PatternsView.itemsChosen += (indices) => OnPatternChosen(indices, true);
            m_PatternsView.selectionChanged += (indices) => OnPatternChosen(indices, false);
            m_PatternsView.columnCount = 3;
            m_PatternsView.itemHeight = 75;
        }

        void OnDropZonePointerDown(PointerDownEvent evt)
        {
            if (evt.button == 1)
            {
                evt.StopImmediatePropagation();
#if !UNITY_2023_2_OR_NEWER
                evt.PreventDefault();
#endif

                m_DropzoneContextMenuAnchor.style.left = evt.localPosition.x;
                m_DropzoneContextMenuAnchor.style.top = evt.localPosition.y;

                if (!m_PreviewImage.image)
                    return;

                var contextMenu = new Menu
                {
                    style =
                    {
                        minWidth = 128
                    }
                };

                var copyAction = new MenuItem
                {
                    label = TextContent.copy,
                    shortcut = $"{actionKeyLabel}+C"
                };
                copyAction.clickable.clicked += CopyImageToClipboard;
                contextMenu.Add(copyAction);

                var pasteAction = new MenuItem
                {
                    label = TextContent.paste,
                    shortcut = $"{actionKeyLabel}+V"
                };
                pasteAction.clickable.clicked += PasteImageFromClipboard;
                contextMenu.Add(pasteAction);

                var menu = MenuBuilder.Build(m_DropzoneContextMenuAnchor, contextMenu);
                menu.dismissed += (builder, type) => m_DropZone.RemoveFromClassList(Styles.focusedUssClassName);
                menu.Show();

                m_DropZone.AddToClassList(Styles.focusedUssClassName);
            }
        }

        void OnDropZoneKeyDown(KeyDownEvent evt)
        {
            if (evt.actionKey)
            {
                if (evt.keyCode == KeyCode.C)
                {
                    evt.StopImmediatePropagation();
#if !UNITY_2023_2_OR_NEWER
                    evt.PreventDefault();
#endif


                    if (m_PreviewImage.image)
                        CopyImageToClipboard();
                }
                else if (evt.keyCode == KeyCode.V)
                {
                    evt.StopImmediatePropagation();
#if !UNITY_2023_2_OR_NEWER
                    evt.PreventDefault();
#endif


                    PasteImageFromClipboard();
                }
            }
        }

        const string k_ArtifactMimeType = "artifact/guid;";
        const string k_ImageMimeType = "image/png;base64,";

        void PasteImageFromClipboard()
        {
            var buffer = GUIUtility.systemCopyBuffer;

            if (string.IsNullOrEmpty(buffer))
                return;

            if (buffer.StartsWith(k_ArtifactMimeType))
            {
                var guid = buffer.Substring(k_ArtifactMimeType.Length);
                var artifact = m_Model.AssetsData.FirstOrDefault(a => a.Guid == guid);

                if (artifact is not null && ArtifactCache.IsInCache(artifact))
                {
                    var cachedObj = ArtifactCache.Read(artifact);
                    if (cachedObj is Texture2D img)
                    {
                        SetGuidWithoutNotify(guid);
                        if (m_Mode == ReferenceOperator.Mode.Color)
                            SetColorImageWithoutNotify(img);
                        else
                            SetShapeImageWithoutNotify(img);
                        dataChanged?.Invoke();
                    }
                }
            }
            else if (buffer.StartsWith(k_ImageMimeType))
            {
                var b64String = buffer.Substring(k_ImageMimeType.Length);
                var bytes = Convert.FromBase64String(b64String);
                var img = new Texture2D(2, 2);
                img.LoadImage(bytes);
                SetGuidWithoutNotify(null);
                if (m_Mode == ReferenceOperator.Mode.Color)
                    SetColorImageWithoutNotify(img);
                else
                    SetShapeImageWithoutNotify(img);
                dataChanged?.Invoke();
            }
            else
            {
                Debug.Log(buffer);
            }
        }

        void CopyImageToClipboard()
        {
            var img = (Texture2D)m_PreviewImage.image;

            if (!img && string.IsNullOrEmpty(m_Guid))
                return;

            GUIUtility.systemCopyBuffer = string.IsNullOrEmpty(m_Guid) ?
                $"{k_ImageMimeType}{Convert.ToBase64String(img.EncodeToPNG())}" : $"{k_ArtifactMimeType}{m_Guid}";

            Toast
                .Build(this, TextContent.inputImageCopiedToClipboard, NotificationDuration.Short)
                .Show();
        }

        void OnDrop(Texture2D obj)
        {
            // TODO: Support drag and drop for Shape mode
            if (m_Mode == ReferenceOperator.Mode.Shape)
            {
                Debug.LogWarning("<b>[Muse]</b> Dropping images in shape mode is not yet supported.");
                return;
            }

            SetGuidWithoutNotify(m_DropManipulator.artifact?.Guid);

            if (m_Mode == ReferenceOperator.Mode.Shape)
                SetShapeImageWithoutNotify(obj);
            else
                SetColorImageWithoutNotify(obj);

            dataChanged?.Invoke();
        }

        void OnDragEnd()
        {
            m_DropZone.RemoveFromClassList("accept-drag");
        }

        void OnDragStart()
        {
            m_DropZone.AddToClassList("accept-drag");
        }

        static void BindPatternItemView(VisualElement el, int idx)
        {
            if (idx >= k_PatternTextures.Length || idx < 0)
                return;

            // TODO: Use PreviewImage instead of Image
            el.Q<Image>().image = k_PatternTextures[idx];
            el.userData = k_ProvidedPatternsBase64Encoded[idx];
        }

        static VisualElement MakePatternItemView()
        {
            var itemView = new VisualElement();
            itemView.AddToClassList("muse-patterns-view__item");

            // TODO: Use PreviewImage instead of Image
            var image = new Image();
            image.AddToClassList("muse-patterns-view__image");
            itemView.Add(image);

            return itemView;
        }

        static string actionKeyLabel =>
            Application.platform is RuntimePlatform.OSXEditor or
                RuntimePlatform.OSXPlayer or
                RuntimePlatform.OSXServer ? "Cmd" : "Ctrl";

        void OnPatternChosen(IEnumerable<object> selection, bool dismissPopup)
        {
            using var selectionEnumerator = selection.GetEnumerator();

            if (selectionEnumerator.MoveNext() && selectionEnumerator.Current is Texture2D tex2D)
            {
                SetGuidWithoutNotify(null);
                OnChosenItemLoaded(tex2D, dismissPopup);
            }
        }

        void OnChosenItemLoaded(Texture2D preview, bool dismissPopup)
        {
            SetShapeImageWithoutNotify(preview);
            dataChanged?.Invoke();

            if (dismissPopup)
                m_PatternsPopover?.Dismiss(DismissType.Action);
        }

        void OnPatternsButtonClicked()
        {
            m_PatternsPopover?.Dismiss(DismissType.Consecutive);

            m_PatternsPopover = Popover
                .Build(m_PatternsButton, m_PatternsView)
                .SetAnchor(m_PatternsButton)
                .SetPlacement(PopoverPlacement.BottomStart)
                .SetArrowVisible(false)
                .SetCrossOffset(-8);

            m_PatternsPopover.Show();
        }

        void OnImportButtonClicked()
        {
#if UNITY_EDITOR
            string lastFolderPath = Preferences.lastImportFolderPath;
            if (!Directory.Exists(lastFolderPath))
                lastFolderPath = Preferences.defaultImportFolderPath;

            var path = UnityEditor.EditorUtility.OpenFilePanelWithFilters(
                TextContent.importImages,
                lastFolderPath,
                new[]
                {
                    "Image",
                    "png,jpg,jpeg"
                });
            if (string.IsNullOrEmpty(path))
                return;

            Preferences.lastImportFolderPath = Path.GetDirectoryName(path);
            var img = new Texture2D(2, 2);
            img.LoadImage(System.IO.File.ReadAllBytes(path));
            SetGuidWithoutNotify(null);
            if (m_Mode == ReferenceOperator.Mode.Color)
                SetColorImageWithoutNotify(img);
            else
                SetShapeImageWithoutNotify(img);
            dataChanged?.Invoke();
#else
            Debug.LogError("Importing images is not supported in builds");
#endif
        }

        void OnIntValueChanged(ChangeEvent<int> evt)
        {
            dataChanged?.Invoke();
        }
        
#if USE_APPUI_COLORPICKER
        void OnPickColorButtonClicked()
        {
            m_PickerPopup?.Dismiss(DismissType.Consecutive);
            
            m_Picker ??= new ColorPicker();
            
            var previousValue = m_PickedColor ?? s_LastPickedColor;   
            m_Picker.previousValue = previousValue;
            m_Picker.SetValueWithoutNotify(previousValue);

            m_Picker.RegisterValueChangedCallback(OnPicking);

            m_PickerPopup = Popover.Build(m_PickColorButton, m_Picker);
            m_PickerPopup.dismissed += OnPickerPopupDismissed;
            m_PickerPopup.SetPlacement(PopoverPlacement.EndTop);
            
            m_PickerPopup.Show();
            
            OnPicking(previousValue);
        }

        void OnPickerPopupDismissed(Popover popup, DismissType reason)
        {
            popup.dismissed -= OnPickerPopupDismissed;
            
            if (m_Picker != null)
            {
                s_LastPickedColor = m_Picker.value;
                m_Picker.UnregisterValueChangedCallback(OnPicking);
            }
        }

        void OnPicking(ChangeEvent<Color> evt)
        {
            OnPicking(evt.newValue);
        }
#else
        void OnPickColorButtonClicked()
        {
#if UNITY_EDITOR
            // Find UnityEditor.ColorPicker.Show(GUIView viewToUpdate, Action<Color> colorChangedCallback, Color col, bool showAlpha, bool hdr) using C# Reflection
            var colorPickerType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.ColorPicker");
            foreach (var methodInfo in colorPickerType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (methodInfo.Name == "Show" && methodInfo.GetParameters().Length == 5)
                {
                    var colorChangedCallback = (Action<Color>)Delegate.CreateDelegate(typeof(Action<Color>), this, nameof(OnColorPickerColorChanged));
                    methodInfo.Invoke(null, new object[] { null, colorChangedCallback, m_PickedColor[(int)m_Mode] ?? Color.black, false, false });
                    break;
                }
            }
#endif
        }

        void OnColorPickerColorChanged(Color color)
        {
            s_LastPickedColor = color;
            OnPicking(color);
        }
        
#endif
        
        void OnPicking(Color color)
        {
            m_PickedColor[(int)m_Mode] = color;
            if (m_Mode == ReferenceOperator.Mode.Color)
            {
                if (!m_PickingTex)
                    m_PickingTex = new Texture2D(512, 512);
                m_PickingTex.SetPixels(Enumerable.Repeat(color, 512 * 512).ToArray());
                m_PickingTex.Apply();
            
                SetGuidWithoutNotify(null);
                SetColorImageWithoutNotify(m_PickingTex);
            }
            else
            {
                UpdatePreviewImage();
            }
            
            dataChanged?.Invoke(); 
        }

        void OnClearButtonClicked()
        {
            OnClearColorButtonClicked(false);
            
            SetGuidWithoutNotify(null);
            if (m_Mode == ReferenceOperator.Mode.Color)
            {
                SetColorImageWithoutNotify(null);
            }
            else
            {
                SetShapeImageWithoutNotify(null);
                RefreshPreview();
            }
            
            dataChanged?.Invoke();
        }
        
        void OnClearColorButtonClicked(bool notify = true)
        {
            m_PickedColor[(int)m_Mode] = null;
            if (!notify) return;
            
            dataChanged?.Invoke();
            RefreshPreview();
        }

        void OnModeChanged(IEnumerable<int> indices)
        {
            using var enumerator = indices.GetEnumerator();
            enumerator.MoveNext();

            var mode = (ReferenceOperator.Mode)enumerator.Current;
            SetModeWithoutNotify(mode);
            dataChanged?.Invoke();
        }

        internal void SetModeWithoutNotify(ReferenceOperator.Mode mode)
        {
            m_Mode = mode;
            m_ModeGroup.SetSelectionWithoutNotify(new []{(int)m_Mode});
            m_PatternsButton.EnableInClassList(Styles.hiddenUssClassName, m_Mode != ReferenceOperator.Mode.Shape);
            m_PickColorButton.EnableInClassList(Styles.hiddenUssClassName, m_Mode != ReferenceOperator.Mode.Color);
            RefreshPreview();
        }

        internal ReferenceOperator.Mode GetMode()
        {
            return m_Mode;
        }

        internal void SetGuidWithoutNotify(string guid)
        {
            m_Guid = guid;
        }

        internal void SetColorImageWithoutNotify(Texture2D img)
        {
            if (!Validate(img))
                return;

            m_ColorImage = Resize(img);

            RefreshPreview();
        }

        static Texture2D Resize(Texture2D texture2D)
        {
            if (texture2D == null)
                return null;

            // If the image is square at 512x152 or smaller it should remain as is.
            if (texture2D.IsSquare() && texture2D.width <= k_MaxTextureSize)
            {
                return texture2D;
            }

            //If the image is square but at a higher resolution it should be downscaled to 512x512
            if (texture2D.IsSquare())
            {
                return texture2D.ResizeTexture(k_MaxTextureSize, k_MaxTextureSize);
            }

            var minSize = Math.Min(texture2D.width, texture2D.height);

            //If the image is not square with its smaller dimension less or equal to 512,
            //it should be center cropped to be square using it smaller dimension.
            if (minSize <= k_MaxTextureSize)
            {
                return texture2D.CropTextureCenter(minSize, minSize);
            }

            // If the image is not square with its smaller dimension greater than 512,
            // it should be first downscaled so that its smaller dimension is 512 than center cropped
            //     to be square at 512x512.

            var ratio = (float)texture2D.width / texture2D.height;
            var textureWidth = k_MaxTextureSize;
            var textureHeight = k_MaxTextureSize;

            if (ratio < 1)
            {
                textureHeight = (int)(texture2D.height * ratio);
            }
            else
            {
                textureWidth = (int)(texture2D.width * ratio);
            }

            var texture2DResized = texture2D.ResizeTexture(textureWidth, textureHeight);
            return texture2DResized.CropTextureCenter(k_MaxTextureSize, k_MaxTextureSize);
        }

        internal void SetShapeImageWithoutNotify(Texture2D img)
        {
            if (!Validate(img))
                return;

            m_ShapeImage = img;
            RefreshPreview();
        }

        static bool Validate(Texture2D img)
        {
            if (img && !img.isReadable)
            {
                Debug.LogError("<b>[Muse]</b> Input image must be readable, please enable read/write in the import settings");
                return false;
            }

            if (img && IsTextureCompressed(img))
            {
                Debug.LogError($"<b>[Muse]</b> Input image must be not be compressed. Please remove compression from the import settings.");
                return false;
            }

            return true;
        }

        static bool IsTextureCompressed(Texture2D texture)
        {
            var format = texture.format;

            switch (format)
            {
                case TextureFormat.DXT1:
                case TextureFormat.DXT5:
                case TextureFormat.PVRTC_RGB2:
                case TextureFormat.PVRTC_RGBA2:
                case TextureFormat.PVRTC_RGB4:
                case TextureFormat.PVRTC_RGBA4:
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ASTC_4x4:
                    return true;
                default:
                    return false;
            }
        }

        void RefreshPreview()
        {
            switch (m_Mode)
            {
                case ReferenceOperator.Mode.Color:
                    m_PreviewImage.image = GetColorImage();
                    break;
                case ReferenceOperator.Mode.Shape:
                {
                    UpdatePreviewImage();
                }
                    break;
                default:
                    m_PreviewImage.image = null;
                    break;
            }

            m_DropzoneMessage.text = m_Mode switch
            {
                ReferenceOperator.Mode.Color => TextContent.dragAndDropColorImageMessage,
                ReferenceOperator.Mode.Shape => TextContent.dragAndDropShapeImageMessage,
                _ => ""
            };
            // TODO: Show DropZone helper in Shape mode when no image is set
            m_DropZoneHelper.EnableInClassList(Styles.hiddenUssClassName, /*m_Mode == ReferenceOperator.Mode.Shape ||*/ m_PreviewImage.image);
            m_ClearButton.SetEnabled(m_PreviewImage.image);
            m_StrengthSlider.EnableInClassList(Styles.hiddenUssClassName, m_Mode != ReferenceOperator.Mode.Color && !m_PreviewImage.image);
            m_ColorField.EnableInClassList(Styles.hiddenUssClassName, m_Mode == ReferenceOperator.Mode.Color || (m_Mode != ReferenceOperator.Mode.Color && !m_PreviewImage.image));
        }

        internal string GetGuid()
        {
            return m_Guid;
        }

        internal Texture2D GetColorImage()
        {
            return m_ColorImage;
        }

        internal Texture2D GetShapeImage()
        {
            return m_ShapeImage;
        }

        internal void SetStrengthWithoutNotify(int strength)
        {
            m_StrengthSlider.SetValueWithoutNotify(strength);
        }
        
        internal void SetColorWithoutNotify(Color? color)
        {
            m_PickedColor[(int)m_Mode] = color;
        }

        internal int GetStrength()
        {
            return m_StrengthSlider.value;
        }

        void ResizeDropZone(GeometryChangedEvent evt)
        {
            var dropZone = (VisualElement)evt.target;
            var size = dropZone.resolvedStyle.width;

            if (!Mathf.Approximately(dropZone.resolvedStyle.height, size))
                dropZone.style.height = size;
        }

        public Color? GetColor()
        {
            if (m_Mode == ReferenceOperator.Mode.Color)
            {
                return m_PickedColor[(int)m_Mode] ?? s_LastPickedColor; 
            }
            else
            {
                return m_PickedColor[(int)m_Mode];
            }
        }

        void UpdatePreviewImage()
        {
            if(m_PickedColor[(int)m_Mode] != null)
                m_ColorField.value = m_PickedColor[(int)m_Mode].Value;
            else
                m_ColorField.Clear(false);
            
            var currentImage = m_PreviewImage.image;

            if (currentImage != null)
            {
                m_PreviewImage.image = null;
                RenderTexture.ReleaseTemporary(currentImage as RenderTexture);
            }
                
            var refImage = GetShapeImage();

            if (refImage == null)
            {
                m_PreviewImage.image = null;
                return;
            }
            
            var activeRT = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(refImage.width, refImage.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            rt.Create();
            var mat = new Material(Shader.Find("Hidden/Muse/ShapePreview"));
            mat.SetColor(Shader.PropertyToID("_ReferenceColor"), m_PickedColor[(int)m_Mode] ?? Color.black);
            Graphics.Blit(refImage, rt, mat);
            m_PreviewImage.image = rt;
            mat.SafeDestroy();
            RenderTexture.active = activeRT;
        }
    }
}