using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// 코어 SlideMotion ↔ 런타임 슬라이드 실경로 등가.
//
// 여기가 등가의 심판이다: 런타임 어셈블리(CharRigDirectionParser · SlideCommandCharR ·
// 커맨드 스펙)를 직접 참조할 수 있는 유일한 자리다. 저작 도구 쪽은 코어만 보므로
// "프리뷰가 그리는 슬라이드 = 게임이 재생하는 슬라이드"의 근거가 이 파일이다.
//
// 왜 필요한가 — 슬라이드의 값은 <b>세 군데</b>에서 만난다: 낱말을 읽는 파서(런타임),
// 방향 벡터와 튐 모양(코어), 그 둘을 쓰는 커맨드 스펙(런타임). 하나만 움직여도 게임과
// 프리뷰가 조용히 갈리고, 어긋남은 "왜 반대로 나가지?"로만 드러난다.
// ─────────────────────────────────────────────────────────────────────────────
public class SlideMotionParityTests
{
    private const float Tolerance = 1e-5f;

    /// <summary>파서가 아는 낱말 전부 — 대표 낱말·별칭·대소문자·공백·모르는 낱말·빈 값.</summary>
    private static readonly string[] Words =
    {
        "left", "l", "right", "r",
        "up", "u", "top", "t",
        "down", "d", "bottom", "b",
        "LEFT", " Right ", "왼쪽", "", null,
    };

    /// <summary>런타임 실경로: 낱말 → 열거형 → 벡터. 커맨드가 실제로 지나는 사슬이다.</summary>
    private static Vector2 RuntimeVector(string word)
        => SlideCommandCharR.DirectionToVector(CharRigDirectionParser.ParseSlideDirection(word));

    [Test]
    public void 방향_낱말이_런타임_사슬과_같은_벡터를_낸다()
    {
        foreach (string word in Words)
        {
            Vec2 core = SlideMotion.DirectionVector(word);
            Vector2 runtime = RuntimeVector(word);

            Assert.That(core.X, Is.EqualTo(runtime.x).Within(Tolerance), $"'{word}' 의 x");
            Assert.That(core.Y, Is.EqualTo(runtime.y).Within(Tolerance), $"'{word}' 의 y");
        }
    }

    [Test]
    public void 모르는_낱말은_양쪽_모두_left로_물러선다()
    {
        // 이 폴백이 갈리면 오타 한 자에 캐릭터가 반대편에서 등장한다 — 오류는 안 난다.
        Assert.That(SlideMotion.DirectionVector("없는방향").X, Is.EqualTo(-1f).Within(Tolerance));
        Assert.That(RuntimeVector("없는방향").x, Is.EqualTo(-1f).Within(Tolerance));
    }

    [Test]
    public void 튐_모양이_런타임_계산과_같다()
    {
        // 지금은 런타임이 코어를 부르므로 동어반복처럼 보이지만, 그것이 이 고정의 뜻이다:
        // 누군가 여기에 사본을 다시 세우면 <b>그 순간</b> 이 줄이 운다.
        for (int i = 0; i <= 256; i++)
        {
            float t = i / 256f;

            Assert.That(
                SlideMotion.PunchTowardEnd(t),
                Is.EqualTo(SlideMotion.Punch(SlideMotion.Kind.In, t)).Within(Tolerance),
                $"등장 튐 t={t}");

            Assert.That(
                SlideMotion.PunchFromStart(t),
                Is.EqualTo(SlideMotion.Punch(SlideMotion.Kind.Out, t)).Within(Tolerance),
                $"퇴장 튐 t={t}");
        }
    }

    [Test]
    public void 튐은_양_끝에서_0이고_범위_밖은_클램프다()
    {
        // 순변위를 바꾸지 않는다는 불변식 — 깨지면 슬라이드가 도착점을 옮긴다.
        foreach (float t in new[] { -1f, 0f, 1f, 2f })
        {
            Assert.That(SlideMotion.PunchTowardEnd(t), Is.EqualTo(0f).Within(Tolerance), $"등장 t={t}");
            Assert.That(SlideMotion.PunchFromStart(t), Is.EqualTo(0f).Within(Tolerance), $"퇴장 t={t}");
        }
    }

    [Test]
    public void ease와_punch를_생략하면_종류별_코어_값이다()
    {
        // 대본이 ease · punch를 안 주면 파서가 코어 상수로 채운다 — 프리뷰가 그리는 모양과 같다.
        SlideCommandSpecCharR slideIn = ParseSlide("slide_in", "c1");
        SlideCommandSpecCharR slideOut = ParseSlide("slide_out", "c1");

        Assert.That(slideIn.kind, Is.EqualTo(SlideMotion.Kind.In));
        Assert.That(slideOut.kind, Is.EqualTo(SlideMotion.Kind.Out));

        Assert.That(slideIn.ease.ToString(), Is.EqualTo(SlideMotion.InEase.ToString()));
        Assert.That(slideOut.ease.ToString(), Is.EqualTo(SlideMotion.OutEase.ToString()));

        Assert.That(slideIn.punch, Is.EqualTo(SlideMotion.InPunchPixels).Within(Tolerance));
        Assert.That(slideOut.punch, Is.EqualTo(SlideMotion.OutPunchPixels).Within(Tolerance));

        // 표적 rect도 같아야 한다 — 다른 노드를 밀면 자리가 어긋난다.
        Assert.That(slideIn.node, Is.EqualTo(CharacterRigNode.CharSlot_Track));
        Assert.That(slideOut.node, Is.EqualTo(CharacterRigNode.CharSlot_Track));

        // 기본 방향도 브리지 시그니처와 같다(등장 left · 퇴장 right).
        Assert.That(
            SlideMotion.DirectionVector(SlideMotion.DefaultDirection(SlideMotion.Kind.In)).X,
            Is.EqualTo(-1f).Within(Tolerance));
        Assert.That(
            SlideMotion.DirectionVector(SlideMotion.DefaultDirection(SlideMotion.Kind.Out)).X,
            Is.EqualTo(+1f).Within(Tolerance));
    }

    [Test]
    public void 대본이_준_ease와_punch를_싣는다()
    {
        SlideCommandSpecCharR spec = ParseSlide("slide_in", "c1", "left", "12u", "10fr", "OutBack", "1u");

        Assert.That(spec.ease.ToString(), Is.EqualTo("OutBack"));
        Assert.That(spec.punch, Is.EqualTo(UnitToken.UnitsToPixels(1f, UnitToken.DefaultReferenceStageWidth)).Within(Tolerance));
    }

    private static SlideCommandSpecCharR ParseSlide(string name, params string[] args)
    {
        var output = new System.Collections.Generic.List<CommandSpecBase>();
        Assert.IsTrue(new StageCommandParser(null).TryParse(new StageCommand(name, args, "test.yarn:1"), output));

        return (SlideCommandSpecCharR)output[0];
    }
}
