using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ChartFunction
{
    public Func<float, Nullable<float>> Fn { get; private set; }
    public Color Color { get; private set; }

    public ChartFunction(Func<float, Nullable<float>> f, Color c)
    {
        Fn = f;
        Color = c;
    }
}

public class ChartUtils
{
    private const int TickCount = 5;
    private const int SamplesCount = 80;

    public static float DrawEquationPreview(float minX, float maxX, Rect rect, List<ChartFunction> functions)
    {
        Rect graphRect = new(
            rect.x + 60,
            rect.y + 8,
            rect.width - 70,
            rect.height - 28
        );

        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        float peakX = minX;

        List<Nullable<Vector3>[]> pointsList = new();

        functions.ForEach(func =>
        {
            Vector3[] points = ComputeFunctionPoints(graphRect, func.Fn, minX, maxX, ref minY, ref maxY, ref peakX);
            pointsList.Add(points);
        });

        DrawTicks(graphRect, minX, maxX, minY, maxY);

        Handles.BeginGUI();

        for (int i = 0; i < pointsList.Count; i++)
        {
            Handles.color = functions[i].Color;
            Handles.DrawAAPolyLine(2f,
                pointsList[i].Select(p => p is not Vector3 _p ? new Color(0f, 0f, 0f, 0f) : Color.goldenRod).ToArray(),
                pointsList[i]
            );
        }

        Handles.EndGUI();

        return peakX;
    }

    private static Vector3[] ComputeFunctionPoints(Rect graphRect, Func<float, Nullable<float>> fn, float minX,
        float maxX,
        ref float minY, ref float maxY, ref float peakX)
    {
        Vector3[] points = new Vector3[SamplesCount];

        Nullable<float>[] ys = new Nullable<float>[SamplesCount];

        for (int i = 0; i < SamplesCount; i++)
        {
            float t = i / (float)(SamplesCount - 1);
            float x = Mathf.Lerp(minX, maxX, t);

            if (fn(x) is not float y)
                continue;

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

            if (ys[i] is null) continue;

            float normalizedY = Mathf.InverseLerp(minY, maxY, ys[i]);

            float px = Mathf.Lerp(graphRect.xMin, graphRect.xMax, t);
            float py = Mathf.Lerp(graphRect.yMax, graphRect.yMin, normalizedY);

            points[i] = new Vector3(px, py, 0f);
        }

        return points;
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