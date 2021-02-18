using System;
using System.Linq;
using Runtime.NewStuff;
using UnityEditor;
using UnityEngine;

namespace Ayaya
{
    public class TraitCollectionEditor : EditorWindow
    {
        private TraitCollection target;
        private TraitCollection newTarget;
        private bool arrayChanged = false;

        private string searchFieldText;

        private int selectedTraitIdx;
        private string[] traitStringList = Array.Empty<string>();
        private Vector2 traitListScrollPosition;


        [MenuItem("Traits/Trait Collection Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<TraitCollectionEditor>();
            window.titleContent = new GUIContent("Trait Collection Editor");
            window.Show();
        }

        private void Update()
        {
            
            if (target != null && target.Traits != null && traitStringList.Length != target.Traits.Length)
            {
                arrayChanged = true;
            }
            
            if (newTarget != target)
            {
                target = newTarget;
                arrayChanged = true;
            }

            if (arrayChanged)
            {
                if (newTarget != null)
                {
                    traitStringList = new string[newTarget.Traits.Count()];
                    for (int i = 0; i < newTarget.Traits.Count(); i++)
                    {
                        traitStringList[i] = $"[{i}] " + newTarget.Traits[i].ReadableFormat;
                    }
                }
            }
        }

        private void OnGUI()
        {
            newTarget = (TraitCollection) EditorGUILayout.ObjectField(target, typeof(TraitCollection), false);
            
            if (target)
            {
                OnGUITraitCollectionEditor();
            }
        }

        private void OnGUITraitCollectionEditor()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            OnGUISidebar();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }

        private void OnGUISidebar()
        {
            if (GUILayout.Button("Save"))
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
            
            searchFieldText = EditorGUILayout.DelayedTextField(searchFieldText);
            OnGUITraitList();
        }

        private void OnGUITraitList()
        {
           
            traitListScrollPosition = GUILayout.BeginScrollView(traitListScrollPosition);
            var fontStyle = new GUIStyle( GUI.skin.button );
            fontStyle.alignment = TextAnchor.MiddleLeft;
            selectedTraitIdx = GUILayout.SelectionGrid(selectedTraitIdx, traitStringList, 1, fontStyle);
            GUILayout.EndScrollView();

            if (GUILayout.Button("Change Maximum"))
            {
                ChangeMaximumModalUtility.Show(ref target);
                GUIUtility.ExitGUI();
            }
            
        }

    }
}