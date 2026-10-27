using System;
using UnityEngine;

public static class PresentationStageKeyParser
{
    public static bool TryParse(
        string raw,
        out PresentationStageKey stage)
    {
        stage = PresentationStageKey.Stage00;

        string s = (raw ?? string.Empty).Trim().ToLowerInvariant();

        switch (s)
        {
            case "0":
            case "00":
            case "s0":
            case "slot0":
            case "slot00":
            case "stage0":
            case "stage00":
            case "a":
                stage = PresentationStageKey.Stage00;
                return true;

            case "1":
            case "01":
            case "s1":
            case "slot1":
            case "slot01":
            case "stage1":
            case "stage01":
            case "b":
                stage = PresentationStageKey.Stage01;
                return true;

            case "2":
            case "02":
            case "s2":
            case "slot2":
            case "slot02":
            case "stage2":
            case "stage02":
            case "c":
                stage = PresentationStageKey.Stage02;
                return true;
        }

        return Enum.TryParse(raw, true, out stage);
    }

    public static PresentationStageKey Parse(
        string raw,
        PresentationStageKey fallback = PresentationStageKey.Stage00)
    {
        if (TryParse(raw, out PresentationStageKey stage))
            return stage;

        Debug.LogWarning(
            $"[PresentationStageKeyParser] Unknown stage '{raw}'. " +
            $"Fallback to '{fallback}'.");

        return fallback;
    }
}

public static class PresentationDepthLayerKeyParser
{
    public static bool TryParse(
        string raw,
        out PresentationDepthLayerKey layer)
    {
        layer = PresentationDepthLayerKey.Mid;

        string s = (raw ?? string.Empty).Trim().ToLowerInvariant();

        switch (s)
        {
            case "0":
            case "far":
            case "f":
            case "deep":
                layer = PresentationDepthLayerKey.Far;
                return true;

            case "1":
            case "back":
            case "b":
            case "bg":
                layer = PresentationDepthLayerKey.Back;
                return true;

            case "2":
            case "mid":
            case "middle":
            case "m":
            case "center":
                layer = PresentationDepthLayerKey.Mid;
                return true;

            case "3":
            case "front":
            case "fr":
            case "fg":
                layer = PresentationDepthLayerKey.Front;
                return true;

            case "4":
            case "close":
            case "near":
            case "c":
                layer = PresentationDepthLayerKey.Close;
                return true;
        }

        return Enum.TryParse(raw, true, out layer);
    }

    public static PresentationDepthLayerKey Parse(
        string raw,
        PresentationDepthLayerKey fallback = PresentationDepthLayerKey.Far)
    {
        if (TryParse(raw, out PresentationDepthLayerKey layer))
            return layer;

        Debug.LogWarning(
            $"[PresentationDepthLayerKeyParser] Unknown depth layer '{raw}'. " +
            $"Fallback to '{fallback}'.");

        return fallback;
    }
}