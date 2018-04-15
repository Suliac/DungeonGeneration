using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GrammarPattern))]
public class GrammarPatternEditor : Editor
{
    GrammarPattern pattern;
    int displayGraphChoices = -1;
    string displayChoiceForGraph = "Before";
    float choiceSize = 0.0f;
    Vector2 scrollPos = Vector2.zero;

    bool autoSelect = true;
    ContentType choiceAutoSelection = ContentType.ToDefine;
    void OnEnable()
    {
        pattern = target as GrammarPattern;
    }

    public override void OnInspectorGUI()
    {
        GUI.changed = false;

        // Width
        GUILayout.BeginHorizontal();
        GUILayout.Label("Width", GUILayout.Width(125));

        if (!int.TryParse(EditorGUILayout.TextField(pattern.Width.ToString()), out pattern.Width))
        {
            EditorGUILayout.TextField(pattern.Width.ToString());
        }

        GUILayout.EndHorizontal();

        // Height
        //GUILayout.BeginHorizontal();
        //GUILayout.Label("Height", GUILayout.Width(125));

        //if (!int.TryParse(EditorGUILayout.TextField(pattern.Height.ToString()), out pattern.Height))
        //{
        //    EditorGUILayout.TextField(pattern.Height.ToString());
        //}

        //GUILayout.EndHorizontal();

        // Strcture pattern ?
        GUILayout.BeginHorizontal();
        GUILayout.Label("Is structure pattern ?", GUILayout.Width(125));
        pattern.IsStructurePattern = EditorGUILayout.Toggle(pattern.IsStructurePattern);
        GUILayout.EndHorizontal();

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(Screen.width * 0.95f), GUILayout.Height(Screen.height - choiceSize * 2.5f - GUI.skin.label.lineHeight * 5f));

        // Graph Before
        GUILayout.BeginHorizontal();
        GUILayout.Label("Graph 'Before'", GUILayout.Width(125));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset", GUILayout.Width(125)))
            Reset(pattern.GraphBefore);

        if (GUILayout.Button("Rotate", GUILayout.Width(125)))
            Rotate(pattern.GraphBefore);
        GUILayout.EndHorizontal();

        DisplayGraph(pattern.GraphBefore, pattern.Width, "Before");

        // Graph After
        GUILayout.BeginHorizontal();
        GUILayout.Label("Graph 'After'", GUILayout.Width(125));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset", GUILayout.Width(125)))
            Reset(pattern.GraphAfter);

        if (GUILayout.Button("Rotate", GUILayout.Width(125)))
            Rotate(pattern.GraphAfter);
        GUILayout.EndHorizontal();

        DisplayGraph(pattern.GraphAfter, pattern.Width, "After");
        GUILayout.EndScrollView();

        // DisplayChoices
        DisplayChoices(pattern.Width, displayChoiceForGraph);

        if (GUI.changed)
            EditorUtility.SetDirty(pattern);
    }

    private void DisplayGraph(List<ContentType> graph, int width, string graphName)
    {
        while (graph.Count <= width * width) // S'il manque des cases on les ajoutes
            graph.Add(ContentType.ToDefine);

        while (graph.Count > width * width) // S'il y a trop de cases on les supprimes
            graph.RemoveAt(graph.Count - 1);

        for (int y = 0; y < width; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < width; x++)
            {
                int indexInTabs = x + y * width;

                string[] options = Enum.GetNames(typeof(ContentType));
                float size = Screen.width * 0.75f / width;

                bool imSelected = displayGraphChoices > -1 && displayGraphChoices == indexInTabs && displayChoiceForGraph == graphName;
                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);

                var oldColor = GUI.backgroundColor;

                if (imSelected)
                {
                    buttonStyle.normal.background = buttonStyle.active.background;
                    buttonStyle.normal.textColor = buttonStyle.active.textColor;
                }
                else
                {
                    switch (graph[indexInTabs])
                    {
                        case ContentType.ToDefine:
                            GUI.backgroundColor = Color.cyan;
                            break;
                        case ContentType.Empty:
                            GUI.backgroundColor = Color.green;
                            break;
                        case ContentType.Block:
                            GUI.backgroundColor = Color.gray;
                            break;
                        case ContentType.Enemy:
                            GUI.backgroundColor = Color.red;
                            break;
                        case ContentType.Bonus:
                            GUI.backgroundColor = Color.magenta;
                            break;
                        case ContentType.Anything:
                            break;
                        default:
                            break;
                    }
                }

                if (GUILayout.Button(graph[indexInTabs].ToString(), buttonStyle, GUILayout.Width(size), GUILayout.Height(size)))
                {
                    if (!autoSelect)
                    {
                        displayGraphChoices = indexInTabs;
                        displayChoiceForGraph = graphName;
                    }
                    else
                    {
                        graph[indexInTabs] = choiceAutoSelection;
                    }
                }
                GUI.backgroundColor = oldColor;

            }
            GUILayout.EndHorizontal();
        }
    }

    private void DisplayChoices(int width, string graphName)
    {
        List<ContentType> graph = graphName == "Before" ? pattern.GraphBefore : pattern.GraphAfter;
        bool imSelected = false;
        int xPos = 0;
        int yPos = 0;
        string[] options = Enum.GetNames(typeof(ContentType));

        if (autoSelect)
            displayGraphChoices = -1;

        for (int y = 0; y < width; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int indexInTabs = x + y * width;
                if (displayGraphChoices == indexInTabs && displayChoiceForGraph == graphName)
                {
                    imSelected = true;
                    xPos = x;
                    yPos = y;
                }
            }
        }

        choiceSize = Screen.width * 0.9f / options.Length;
        GUILayout.BeginHorizontal();
        autoSelect = GUI.Toggle(new Rect(0, Screen.height - choiceSize * 1.5f - GUI.skin.label.lineHeight * 2.75f, Screen.width, GUI.skin.label.lineHeight * 1.2f), autoSelect, "Auto selection");
        GUILayout.EndHorizontal();
        if (imSelected)
        {
            GUI.Label(new Rect(0, Screen.height - choiceSize * 1.5f - GUI.skin.label.lineHeight * 1.5f, Screen.width, choiceSize), "Choix de contenu pour la case en (" + xPos + "," + yPos + ") du graph '" + graphName + "'");
            graph[displayGraphChoices] = (ContentType)GUI.SelectionGrid(new Rect(0, Screen.height - choiceSize * 1.5f, Screen.width, choiceSize * 0.75f), (int)graph[displayGraphChoices], options, 5);
        }
        else
        {
            GUI.Label(new Rect(0, Screen.height - choiceSize * 1.5f - GUI.skin.label.lineHeight * 1.5f, Screen.width, choiceSize), "Choix de contenu (aucune case selectionnée) : ");
            choiceAutoSelection = (ContentType)GUI.SelectionGrid(new Rect(0, Screen.height - choiceSize * 1.5f, Screen.width, choiceSize * 0.75f), (int)choiceAutoSelection, options, 5);
        }
    }

    private void Reset(List<ContentType> graph)
    {
        for (int i = 0; i < graph.Count; i++)
        {
            graph[i] = autoSelect ? choiceAutoSelection : ContentType.ToDefine;
        }
    }

    private void Rotate(List<ContentType> graph)
    {
        ContentType[] newGraph = new ContentType[graph.Count];
        for (int i = 0; i < graph.Count; i++)
        {
            int x = i % pattern.Width;
            int y = i / pattern.Width;

            int newX = pattern.Width - (y + 1);
            int newY = x;

            int newI = newX + pattern.Width * newY;
            newGraph[newI] = graph[i];
        }

        for (int i = 0; i < graph.Count; i++)
            graph[i] = newGraph[i];
    }
}
