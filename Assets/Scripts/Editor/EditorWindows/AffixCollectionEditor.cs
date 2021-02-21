using System;
using System.Collections.Generic;
using System.Linq;
using Runtime.Helpers;
using Runtime.NewStuff;
using UnityEditor;
using UnityEngine;

namespace Ayaya
{
    public class AffixCollectionEditor : EditorWindow
    {
        private AffixCollection _target;
        private AffixCollection _newTarget;

        private string _searchFieldText = "";
        private Vector2 _affixListScrollPosition;
        private Font _consoleFont;
        private const int SidebarWidth = 300;
        private Texture2D _verticalLineTex;
        private Texture2D _lightTex;
        private Texture2D _darkTex;
        private Texture2D _blueTex;
        private int _selectedAffixIdx;

        private string[] traitVersionTypes;
        private string[] traitCollectionsStrings;
        private string[][] availableTraitsStrings;

        private int previewPowerLevel;
        private int previewVarianceRoll;

        [MenuItem("Data/Affix Collection Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<AffixCollectionEditor>();
            window.titleContent = new GUIContent("Affix Collection Editor");
            window.Show();
        }

        public static void ShowWindow(AffixCollection collection)
        {
            var window = GetWindow<AffixCollectionEditor>();
            window.titleContent = new GUIContent("Affix Collection Editor");
            window._newTarget = collection;
            window.Show();
        }

        private void OnEnable()
        {
            _verticalLineTex = TextureHelper.MakeTex(2, 2, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            _lightTex = TextureHelper.MakeTex(2, 2, new Color(0f, 0f, 0f, 0.05f));
            _darkTex = TextureHelper.MakeTex(2, 2, new Color(0f, 0f, 0f, 0.1f));
            _blueTex = TextureHelper.MakeTex(2, 2, new Color(44f / 255f, 93 / 255f, 135f / 255f, 1f));
            _consoleFont = Font.CreateDynamicFontFromOSFont("Consolas", 11);

            traitVersionTypes = Enum.GetNames(typeof(TraitVersion));
        }

        private void Update()
        {
            if (_newTarget != _target)
            {
                _target = _newTarget;

                if (_target != null)
                {
                    EditorUtility.SetDirty(_target);
                }
            }
        }

        private void RefreshTraitCollectionStrings()
        {
            traitCollectionsStrings = new string[_target.traitCollectionDictionary.traitCollections.Length];

            for (int i = 0; i < _target.traitCollectionDictionary.traitCollections.Length; i++)
            {
                if (_target.traitCollectionDictionary.traitCollections[i] == null ||
                    _target.traitCollectionDictionary.traitCollections?[i].traits == null ||
                    _target.traitCollectionDictionary.traitCollections[i].traits.Length <= 0)
                {
                    continue;
                }

                var collectionName = _target.traitCollectionDictionary.traitCollections[i].name;
                traitCollectionsStrings[i] = collectionName;
            }
        }

        private void RefreshAvailableTraitsStrings()
        {
            availableTraitsStrings = new string[_target.traitCollectionDictionary.traitCollections.Length][];
            for (int i = 0; i < _target.traitCollectionDictionary.traitCollections.Length; i++)
            {
                if (_target.traitCollectionDictionary.traitCollections[i] == null ||
                    _target.traitCollectionDictionary.traitCollections?[i].traits == null ||
                    _target.traitCollectionDictionary.traitCollections[i].traits.Length <= 0)
                {
                    continue;
                }

                availableTraitsStrings[i] = new string[_target.traitCollectionDictionary.traitCollections[i].traits.Length];

                for (int y = 0; y < _target.traitCollectionDictionary.traitCollections[i].traits.Length; y++)
                {
                    var traitName = _target.traitCollectionDictionary.traitCollections[i].traits[y]?.name;
                    availableTraitsStrings[i][y] = traitName ?? "null";
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            _newTarget = EditorGUILayout.ObjectField(_newTarget, typeof(AffixCollection), false, GUILayout.Width(350)) as AffixCollection;

            if (_target)
            {
                if (GUILayout.Button("Save", GUILayout.Width(100)))
                {
                    EditorUtility.SetDirty(_target);
                    AssetDatabase.SaveAssets();
                }
            }

            GUILayout.EndHorizontal();

            if (_target)
            {
                GUILayout.BeginHorizontal();
                _target.traitCollectionDictionary =
                    EditorGUILayout.ObjectField(_target.traitCollectionDictionary, typeof(TraitCollectionDictionary), false, GUILayout.Width(350)) as
                        TraitCollectionDictionary;

                if (GUILayout.Button("Refresh", GUILayout.Width(100)))
                {
                    RefreshTraitCollectionStrings();
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                if (_target.traitCollectionDictionary)
                {
                    OnAffixTraits();
                }
            }
        }

        private void OnAffixTraits()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(SidebarWidth));
            OnSidebar();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            OnGUIInspector();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        #region Sidebar

        private void OnSidebar()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Search:");
            _searchFieldText = EditorGUILayout.DelayedTextField(_searchFieldText, GUILayout.Width(SidebarWidth - 20));
            GUILayout.Space(5);

            OnGUITraitList();
            GUILayout.EndVertical();
        }

        private void OnGUITraitList()
        {
            _affixListScrollPosition = GUILayout.BeginScrollView(_affixListScrollPosition);

            var baseStyle = new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Clip,
                margin = new RectOffset(0, 0, 0, 0),
                font = _consoleFont, fontSize = 12,
                padding = new RectOffset(0, 0, 2, 2),
                fixedHeight = 20
            };

            var lightStyle = new GUIStyle(baseStyle);
            lightStyle.normal.background = _lightTex;

            var darkStyle = new GUIStyle(baseStyle);
            darkStyle.normal.background = _darkTex;

            var selectedStyle = new GUIStyle(baseStyle);
            selectedStyle.normal.background = _blueTex;

            for (int i = 0; i < _target.affixes.Length; i++)
            {
                if (_target.affixes[i] == null)
                {
                    _target.affixes[i] = new Affix();
                }

                var name = $"[{i:D3}] " + _target.affixes[i].name;

                if (!name.ToLower().Contains(_searchFieldText.ToLower()) && _searchFieldText?.Length > 0) continue;

                var idx = i;

                var style = idx % 2 == 0 ? lightStyle : darkStyle;
                var actualStyle = idx == _selectedAffixIdx ? selectedStyle : style;

                if (GUILayout.Button(name, actualStyle, GUILayout.MinWidth(SidebarWidth - 20), GUILayout.ExpandWidth(true)))
                {
                    _selectedAffixIdx = idx;
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);

            if (GUILayout.Button("Change Maximum ...", GUILayout.Width(SidebarWidth - 20)))
            {
                ChangeAffixCollectionMaximumModal.Show(ref _target);
                GUIUtility.ExitGUI();
            }
        }

        #endregion

        private void OnGUIInspector()
        {
            if (_selectedAffixIdx >= 0 && _selectedAffixIdx <= _target.affixes.Length && _target.affixes.Length > 0)
            {
                _target.affixes[_selectedAffixIdx].isLocked = EditorGUILayout.ToggleLeft("Lock", _target.affixes[_selectedAffixIdx].isLocked);

                if (_target.affixes[_selectedAffixIdx].isLocked)
                {
                    OnGuiAffixIsLocked();
                    return;
                }


                OnGuiName();
                OnGuiAffixType();

                if (_target.traitCollectionDictionary.traitCollections != null && _target.traitCollectionDictionary.traitCollections.Length > 0)
                {
                    if (traitCollectionsStrings == null || traitCollectionsStrings.Length != _target.traitCollectionDictionary.traitCollections.Length)
                    {
                        RefreshTraitCollectionStrings();
                    }

                    var collectionIdx = _target.affixes[_selectedAffixIdx].traitCollectionIdx;
                    var collectionsArr = _target.traitCollectionDictionary.traitCollections;

                    if (collectionIdx < 0 || collectionIdx >= collectionsArr.Length)
                    {
                        _target.affixes[_selectedAffixIdx].traitCollectionIdx = 0;
                        collectionIdx = 0;
                    }

                    OnGuiSelectCollectionIndex();

                    if (_target.traitCollectionDictionary.traitCollections[collectionIdx]?.traits != null &&
                        _target.traitCollectionDictionary.traitCollections[collectionIdx].traits.Length > 0)
                    {
                        if (availableTraitsStrings == null ||
                            availableTraitsStrings[collectionIdx].Length != _target.traitCollectionDictionary.traitCollections.Length)
                        {
                            RefreshAvailableTraitsStrings();
                        }

                        if (availableTraitsStrings[collectionIdx] == null ||
                            availableTraitsStrings[collectionIdx].Length != _target.traitCollectionDictionary.traitCollections[collectionIdx].traits.Length)
                        {
                            RefreshAvailableTraitsStrings();
                        }

                        var traitIdx = _target.affixes[_selectedAffixIdx].traitIdx;

                        if (traitIdx < 0 || traitIdx >= _target.traitCollectionDictionary.traitCollections[collectionIdx].traits.Length)
                        {
                            _target.affixes[_selectedAffixIdx].traitIdx = 0;
                        }

                        OnGuiSelectTraitIndex();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Variance:");
                        _target.affixes[_selectedAffixIdx].variance =
                            EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].variance, GUILayout.Width(35));
                        GUILayout.Label((_target.affixes[_selectedAffixIdx].variance * 100f) + "%");
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.Space(5);

                        OnGuiSelectVersion();

                        OnGuiValueInput();
                        OnGuiInputReadableFormat();
                        OnGuiReadablePreview();
                    }
                }
            }
        }

        private void OnGuiName()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:");
            _target.affixes[_selectedAffixIdx].name = GUILayout.TextField(_target.affixes[_selectedAffixIdx].name, 32, GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void OnGuiAffixType()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Affix Type:");
            _target.affixes[_selectedAffixIdx].affixType =
                (AffixType) EditorGUILayout.EnumPopup(_target.affixes[_selectedAffixIdx].affixType, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void OnGuiSelectCollectionIndex()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Trait Collection:");
            _target.affixes[_selectedAffixIdx].traitCollectionIdx =
                EditorGUILayout.Popup(_target.affixes[_selectedAffixIdx].traitCollectionIdx, traitCollectionsStrings, GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void OnGuiSelectTraitIndex()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Trait:");
            _target.affixes[_selectedAffixIdx].traitIdx =
                EditorGUILayout.Popup(_target.affixes[_selectedAffixIdx].traitIdx,
                    availableTraitsStrings[_target.affixes[_selectedAffixIdx].traitCollectionIdx], GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void OnGuiSelectVersion()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Value Type:");
            var versions = _target.traitCollectionDictionary.traitCollections[_target.affixes[_selectedAffixIdx].traitCollectionIdx]
                .traits[_target.affixes[_selectedAffixIdx].traitIdx].versions;
            var enums = Trait.ReturnSelectedElements(versions);

            List<string> enumStrings = new List<string>();

            for (int i = 0; i < enums.Count; i++)
            {
                enumStrings.Add(enums[i].ToString());
            }

            int idx = enums.FindIndex(e => e == _target.affixes[_selectedAffixIdx].traitVersion);
            if (idx < 0 || idx >= enums.Count)
            {
                idx = 0;
                _target.affixes[_selectedAffixIdx].traitVersion = enums[0];
            }

            idx = EditorGUILayout.Popup(idx, enumStrings.ToArray());

            var traitVersion = (TraitVersion) Enum.Parse(typeof(TraitVersion), enumStrings[idx]);

            if (_target.affixes[_selectedAffixIdx].traitVersion != traitVersion)
            {
                _target.affixes[_selectedAffixIdx].traitVersion = traitVersion;
            }

            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.gray;
            GUILayout.Label(_target.affixes[_selectedAffixIdx].traitVersion.ToString(), style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void OnGuiValueInput()
        {
            switch (_target.affixes[_selectedAffixIdx].traitVersion)
            {
                case TraitVersion.AddsRemovesFixedFlat:
                    OnGuiAddsRemoveFixedValues();
                    break;
                case TraitVersion.AddsRemovesFixedPercentage:
                    OnGuiAddsRemoveFixedValues();
                    break;
                case TraitVersion.AddsRemovesRangeFlat:
                    OnGuiAddsRemovesRangeFlatValues();
                    break;
                case TraitVersion.IncreasesReducesFixedPercentage:
                    OnGuiAddsRemoveFixedValues();
                    break;
                case TraitVersion.MoreLessFixedPercentage:
                    OnGuiAddsRemoveFixedValues();
                    break;
            }
        }

        private void OnGuiAddsRemoveFixedValues()
        {
            if (_target.affixes[_selectedAffixIdx].valuesPowerMin == null ||
                _target.affixes[_selectedAffixIdx].valuesPowerMin.Length != 1)
            {
                _target.affixes[_selectedAffixIdx].valuesPowerMin = new float[1];
            }

            GUILayout.BeginHorizontal();


            GUILayout.Label("Power (0):");
            _target.affixes[_selectedAffixIdx].valuesPowerMin[0] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMin[0], GUILayout.Width(40));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_target.affixes[_selectedAffixIdx].valuesPowerMax == null ||
                _target.affixes[_selectedAffixIdx].valuesPowerMax.Length != 1)
            {
                _target.affixes[_selectedAffixIdx].valuesPowerMax = new float[1];
            }

            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Power (60):");
            _target.affixes[_selectedAffixIdx].valuesPowerMax[0] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMax[0], GUILayout.Width(40));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void OnGuiAddsRemovesRangeFlatValues()
        {
            if (_target.affixes[_selectedAffixIdx].valuesPowerMin == null ||
                _target.affixes[_selectedAffixIdx].valuesPowerMin.Length != 2)
            {
                _target.affixes[_selectedAffixIdx].valuesPowerMin = new float[2];
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power (0):");
            _target.affixes[_selectedAffixIdx].valuesPowerMin[0] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMin[0], GUILayout.Width(40));
            GUILayout.Label("to");
            _target.affixes[_selectedAffixIdx].valuesPowerMin[1] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMin[1], GUILayout.Width(40));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_target.affixes[_selectedAffixIdx].valuesPowerMax == null ||
                _target.affixes[_selectedAffixIdx].valuesPowerMax.Length != 2)
            {
                _target.affixes[_selectedAffixIdx].valuesPowerMax = new float[2];
            }

            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Power (60):");
            _target.affixes[_selectedAffixIdx].valuesPowerMax[0] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMax[0], GUILayout.Width(40));
            GUILayout.Label("to");
            _target.affixes[_selectedAffixIdx].valuesPowerMax[1] =
                EditorGUILayout.FloatField(_target.affixes[_selectedAffixIdx].valuesPowerMax[1], GUILayout.Width(40));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void OnGuiInputReadableFormat()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Readable Format:");
            _target.affixes[_selectedAffixIdx].readableFormat = GUILayout.TextField(_target.affixes[_selectedAffixIdx].readableFormat, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void OnGuiReadablePreview()
        {


            var readablePreview = _target.affixes[_selectedAffixIdx].readableFormat;

            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.yellow;
            GUILayout.Label("Preview:");

            GUILayout.BeginHorizontal();
            previewPowerLevel = EditorGUILayout.IntSlider(previewPowerLevel, 0, 60, GUILayout.Width(150));
            GUILayout.Label("Power");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            previewVarianceRoll = EditorGUILayout.IntSlider(previewVarianceRoll, 0, 100, GUILayout.Width(150));
            GUILayout.Label("Variance Roll");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            var value = _target.affixes[_selectedAffixIdx].GetValueRolledAndScaled(previewVarianceRoll, previewPowerLevel);

            try
            {
                if (_target.affixes[_selectedAffixIdx].valuesPowerMin.Length == 1 &&
                    readablePreview.Contains("{0}"))
                {
                    string valueString = value[0].ToString();

                    if (_target.affixes[_selectedAffixIdx].traitVersion == TraitVersion.AddsRemovesFixedPercentage ||
                        _target.affixes[_selectedAffixIdx].traitVersion == TraitVersion.IncreasesReducesFixedPercentage ||
                        _target.affixes[_selectedAffixIdx].traitVersion == TraitVersion.MoreLessFixedPercentage)
                    {
                        valueString = (value[0] * 100f) + "%";
                    }

                    GUILayout.Label(String.Format(readablePreview, valueString), style);
                }

                if (_target.affixes[_selectedAffixIdx].valuesPowerMin.Length == 2 &&
                    readablePreview.Contains("{0}") &&
                    readablePreview.Contains("{1}"))
                {
                    GUILayout.Label(String.Format(readablePreview, value[0],
                        value[1]), style);
                }
            }
            catch (FormatException e)
            {
                return;
            }
        }

        private void OnGuiAffixIsLocked()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.grey;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:");
            GUILayout.Label(_target.affixes[_selectedAffixIdx].name, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);


            GUILayout.BeginHorizontal();
            GUILayout.Label("Affix Type:");
            GUILayout.Label(_target.affixes[_selectedAffixIdx].affixType.ToString(), style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Collection:");
            GUILayout.Label(_target.affixes[_selectedAffixIdx].traitCollectionIdx.ToString(), style);
            GUILayout.Label("=> " + _target.traitCollectionDictionary.traitCollections[_target.affixes[_selectedAffixIdx].traitCollectionIdx].ToString(), style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Trait:");
            GUILayout.Label(_target.affixes[_selectedAffixIdx].traitIdx.ToString(), style);
            GUILayout.Label(
                "=> " + _target.traitCollectionDictionary.traitCollections[_target.affixes[_selectedAffixIdx].traitCollectionIdx]
                    .traits[_target.affixes[_selectedAffixIdx].traitIdx].name, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Variance:");
            GUILayout.Label((_target.affixes[_selectedAffixIdx].variance * 100f) + "%", style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Readable Format:");
            GUILayout.Label(_target.affixes[_selectedAffixIdx].readableFormat, style);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            
            OnGuiReadablePreview();
        }
    }
}