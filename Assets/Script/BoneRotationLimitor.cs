using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
using Expand;
#endif

public class BoneRotationLimitor : MonoBehaviour
{
    public List<Limitor> Target = new();

    [System.Serializable]
    public class Limitor
    {
        public GameObject Target;
        public Vector3 Default;

        public bool Limit_X;
        public bool Limit_Y;
        public bool Limit_Z;

        public Vector3 LimitAngle;

        public Limitor(Vector3 DefaultVaule, bool X, bool Y, bool Z, Vector3 limit)
        {
            Default = DefaultVaule;
            Limit_X = X;
            Limit_Y = Y;
            Limit_Z = Z;
            LimitAngle = limit;
        }

        public void SetDefault()
        {
            Default = Target.transform.localRotation.eulerAngles;
        }
    }
    void Start()
    {
        for (int i = 0; i < Target.Count; i++)
        {
            Target[i].SetDefault();
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < Target.Count; i++)
        {
            Vector3 rot = Target[i].Target.transform.localRotation.eulerAngles;
            if (Target[i].Limit_X)
            {

            }
            if (Target[i].Limit_Y)
            {
                float Max = (Mathf.DeltaAngle(rot.y, Target[i].Default.y + Target[i].LimitAngle.y));
                float Min = Mathf.DeltaAngle(rot.y, Target[i].Default.y - Target[i].LimitAngle.y);

                //Target[i].LimitAngle.y

                if (Min > Target[i].LimitAngle.y || Max > Target[i].LimitAngle.y)
                {
                    if (Min > Max)
                    {
                        rot.y = Target[i].Default.y - Target[i].LimitAngle.y;
                    }
                    else
                    {
                        rot.y = Target[i].Default.y + Target[i].LimitAngle.y;
                    }

                    Target[i].Target.transform.localRotation = Quaternion.Euler(rot);

                    //Debug.Log(Target[i].Target + " / " + rot);
                }
            }
            if (Target[i].Limit_Z)
            {

            }
        }

        Debug.Log(Target[0].Target + " / " + Target[0].Target.transform.localRotation.eulerAngles);
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(BoneRotationLimitor.Limitor))]
public class LimitorEditor : PropertyDrawer
{
    Rect DrawRect;
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {

        return 20 * 6;
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //base.OnGUI(position, property, label);

        DrawRect = new Rect(position.x, position.y, position.width, 20);
        property.isExpanded = EditorGUI.Foldout(DrawRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            DrawRect = EditorExpand.NextLine(position, DrawRect, 0, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("Target"));

            DrawRect = EditorExpand.NextLine(position, DrawRect);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("Default"));

            DrawRect = EditorExpand.NextLine(position, DrawRect);
            DrawRect = EditorExpand.RateRect(position, DrawRect, 0, 4);
            EditorGUI.LabelField(DrawRect, "Limit Axis");

            EditorGUIUtility.labelWidth = 25;
            DrawRect = EditorExpand.RateRect(position, DrawRect, 1, 4);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("Limit_X"), new GUIContent { text = "X" });

            DrawRect = EditorExpand.RateRect(position, DrawRect, 2, 4);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("Limit_Y"), new GUIContent { text = "Y" });

            DrawRect = EditorExpand.RateRect(position, DrawRect, 3, 4);
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("Limit_Z"), new GUIContent { text = "Z" });

            DrawRect = EditorExpand.NextLine(position, DrawRect);
            EditorGUIUtility.labelWidth = 100;
            EditorGUI.PropertyField(DrawRect, property.FindPropertyRelative("LimitAngle"), true);
        }
    }
}
#endif