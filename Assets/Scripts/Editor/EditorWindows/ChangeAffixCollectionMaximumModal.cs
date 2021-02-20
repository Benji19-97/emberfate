using System;
using Runtime.NewStuff;
using UnityEditor;
using UnityEngine;

namespace Ayaya
{
    public class ChangeAffixCollectionMaximumModal : EditorWindow
    {
        public AffixCollection collection;
        public int size;
        
        public static void Show(ref AffixCollection affixCollection)
        {
            var window = ScriptableObject.CreateInstance(typeof(ChangeAffixCollectionMaximumModal)) as ChangeAffixCollectionMaximumModal;
            window.titleContent = new GUIContent("Change Maximum");
            window.collection = affixCollection;
            window.size = affixCollection.affixes.Length;
            window.minSize = new Vector2(200, 75);
            window.maxSize = new Vector2(200, 75);
            window.ShowModalUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Maximum:");
            size = EditorGUILayout.IntField(size);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                Array.Resize(ref collection.affixes, Mathf.Clamp(size, 0, 9999));
                Close();
            }

            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}