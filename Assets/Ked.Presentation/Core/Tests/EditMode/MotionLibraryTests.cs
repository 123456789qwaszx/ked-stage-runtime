using System.Text.RegularExpressions;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// motions.json 로더 — 스키마 수용, kind·mode 낱말, 성분 생략, 모션 단위 검증(어긋난 것은 로그 + 무시).
public class MotionLibraryTests
{
    private const string Nod = @"{ ""name"": ""nod"", ""rig"": ""character"", ""duration"": 0.6,
        ""channels"": [
          { ""node"": ""CharacterPortrait_Track_Move_Y"", ""kind"": ""position"", ""mode"": ""offset"",
            ""y"": [ { ""t"": 0, ""v"": 0, ""inTangent"": 0, ""outTangent"": -120 },
                     { ""t"": 0.3, ""v"": -18, ""inTangent"": 0, ""outTangent"": 0 },
                     { ""t"": 0.6, ""v"": 0, ""inTangent"": 60, ""outTangent"": 0 } ] },
          { ""node"": ""CharacterPortrait_SwayPivot"", ""kind"": ""Euler"", ""mode"": ""ABSOLUTE"",
            ""z"": [ { ""t"": 0, ""v"": 3 }, { ""t"": 0.6, ""v"": 0 } ] }
        ] }";

    private static string Doc(params string[] motions)
        => @"{ ""schema"": ""motions/1"", ""motions"": [" + string.Join(",", motions) + "] }";

    [Test]
    public void 유효한_문서를_읽고_채널과_키가_그대로_나온다()
    {
        MotionLibrary library = MotionLibrary.Parse(Doc(Nod), "test");

        Assert.AreEqual(1, library.Count);
        Assert.IsTrue(library.TryGet("nod", out MotionClip clip));
        Assert.AreEqual("character", clip.Rig);
        Assert.AreEqual(0.6f, clip.Duration, 0f);
        Assert.AreEqual(2, clip.Channels.Length);

        MotionChannel y = clip.Channels[0];
        Assert.AreEqual(MotionChannelKind.Position, y.Kind);
        Assert.AreEqual(MotionChannelMode.Offset, y.Mode);
        Assert.IsFalse(y.HasX, "생략한 성분은 null이다 — JsonUtility의 빈 리스트를 null로 읽는다");
        Assert.AreEqual(3, y.Y.Length);
        Assert.AreEqual(-120f, y.Y[0].OutTangent, 0f);
        Assert.AreEqual(-18f, y.Y[1].Value, 0f);

        MotionChannel z = clip.Channels[1];
        Assert.AreEqual(MotionChannelKind.Euler, z.Kind, "kind는 대소문자를 가리지 않는다");
        Assert.AreEqual(MotionChannelMode.Absolute, z.Mode);
        Assert.IsTrue(z.HasZ);

        Assert.IsTrue(library.Clips.ContainsKey("nod"), "리듀서 튜닝에 그대로 넘기는 사전");
    }

    [TestCase(@"{ ""name"": ""x"", ""duration"": 1, ""channels"": [ { ""node"": ""CharSlot_Track_X"", ""kind"": ""slide"", ""mode"": ""offset"", ""x"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 1, ""v"": 1 } ] } ] }", "kind")]
    [TestCase(@"{ ""name"": ""x"", ""duration"": 1, ""channels"": [ { ""node"": ""CharSlot_Track_X"", ""kind"": ""position"", ""x"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 1, ""v"": 1 } ] } ] }", "mode")]
    [TestCase(@"{ ""name"": ""x"", ""duration"": 1, ""channels"": [ { ""node"": ""CharSlot_Track_Focus"", ""kind"": ""position"", ""mode"": ""offset"", ""x"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 1, ""v"": 1 } ] } ] }", "Track_Focus")]
    [TestCase(@"{ ""name"": ""x"", ""duration"": 1, ""channels"": [ { ""node"": ""CharSlot_Track_X"", ""kind"": ""position"", ""mode"": ""offset"", ""x"": [ { ""t"": 0, ""v"": 5 }, { ""t"": 1, ""v"": 1 } ] } ] }", "offset")]
    [TestCase(@"{ ""name"": ""x"", ""duration"": 1, ""channels"": [ { ""node"": ""CharSlot_Track_X"", ""kind"": ""position"", ""mode"": ""offset"", ""x"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 0.5, ""v"": 1 } ] } ] }", "길이")]
    public void 어긋난_모션은_경고하고_무시한다(string motionJson, string expectInWarning)
    {
        LogAssert.Expect(LogType.Warning, new Regex("MotionLibrary.*무시.*" + Regex.Escape(expectInWarning)));

        MotionLibrary library = MotionLibrary.Parse(Doc(motionJson), "test");

        Assert.AreEqual(0, library.Count);
        Assert.IsFalse(library.TryGet("x", out _));
    }

    [Test]
    public void 스키마_노드_목록을_주면_없는_노드를_거른다()
    {
        string stray = @"{ ""name"": ""stray"", ""duration"": 1, ""channels"": [ { ""node"": ""NoSuchNode"", ""kind"": ""position"", ""mode"": ""offset"", ""x"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 1, ""v"": 1 } ] } ] }";

        Assert.AreEqual(1, MotionLibrary.Parse(Doc(stray), "test").Count, "목록이 없으면 노드는 보지 않는다");

        LogAssert.Expect(LogType.Warning, new Regex("MotionLibrary.*스키마"));
        Assert.AreEqual(0, MotionLibrary.Parse(Doc(stray), "test", CharacterRigSchema.NodeIds()).Count);

        Assert.AreEqual(1, MotionLibrary.Parse(Doc(Nod), "test", CharacterRigSchema.NodeIds()).Count, "실제 스키마의 노드는 통과한다");
    }

    [Test]
    public void 같은_이름이_둘이면_뒤의_것을_무시한다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("MotionLibrary.*둘"));

        MotionLibrary library = MotionLibrary.Parse(Doc(Nod, Nod), "test");

        Assert.AreEqual(1, library.Count);
    }

    [Test]
    public void 깨진_JSON과_빈_문서는_오류를_내고_빈_저장소다()
    {
        LogAssert.Expect(LogType.Error, new Regex("MotionLibrary"));
        Assert.AreEqual(0, MotionLibrary.Parse("이건 JSON이 아니다 {", "test").Count);

        Assert.AreEqual(0, MotionLibrary.Parse(@"{ ""schema"": ""motions/1"" }", "test").Count);
        Assert.AreSame(MotionLibrary.Empty, MotionLibrary.Parse("이건 JSON이 아니다 {", "test"));
    }
}
