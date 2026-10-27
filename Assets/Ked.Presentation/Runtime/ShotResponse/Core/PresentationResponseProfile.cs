using System;
using UnityEngine;

[Serializable]
public sealed class PresentationResponseProfile
{
    [Tooltip(
        "카메라 scale 변화량 1.0당 focusPoint 기준으로 대상이 X/Y축별로 밀려나는 거리. " +
        "예: zoom=8, cameraScale=1.4이면 zoomAmount=0.4. " +
        "X(좌우 구도)와 Y(높이/여백)는 서로 완전히 독립적으로 계산됩니다. 0이면 그 축은 퍼지지 않음.")]
    public Vector2 focusSpreadPixelsPerZoom = Vector2.zero;

    [Tooltip(
        "pan 픽셀값을 X/Y축별로 따라가는 정도. " +
        "panInRigSpace는 이미 픽셀 단위이므로 이 값은 보통 -0.1~0.1 사용. ")]
    public Vector2 panResponse = Vector2.zero;

    [Tooltip(
        "StageZoom_Root의 cameraScale을 layer localScale에서 얼마나 상쇄/강조할지. " +
        "0이면 layer 자체 scale 변화 없음, -1이면 StageZoom을 완전히 상쇄, 양수면 더 크게 강조. " +
        "계산식 = baseLocalScale * pow(cameraScale, zoomScaleResponse).")]
    public float zoomScaleResponse = 0f;

    public static PresentationResponseProfile Zero => new()
    {
        focusSpreadPixelsPerZoom = Vector2.zero,
        panResponse = Vector2.zero,
        zoomScaleResponse = 0f,
    };

    public static PresentationResponseProfile DepthFar => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(-8f, 0f),
        panResponse = new Vector2(-0.04f, 0f),
        zoomScaleResponse = -0.15f,
    };

    public static PresentationResponseProfile DepthBack => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(-4f, 0f),
        panResponse = new Vector2(-0.02f, 0f),
        zoomScaleResponse = -0.08f,
    };

    public static PresentationResponseProfile DepthMid => new()
    {
        focusSpreadPixelsPerZoom = Vector2.zero,
        panResponse = Vector2.zero,
        zoomScaleResponse = 0f,
    };

    public static PresentationResponseProfile DepthFront => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(4f, 0f),
        panResponse = new Vector2(0.02f, 0f),
        zoomScaleResponse = 0.04f,
    };

    public static PresentationResponseProfile DepthClose => new()
    {
        focusSpreadPixelsPerZoom = new Vector2(16f, 0f),
        panResponse = new Vector2(0.2f, 0f),
        zoomScaleResponse = 0.16f,
    };
}