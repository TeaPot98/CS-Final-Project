using System;
using UnityEditor;
using UnityEngine;

public class ChartUtils
{
    private const int TickCount = 5;
    private const int SamplesCount = 80;

    public static float DrawEquationPreview(float minX, float maxX, Rect rect, Func<float, float> fn)
    {
        Rect graphRect = new(
            rect.x + 60,
            rect.y + 8,
            rect.width - 70,
            rect.height - 28
        );

        Vector3[] points = new Vector3[SamplesCount];

        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        float peakX = minX;

        float[] ys = new float[SamplesCount];

        for (int i = 0; i < SamplesCount; i++)
        {
            float t = i / (float)(SamplesCount - 1);
            float x = Mathf.Lerp(minX, maxX, t);
            float y = fn(x);

            ys[i] = y;
            minY = Mathf.Min(minY, y);
            peakX = y >= maxY ? x : peakX;
            maxY = Mathf.Max(maxY, y);
        }

        if (Mathf.Approximately(minY, maxY))
        {
            minY -= 1f;
            maxY += 1f;
        }

        for (int i = 0; i < SamplesCount; i++)
        {
            float t = i / (float)(SamplesCount - 1);
            float normalizedY = Mathf.InverseLerp(minY, maxY, ys[i]);

            float px = Mathf.Lerp(graphRect.xMin, graphRect.xMax, t);
            float py = Mathf.Lerp(graphRect.yMax, graphRect.yMin, normalizedY);

            points[i] = new Vector3(px, py, 0f);
        }

        DrawTicks(graphRect, minX, maxX, minY, maxY);

        Handles.BeginGUI();
        Handles.color = Color.cyan;
        Handles.DrawAAPolyLine(2f, points);
        Handles.EndGUI();

        return peakX;
    }

    private static void DrawTicks(
        Rect graphRect,
        float minX,
        float maxX,
        float minY,
        float maxY
    )
    {
        GUIStyle labelStyle = new(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.75f, 0.75f, 0.75f) },
            alignment = TextAnchor.MiddleCenter
        };

        Handles.BeginGUI();

        Handles.color = new Color(1f, 1f, 1f, 0.25f);

        for (int i = 0; i < TickCount; i++)
        {
            float t = i / (float)(TickCount - 1);

            float x = Mathf.Lerp(graphRect.xMin, graphRect.xMax, t);
            float xValue = Mathf.Lerp(minX, maxX, t);

            Handles.DrawLine(
                new Vector3(x, graphRect.yMax),
                new Vector3(x, graphRect.yMax + 4)
            );

            GUI.Label(
                new Rect(x - 25, graphRect.yMax + 4, 50, 16),
                xValue.ToString("0.##"),
                labelStyle
            );

            float y = Mathf.Lerp(graphRect.yMax, graphRect.yMin, t);
            float yValue = Mathf.Lerp(minY, maxY, t);

            Handles.DrawLine(
                new Vector3(graphRect.xMin - 4, y),
                new Vector3(graphRect.xMin, y)
            );

            GUI.Label(
                new Rect(0, y - 8, graphRect.xMin - 6, 16),
                yValue.ToString("0.##"),
                labelStyle
            );
        }

        Handles.color = new Color(1f, 1f, 1f, 0.45f);

        Handles.DrawLine(
            new Vector3(graphRect.xMin, graphRect.yMin),
            new Vector3(graphRect.xMin, graphRect.yMax)
        );

        Handles.DrawLine(
            new Vector3(graphRect.xMin, graphRect.yMax),
            new Vector3(graphRect.xMax, graphRect.yMax)
        );

        Handles.EndGUI();
    }
}