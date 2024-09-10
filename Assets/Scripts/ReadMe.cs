using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "NewReadMe", menuName = "Custom/ReadMe")]
public class ReadMe : ScriptableObject
{
    [HideInInspector]
    [SerializeField]
    private string content = "This is the default content of your ReadMe.";

    public string GetContent()
    {
        return content;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ReadMe))]
public class ReadMeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ReadMe readme = (ReadMe)target;
        EditorGUILayout.LabelField("Content", readme.GetContent(), EditorStyles.wordWrappedLabel);
    }
}
#endif