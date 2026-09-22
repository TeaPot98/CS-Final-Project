using System;
using System.Collections.Generic;
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

    public static float DrawEquationPreview(float minX, float maxX, Rect rect, List<ChartFunction> functions,
        Func<float, string> xTickMapper = null)
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

        List<float?[]> samplesList = new();

        foreach (ChartFunction func in functions)
        {
            float?[] samples = new float?[SamplesCount];

            for (int i = 0; i < SamplesCount; i++)
            {
                float t = i / (float)(SamplesCount - 1);
                float x = Mathf.Lerp(minX, maxX, t);

                if (func.Fn(x) is not float y || !float.IsFinite(y))
                    continue;

                samples[i] = y;
                minY = Mathf.Min(minY, y);
                peakX = y >= maxY ? x : peakX;
                maxY = Mathf.Max(maxY, y);
            }

            samplesList.Add(samples);
        }

        if (!float.IsFinite(minY) || !float.IsFinite(maxY))
        {
            minY = 0f;
            maxY = 1f;
        }
        else if (Mathf.Approximately(minY, maxY))
        {
            minY -= 1f;
            maxY += 1f;
        }

        DrawTicks(graphRect, minX, maxX, minY, maxY, xTickMapper);

        Handles.BeginGUI();

        for (int functionIndex = 0; functionIndex < samplesList.Count; functionIndex++)
        {
            List<Vector3> points = new();

            for (int sampleIndex = 0; sampleIndex < SamplesCount; sampleIndex++)
            {
                if (samplesList[functionIndex][sampleIndex] is not float y)
                    continue;

                float t = sampleIndex / (float)(SamplesCount - 1);
                float normalizedY = Mathf.InverseLerp(minY, maxY, y);
                float px = Mathf.Lerp(graphRect.xMin, graphRect.xMax, t);
                float py = Mathf.Lerp(graphRect.yMax, graphRect.yMin, normalizedY);

                points.Add(new Vector3(px, py, 0f));
            }

            if (points.Count < 2)
                continue;

            Handles.color = functions[functionIndex].Color;
            Handles.DrawAAPolyLine(3f, points.ToArray());
        }

        Handles.EndGUI();

        return peakX;
    }

    private static void DrawTicks(
        Rect graphRect,
        float minX,
        float maxX,
        float minY,
        float maxY, Func<float, string> xTickMapper = null
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

            string mappedXLabel = xTickMapper == null ? xValue.ToString("0.##") : xTickMapper(xValue);

            GUI.Label(
                new Rect(x - 25, graphRect.yMax + 4, 50, 16),
                mappedXLabel,
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