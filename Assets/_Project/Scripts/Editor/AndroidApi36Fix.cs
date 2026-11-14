
using UnityEditor;
using UnityEngine;

public static class AndroidApi36Fix
{
    [MenuItem("Tools/BlockPuzzle/Android/Set Target API 36")]
    public static void SetTargetApi36()
    {
        PlayerSettings.Android.targetSdkVersion =
            AndroidSdkVersions.AndroidApiLevel36;

        AssetDatabase.SaveAssets();

        Debug.Log(
            "[Android Release] Active Target API: " +
            PlayerSettings.Android.targetSdkVersion);
    }
}
