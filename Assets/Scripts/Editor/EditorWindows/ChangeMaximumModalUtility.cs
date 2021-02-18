using System;
using Runtime.NewStuff;
using UnityEditor;
using UnityEngine;

namespace Ayaya
{
    public class ChangeMaximumModalUtility : EditorWindow
    {
        public TraitCollection collection;
        public int size;
        
        public static void Show(ref TraitCollection traitCollection)
        {
            var window = ScriptableObject.CreateInstance(typeof(ChangeMaximumModalUtility)) as ChangeMaximumModalUtility;
            window.titleContent = new GUIContent("Change Maximum");
            window.collection = traitCollection;
            window.size = traitCollection.Traits.Length;
            window.ShowModalUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Maximum:");
            size = EditorGUILayout.IntField(size);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                Array.Resize(ref collection.Traits, size);
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