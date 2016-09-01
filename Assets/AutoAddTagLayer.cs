using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class AutoAddTagLayer : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string s in importedAssets)
        {
            if (s.Equals("Assets/AutoAddTagLayer.cs"))
            {
                List<string> tags = new List<string>() {"Grid", "Enemy", "Test1"};
                AddTag(tags);

                List<string> layers = new List<string>() { "name:Grid", "Character", "Effect", "UI", "Test2" };
                AddLayer(layers);

                return;
            }
        }
    }

    static void AddTag(List<string> tags)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty it = tagManager.GetIterator();
        while (it.NextVisible(true))
        {
            if (it.name == "tags")
            {
                foreach (string tag in tags)
                {
                    if (!IsHasTag(tag))
                    {
                        it.InsertArrayElementAtIndex(it.arraySize);
                        it.GetArrayElementAtIndex(it.arraySize - 1).stringValue = tag;
                    }
                }
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
    }

    static bool IsHasTag(string tag)
    {
        for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.tags.Length; i++)
        {
            if (UnityEditorInternal.InternalEditorUtility.tags[i].Contains(tag))
                return true;
        }
        return false;
    }

    static void AddLayer(List<string> layers)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty it = tagManager.GetIterator();
        while (it.NextVisible(true))
        {
            if (it.name == "m_SortingLayers")
            {
                foreach (string layer in layers)
                    if (!IsHasLayer(layer))
                        if (it.type == "string")
                            if (string.IsNullOrEmpty(it.stringValue))
                                it.stringValue = layer;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
    }

    static bool IsHasLayer(string layer)
    {
        for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.layers.Length; i++)
        {
            if (UnityEditorInternal.InternalEditorUtility.layers[i].Contains(layer))
                return true;
        }
        return false;
    }
}
