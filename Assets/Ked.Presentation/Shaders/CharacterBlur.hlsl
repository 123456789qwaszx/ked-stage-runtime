#ifndef KED_CHARACTER_BLUR_INCLUDED
#define KED_CHARACTER_BLUR_INCLUDED

// 캐릭터 초상용 디포커스 블러 — 밉 1탭 + 십자 4탭.
//
// 왜 둘을 겹치는가:
//   밉만 쓰면 높은 LOD에서 확대 배율이 커져 이중선형 계단이 그대로 드러난다.
//   탭만 쓰면 넓게 흐리려 할 때 탭 사이가 비어 유령상(ghosting)이 생긴다.
//   밉이 거친 평균을 만들고, 탭이 그 사이를 메운다.
//
// ⚠ 탭 간격이 exp2(lod)에 비례하는 것이 이 함수의 핵심이다.
//   LOD n에서 밉 텍셀은 원본의 2^n배다. 간격을 고정하면 네 탭이 중앙과 같은
//   밉 텍셀을 찍어서 5탭이 아무 일도 하지 않는다.
//
// ⚠ UV가 0..1 전체를 쓴다고 가정한다(초상은 spriteMode=Single, 아틀라스 없음).
//   나중에 아틀라스로 묶으면 높은 LOD에서 이웃 스프라이트를 끌어오므로
//   패딩/익스트루드가 필수가 된다.
//
// ⚠ 텍스처에 밉맵이 없으면 lod 인자가 무시되어 아무것도 안 흐려진다.
//   초상 41장은 enableMipMap=1 + Trilinear로 맞춰 두었다.

// 가중치 합 = 0.40 + 0.15*4 = 1.0.
// 합이 1이 아니면 블러가 켜질 때 밝기가 같이 변한다.
#define KED_BLUR_W_CENTER 0.40
#define KED_BLUR_W_SIDE   0.15

float4 KedCharacterBlurSample(
    UnityTexture2D    tex,
    UnitySamplerState smp,
    float2            uv,
    float             amount,        // 0..1 — _BlurAmount
    float             maxLod,        // 최대 밉 단계 (2.0 권장)
    float             radiusTexels)  // 밉 텍셀 단위 탭 간격 (1.5 권장)
{
    float  lod = amount * maxLod;

    // 밉 한 단계마다 텍셀이 2배가 되므로 탭 간격도 같이 벌린다.
    float2 ofs = tex.texelSize.xy * exp2(lod) * radiusTexels;

    float4 c;
    c  = SAMPLE_TEXTURE2D_LOD(tex.tex, smp.samplerstate, uv,                        lod) * KED_BLUR_W_CENTER;
    c += SAMPLE_TEXTURE2D_LOD(tex.tex, smp.samplerstate, uv + float2( ofs.x, 0.0f), lod) * KED_BLUR_W_SIDE;
    c += SAMPLE_TEXTURE2D_LOD(tex.tex, smp.samplerstate, uv + float2(-ofs.x, 0.0f), lod) * KED_BLUR_W_SIDE;
    c += SAMPLE_TEXTURE2D_LOD(tex.tex, smp.samplerstate, uv + float2( 0.0f, ofs.y), lod) * KED_BLUR_W_SIDE;
    c += SAMPLE_TEXTURE2D_LOD(tex.tex, smp.samplerstate, uv + float2( 0.0f,-ofs.y), lod) * KED_BLUR_W_SIDE;

    return c;
}

// Shader Graph Custom Function 진입점.
// 노드의 Name 필드에는 접미사를 뺀 "CharacterBlur"만 적는다.
void CharacterBlur_float(
    UnityTexture2D    Tex,
    UnitySamplerState Sampler,
    float2            UV,
    float             Amount,
    float             MaxLod,
    float             RadiusTexels,
    out float4        RGBA,
    out float         A)
{
    RGBA = KedCharacterBlurSample(Tex, Sampler, UV, Amount, MaxLod, RadiusTexels);
    A    = RGBA.a;
}

// 그래프 정밀도가 Half로 바뀌어도 컴파일되도록 둔다. 내부 계산은 float 그대로다 —
// 밉 텍셀 오프셋이 1e-4 규모라 half로 계산하면 정밀도가 부족하다.
void CharacterBlur_half(
    UnityTexture2D    Tex,
    UnitySamplerState Sampler,
    half2             UV,
    half              Amount,
    half              MaxLod,
    half              RadiusTexels,
    out half4         RGBA,
    out half          A)
{
    float4 c = KedCharacterBlurSample(Tex, Sampler, float2(UV), float(Amount), float(MaxLod), float(RadiusTexels));
    RGBA = half4(c);
    A    = half(c.a);
}

#endif // KED_CHARACTER_BLUR_INCLUDED
