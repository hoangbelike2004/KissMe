using UnityEngine;
using UnityEditor;

// [QUAN TRỌNG] Tham số 'true' ở đây sẽ kích hoạt Editor cho cả các class con
[CustomEditor(typeof(Winzone), true)]
[CanEditMultipleObjects]
public class WinZoneScriptEditor : Editor
{
    SerializedProperty winzoneTypeProp;
    SerializedProperty vfxPoolProp;
    SerializedProperty isSpecialProp;
    SerializedProperty targetTagProp;
    SerializedProperty isGoalProp;
    SerializedProperty isFollowProp;
    SerializedProperty isWinningObjectProp;

    void OnEnable()
    {
        // Link các biến (Unity sẽ tự tìm biến trong class cha nếu class con không có)
        winzoneTypeProp = serializedObject.FindProperty("winzoneType");
        vfxPoolProp = serializedObject.FindProperty("VFX_Pool");
        isSpecialProp = serializedObject.FindProperty("isSpecial");
        targetTagProp = serializedObject.FindProperty("targetTag");
        isGoalProp = serializedObject.FindProperty("isGoal");
        isFollowProp = serializedObject.FindProperty("isFollow");
        isWinningObjectProp = serializedObject.FindProperty("isWinningObject");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Vẽ tất cả các biến (bao gồm cả biến riêng của class con nếu có)
        // DrawDefaultInspector() rất thông minh, nó sẽ vẽ cả các biến mới bạn khai báo thêm trong class con
        DrawDefaultInspector();

        // 2. Logic ẩn hiện VFX_Pool (Áp dụng cho mọi class kế thừa)
        if (isGoalProp.boolValue == true)
        {
            EditorGUILayout.Space();
            // Nếu bạn muốn vẽ nó đẹp hơn thì dùng code này, còn không thì DrawDefaultInspector đã vẽ rồi 
            // nhưng nó sẽ bị ẩn bởi [HideInInspector] bên script gốc.
            // Vì vậy ta vẫn cần vẽ thủ công ở đây để nó hiện ra khi điều kiện thỏa mãn.
            EditorGUILayout.PropertyField(vfxPoolProp, new GUIContent("Effect chiến thắng"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}