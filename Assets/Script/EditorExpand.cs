using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Expand
{
    public static class EditorExpand
    {
        public static Rect NextLine(Rect Pos, Rect DrawRect, float Offset = 0, float LineHeight = 20)
        {
            return new Rect(Pos.x + Offset, (DrawRect.y + DrawRect.height), Pos.width - Offset, LineHeight);//Test LineHeight >> DrawRect.height
        }
        /// <summary>
        /// Use Change LineHeight
        /// </summary>
        /// <param name="Pos"></param>
        /// <param name="DrawRect"></param>
        /// <param name="Offset"></param>
        /// <param name="LineHeight"></param>
        /// <returns></returns>
        public static Rect NextLineOverride(Rect Pos, Rect DrawRect, float Offset = 0, float LineHeight = 20)
        {
            return new Rect(Pos.x + Offset, (DrawRect.y + LineHeight), Pos.width - Offset, LineHeight);
        }//Use Change LineHeight
        public static Rect GetNextSpace(Rect Pos, Rect DrawRect, float width, float PreWidth = 0, bool LineFirst = false, int LineHeight = 20)
        {
            if (LineFirst)
            {
                return new Rect(Pos.x, DrawRect.y, width, LineHeight);
            }
            else
            {
                return new Rect(DrawRect.x + PreWidth, DrawRect.y, width, LineHeight);
            }
        }
        public static Rect RateRect(Rect Pos, Rect DrawRect, int index, int Amount, float Offset = 0, int LineHeight = 20)
        {
            float size = (Pos.width - Offset) / Amount;
            return new Rect((Pos.x + Offset + size * index), DrawRect.y, size, LineHeight);
        }
        public static Rect RateRect(Rect Pos, Rect DrawRect, int index, int Amount, float Height, float Offset = 0)
        {
            float size = (Pos.width - Offset) / Amount;
            return new Rect((Pos.x + Offset + size * index), DrawRect.y, size, Height);
        }

        public static Rect ResizedLabel(Rect Pos, Rect DrawRect, string Text)
        {
            EditorGUI.indentLevel = 0;
            float Size = GUI.skin.label.CalcSize(new GUIContent(Text)).x;

            Rect LdrawRect = new Rect(DrawRect.x, DrawRect.y, Size, DrawRect.height);

            EditorGUI.LabelField(LdrawRect, Text);
            return new Rect(LdrawRect.x + Size, LdrawRect.y, Pos.width - Size, LdrawRect.height);
        }

        public static int LayerMaskField(Rect pos, string label, int Layers, float Space = 0)
        {
            {/*
            List<string> layers = new List<string>();
            List<int> layerNumbers = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & Layers) > 0)
                    maskWithoutEmpty |= (1 << i);//  ========>  maskWithoutEmpty = maskWithoutEmpty <비트 OR 연산> (1 << i {2의 i 제곱} )
            }
            maskWithoutEmpty = EditorGUI.MaskField(pos, label, maskWithoutEmpty, layers.ToArray());
            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            return mask;*/
            }

            Rect LRect = new Rect(pos.x + Space, pos.y, pos.width - Space, pos.height);
            LRect = ResizedLabel(pos, LRect, label);

            float InputWidth = Mathf.Max(0, (pos.width - LRect.width));

            LRect = GetNextSpace(pos, LRect, InputWidth);

            return EditorGUI.MaskField(LRect, Layers, UnityEditorInternal.InternalEditorUtility.layers);
        }
        public static LayerMask LayerMaskField(Rect pos, string label, LayerMask Layers)
        {
            return LayerMaskField(pos, label, Layers.value);
        }

        public static Rect LabelRateField(Rect pos, Rect DrawRect, string Text, int LineAmount, float Space = 0, bool hightLight = false)
        {
            Rect LRect = new Rect(DrawRect.x + Space, DrawRect.y, ((pos.width) / LineAmount), DrawRect.height);

            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(LRect, Text);
            if (hightLight)
            {
                EditorGUI.HelpBox(LRect, "", MessageType.None);
            }

            return new Rect((LRect.x + ((pos.width) / LineAmount)), DrawRect.y, ((pos.width) / LineAmount), DrawRect.height);
        }
        public static Rect LabelRateField(Rect pos, Rect DrawRect, GUIContent Text, int LineAmount, float Space = 0, bool hightLight = false)
        {
            Rect LRect = new Rect(DrawRect.x + Space, DrawRect.y, ((pos.width) / LineAmount), DrawRect.height);

            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(LRect, Text);
            if (hightLight)
            {
                EditorGUI.HelpBox(LRect, "", MessageType.None);
            }

            return new Rect((LRect.x + ((pos.width) / LineAmount)), DrawRect.y, ((pos.width) / LineAmount), DrawRect.height);
        }

        /// <summary>
        /// NotWork Include List, Array But Can Make Support / T - Find Type
        /// </summary>
        /// <typeparam name="T">Find Type</typeparam>
        /// <param name="fieldInfo"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static object GetPropertyDrawerTarget<T>(FieldInfo fieldInfo, SerializedProperty property)
        {
            object Lobj = null;
            if (property.serializedObject.targetObject.GetType() == typeof(T))
            {
                Lobj = fieldInfo.GetValue(property.serializedObject.targetObject);
            }
            else
            {
                var paths = property.propertyPath.Split('.');
                object LChildObj = property.serializedObject.targetObject.GetType().GetField(paths[0])
                    .GetValue(property.serializedObject.targetObject);
                FieldInfo LChildField = property.serializedObject.targetObject.GetType().GetField(paths[0]);
                //Debug.Log(property.serializedObject.targetObject.GetType().GetField(paths[0]) + " | " + paths[0]);

                if (typeof(T) == LChildObj.GetType())
                {
                    Lobj = LChildObj;
                    return Lobj;
                }

                if (LChildField != null)
                {
                    for (int i = 1; i < paths.Length; i++)
                    {
                        LChildField = LChildField.FieldType.GetField(paths[i]);

                        if (i + 1 != paths.Length)
                        {
                            LChildObj = LChildObj.GetType().GetField(paths[i]).GetValue(LChildObj);
                        }//LChildField 보다 1단계 상위에 있어야함
                    }

                    //Debug.Log(LChildField.Name + " | " + LChildObj.ToString());
                    if (typeof(T) == LChildObj.GetType())
                    {
                        Lobj = LChildObj;
                    }
                    else
                    {
                        Lobj = LChildField.GetValue(LChildObj);
                    }
                }
            }
            return Lobj;
        }//리스트와 배열이 있는경우는 미구현

        public static System.Type PropertyTypeToType(string propertyType)
        {
            switch (propertyType)
            {
                case "int":
                    return typeof(int);
                case "bool":
                    return typeof(bool);
                case "float":
                    return typeof(float);
                case "string":
                    return typeof(string);
                case "Color":
                    return typeof(Color);
                case "PPtr<$GameObject>":
                    return typeof(GameObject);
                case "LayerMask":
                    return typeof(LayerMask);
                case "Enum":
                    return null;
                case "Vector2":
                    return typeof(Vector2);
                case "Vector3":
                    return typeof(Vector3);
                case "Vector4":
                    return typeof(Vector4);
                case "Rect":
                    return typeof(Rect);
                case "ArraySize":
                    return null;
                case "Character":
                    return null;
                case "AnimationCurve":
                    return typeof(AnimationCurve);
                case "Bounds":
                    return typeof(Bounds);
                case "Gradient":
                    return typeof(Gradient);
                case "Quaternion":
                    return typeof(Quaternion);
                case "ExposedReference":
                case "FixedBufferSize":
                case "ManagedReference":
                    return null;
                case "Vector2Int":
                    return typeof(Vector2Int);
                case "Vector3Int":
                    return typeof(Vector3Int);
                case "RectInt":
                    return typeof(RectInt);
                case "BoundsInt":
                    return typeof(BoundsInt);
                default:
                    return null;
            }
            //Debug.Log(EditorExpand.PropertyTypeToType(property.type).FullName);
        }

        public static SerializedProperty FindProperty(SerializedProperty property , params string[] RelativePath)
        {
            SerializedProperty LPath = property;
            for (int i = 0; i < RelativePath.Length; i++)
            {
                LPath = property.FindPropertyRelative(RelativePath[i]);
            }

            return LPath;
        }
    }

    #region AttributeLabel
    public class AttributeLabel : PropertyAttribute
    {
        public bool HightLight = false;
        public bool Expand = false;
        public string Text = "";
        public AttributeLabel(string text, bool hightLight = false, bool expand = false)
        {
            Text = text;
            Expand = expand;
            HightLight = hightLight;
        }
    }
    [CustomPropertyDrawer(typeof(AttributeLabel))]
    public class AttributeLabelEditor : PropertyDrawer
    {
        bool HightLight = false;
        string Text = "";
        AttributeLabel attributeLabel;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            //return Mathf.Min(20, GUI.skin.label.CalcSize(new GUIContent(Text)).y) + 20;
            return EditorGUI.GetPropertyHeight(property, true) + Mathf.Min(20, GUI.skin.label.CalcSize(new GUIContent(Text)).y);
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            attributeLabel = (AttributeLabel)attribute;
            HightLight = attributeLabel.HightLight;
            Text = attributeLabel.Text;

            Vector2 TextArea = GUI.skin.label.CalcSize(new GUIContent(Text));
            Rect DrawRect = new Rect();

            if (attributeLabel.Expand)
            {
                DrawRect = new Rect(position.x, position.y, position.width, TextArea.y);
            }
            else
            {
                DrawRect = new Rect(position.x, position.y, TextArea.x + 15, TextArea.y);
            }

            if (HightLight)
            {
                EditorGUI.HelpBox(DrawRect, Text, MessageType.None);
                DrawRect = EditorExpand.NextLine(position, DrawRect);
                //TypeEnum Ltype = VariableCollection.ConvertTypeEnum(EditorExpand.PropertyTypeToType(property.type).Name);
                EditorGUI.PropertyField(DrawRect, property, new GUIContent(property.displayName), true);
                //EditorExpand.PropertyField(position, DrawRect, property, property.displayName, 1);
            }
            else
            {
                EditorGUI.LabelField(DrawRect, Text);
                DrawRect = EditorExpand.NextLine(position, DrawRect);
                //TypeEnum Ltype = VariableCollection.ConvertTypeEnum(EditorExpand.PropertyTypeToType(property.type).Name);
                //ditorExpand.PropertyField(position, DrawRect, property, property.displayName, 1);
                EditorGUI.PropertyField(DrawRect, property, new GUIContent(property.displayName), true);
            }
        }
    }//[AttributeLabel("Testing", true, true)]
    #endregion AttributeLabel

    #region AttributeLayer
    public class AttributeLayer : PropertyAttribute
    {

    }
    [CustomPropertyDrawer(typeof(AttributeLayer))]
    public class AttributeLayerEditor : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 20;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.intValue = EditorGUI.LayerField(position, property.displayName, property.intValue);
        }
    }//[AttributeLayer("Team Layer")] //Editor는 저장X, PropertyDrawer는 전부 구현하거나(일부분 구현X)
    #endregion AttributeLayer

    #region AttributeLayerMask
    public class AttributeLayerMask : PropertyAttribute
    {

    }
    [CustomPropertyDrawer(typeof(AttributeLayerMask))]
    public class AttributeLayerMaskEditor : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 20;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //
            //string lastlayer = UnityEditorInternal.InternalEditorUtility.layers[UnityEditorInternal.InternalEditorUtility.layers.Length - 1];

            int lastlayer = 31;
            for (int i = 31; i >= 0; i--)
            {
                if (! string.IsNullOrEmpty(UnityEditorInternal.InternalEditorUtility.GetLayerName(i)))
                {
                    lastlayer = i;
                    break;
                }
            }

            string[] layers = new string[lastlayer + 1];
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i] = UnityEditorInternal.InternalEditorUtility.GetLayerName(i);
            }

            //UnityEditorInternal.InternalEditorUtility.GetLayerName(n번째) - 이걸로 레이어 원래 순서로 나옴
            //레이어 31번부터 역으로 검사후, Null이 아닌 첫 인덱스 저장후 MaskField에 쓸 이름배열 생성

            property.intValue = EditorGUI.MaskField(position, label.text, property.intValue, layers);
        }
    }//[AttributeLayer("Team Layer")] //Editor는 저장X, PropertyDrawer는 전부 구현하거나(일부분 구현X)
    #endregion AttributeLayerMask

    #region AttributeHorizontal
    public class AttributeHorizontal : PropertyAttribute
    {
        public string[] BasePath;
        public string[] Paths;
        public AttributeHorizontal(string[] basePath, params string[] paths)
        {
            BasePath = basePath;
            Paths = paths;
        }
    }

    [CustomPropertyDrawer(typeof(AttributeHorizontal))]
    public class AttributeHorizontalEditor : PropertyDrawer
    {
        AttributeHorizontal attributeHorizontal;
        Rect DrawRect;
        SerializedProperty BaseProperty;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            attributeHorizontal = (AttributeHorizontal)attribute;
            string[] BasePath = attributeHorizontal.BasePath;
            string[] Paths = attributeHorizontal.Paths;
            DrawRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(DrawRect, property, label, true);
            //DrawRect = EditorExpand.PropertyField(position, DrawRect, property, property.displayName, (Paths.Length + 1));

            if (BasePath.Length > 0)
            {
                for (int i = 0; i < BasePath.Length; i++)
                {
                    if (i == 0)
                    {
                        BaseProperty = property.serializedObject.FindProperty(BasePath[i]);
                    }
                    else
                    {
                        BaseProperty = BaseProperty.FindPropertyRelative(BasePath[i]);
                    }
                }

                for (int i = 0; i < Paths.Length; i++)
                {
                    var Lproperty = BaseProperty.FindPropertyRelative(Paths[i]);//먼저 BasePath에 접근
                    EditorGUI.PropertyField(DrawRect, Lproperty, new GUIContent(Lproperty.displayName), true);
                    //DrawRect = EditorExpand.PropertyField(position, DrawRect, Lproperty, Lproperty.displayName, (Paths.Length + 1));
                }
            }
        }
    }
    #endregion//Attribute으로 다른 변수 접근해서 가로배치//[AttributeHorizontal(new string[] { 해당 변수속한 상위클래스 경로 }, 가로 배치할 변수이름)]

    #region AttributeMask
    public class AttributeMask : PropertyAttribute
    {
        public string[] SelectList;

        public AttributeMask(params string[] selectList)
        {
            SelectList = selectList;
        }
    }
    [CustomPropertyDrawer(typeof(AttributeMask))]
    public class AttributeMaskEditor : PropertyDrawer
    {
        AttributeMask attributeField;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 20;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            attributeField = (AttributeMask)attribute;
            property.intValue = EditorGUI.MaskField(position, label.text, property.intValue, attributeField.SelectList);
        }
    }
    #endregion AttributeLayerMask

    #region AttributeHidden
    /// <summary>
    /// Just Hidden , Can Access
    /// </summary>
    public class AttributeHidden : PropertyAttribute
    {
    }
    [CustomPropertyDrawer(typeof(AttributeHidden))]
    public class AttributeHiddenEditor : PropertyDrawer
    {
        //AttributeHidden attributeField;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
        }
    }
    #endregion AttributeLayerMask

    [CustomPropertyDrawer(typeof(Gradient))]
    public class GradientEditor : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //base.OnGUI(position, property, label);

            if (property.FindPropertyRelative("colorKeys") == null || property.FindPropertyRelative("alphaKeys") == null)
            {
                EditorGUI.PropertyField(position, property, label, true);

                Debug.Log("Can't Enable HDR GradientField \n" + property.propertyPath);
            }
            else
            {
                Gradient gradient = EditorGUI.GradientField(position, label, GradientVaule(property), true);

                GradientVaule(property, gradient);
            }
        }
        public Gradient GradientVaule(SerializedProperty property)
        {
            if (property != null)
            {
                return new Gradient();
            }

            var p_color = property.FindPropertyRelative("colorKeys");
            var p_alpha = property.FindPropertyRelative("alphaKeys");

            if (p_color != null || p_alpha != null)
            {
                GradientColorKey[] colorKeys = new GradientColorKey[p_color.arraySize];
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[p_alpha.arraySize];

                for (int i = 0; i < colorKeys.Length; i++)
                {
                    colorKeys[i] = new GradientColorKey(p_color.GetArrayElementAtIndex(i).FindPropertyRelative("color").colorValue,
                        p_color.GetArrayElementAtIndex(i).FindPropertyRelative("time").floatValue);
                }
                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    alphaKeys[i] = new GradientAlphaKey(p_alpha.GetArrayElementAtIndex(i).FindPropertyRelative("alpha").floatValue,
                        p_alpha.GetArrayElementAtIndex(i).FindPropertyRelative("time").floatValue);
                }
                GradientMode mode = property.FindPropertyRelative("mode").enumValueIndex == 0 ? GradientMode.Blend : GradientMode.Fixed;

                Gradient result = new();
                result.SetKeys(colorKeys, alphaKeys);
                result.mode = mode;

                return result;
            }
            else
            {
                return null;
            }
        }//Get
        public bool GradientVaule(SerializedProperty property, Gradient gradient)
        {
            if (gradient != null)
            {
                if (property.FindPropertyRelative("colorKeys") != null)
                {
                    for (int c = 0; c < gradient.colorKeys.Length; c++)
                    {
                        property.FindPropertyRelative("colorKeys").arraySize = gradient.colorKeys.Length;

                        property.FindPropertyRelative("colorKeys").GetArrayElementAtIndex(c).FindPropertyRelative("color").colorValue =
                            gradient.colorKeys[c].color;
                        property.FindPropertyRelative("colorKeys").GetArrayElementAtIndex(c).FindPropertyRelative("time").floatValue =
                            gradient.colorKeys[c].time;
                    }
                }

                if (property.FindPropertyRelative("alphaKeys") != null)
                {
                    for (int c = 0; c < gradient.alphaKeys.Length; c++)
                    {
                        property.FindPropertyRelative("alphaKeys").arraySize = gradient.colorKeys.Length;

                        property.FindPropertyRelative("alphaKeys").GetArrayElementAtIndex(c).FindPropertyRelative("alpha").floatValue =
                            gradient.alphaKeys[c].alpha;
                        property.FindPropertyRelative("alphaKeys").GetArrayElementAtIndex(c).FindPropertyRelative("time").floatValue =
                            gradient.alphaKeys[c].time;
                    }
                }

                if (property.FindPropertyRelative("mode") != null)
                    property.FindPropertyRelative("mode").enumValueIndex = gradient.mode == GradientMode.Blend ? 0 : 1;
            }

            return gradient != null;
        }//Set
    }
}
#endif