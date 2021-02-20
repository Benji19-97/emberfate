using Ayaya;
using Runtime.NewStuff;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Editor
{
    public class CustomAssetHandler
    {
        [OnOpenAsset(1)]
            public static bool step1(int instanceID, int line)
            {
                if (Selection.activeObject as TraitCollection != null) {
                    TraitCollectionEditor.ShowWindow(Selection.activeObject as TraitCollection);
                    return true; 
                }

                return false;
            }
    }
}