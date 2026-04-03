using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LevelManager.ThemeAssets))]
public class ThemeAssetsDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get the index of the current element in the list
        string path = property.propertyPath;
        int index = 0;
        if (path.Contains("["))
        {
            string indexStr = path.Substring(path.LastIndexOf("[") + 1).Replace("]", "");
            int.TryParse(indexStr, out index);
        }

        EditorGUI.BeginProperty(position, label, property);
        
        // Use standard foldout
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label);
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + 2;

            // Helper to draw property and update y
            void DrawField(string fieldName)
            {
                SerializedProperty p = property.FindPropertyRelative(fieldName);
                if (p != null)
                {
                    float h = EditorGUI.GetPropertyHeight(p);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), p, true);
                    y += h + 2;
                }
            }

            // Always show base theme fields
            DrawField("obstacleSprite");
            DrawField("obstacleScale");
            
            if (index >= 2)
            {
                DrawField("wallSprite");
                DrawField("wallScale");
            }

            DrawField("obstacleYOffset");

            if (index >= 2)
            {
                DrawField("wallYOffset");
                DrawField("longWallYOffset");
            }

            if (index >= 1)
            {
                DrawField("enemyYOffset");
            }

            if (index >= 3)
            {
                DrawField("bushYOffset");
            }

            if (index != 4)
            {
                EditorGUI.LabelField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), "Physics Overrides (BoxCollider2D)", EditorStyles.boldLabel);
                y += EditorGUIUtility.singleLineHeight + 2;

                // Always show Obstacle Bag (Level 1+)
                DrawField("obstacleColliderSize");

                // Element index 1 is Level 2
                if (index >= 1)
                {
                    DrawField("enemyColliderSize");
                }

                // Element index 2 is Level 3
                if (index == 2 || index >= 5)
                {
                    DrawField("wallColliderSize");
                    DrawField("longWallColliderSize");
                }

                // Element index 3 is Level 4
                if (index >= 3)
                {
                    if (index == 3)
                    {
                        DrawField("wallColliderSize");
                        DrawField("bushColliderSize");
                    }
                    else if (index >= 5)
                    {
                        DrawField("bushColliderSize");
                    }
                }

                if (index >= 2 && index != 4)
                {
                    DrawField("wallPhysicsMaterial");
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
        
        string path = property.propertyPath;
        int index = 0;
        if (path.Contains("["))
        {
            string indexStr = path.Substring(path.LastIndexOf("[") + 1).Replace("]", "");
            int.TryParse(indexStr, out index);
        }

        float h = EditorGUIUtility.singleLineHeight + 4; // Foldout + padding
        
        float FieldHeight(string fieldName) => property.FindPropertyRelative(fieldName) != null ? EditorGUI.GetPropertyHeight(property.FindPropertyRelative(fieldName)) + 2 : 0;

        h += FieldHeight("obstacleSprite");
        h += FieldHeight("obstacleScale");
        
        if (index >= 2)
        {
            h += FieldHeight("wallSprite");
            h += FieldHeight("wallScale");
        }

        h += FieldHeight("obstacleYOffset");

        if (index >= 2)
        {
            h += FieldHeight("wallYOffset");
            h += FieldHeight("longWallYOffset");
        }

        if (index >= 1)
        {
            h += FieldHeight("enemyYOffset");
        }

        if (index >= 3)
        {
            h += FieldHeight("bushYOffset");
        }

        if (index != 4)
        {
            h += EditorGUIUtility.singleLineHeight + 2; // Physics Header

            h += FieldHeight("obstacleColliderSize");

            if (index >= 1)
            {
                h += FieldHeight("enemyColliderSize");
            }
            
            if (index == 2 || index >= 5)
            {
                h += FieldHeight("wallColliderSize");
                h += FieldHeight("longWallColliderSize");
            }
            
            if (index >= 3)
            {
                if (index == 3)
                {
                    h += FieldHeight("wallColliderSize");
                    h += FieldHeight("bushColliderSize");
                }
                else if (index >= 5)
                {
                    h += FieldHeight("bushColliderSize");
                }
            }

            if (index >= 2 && index != 4)
            {
                h += FieldHeight("wallPhysicsMaterial");
            }
        }

        return h;
    }
}
