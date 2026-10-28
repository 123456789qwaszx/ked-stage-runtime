using System.Collections.Generic;
using System.Text.RegularExpressions;
using Ked.Presentation.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// StageCommand → CommandSpec 파서 — 표의 생략값과 풀림(한 커맨드 → 여러 스펙)을 고정한다.
// 핸들러 본문은 브리지에서 그대로 옮긴 것이라 값 자체는 종전 식에서 온다.
public class StageCommandParserTests
{
    private static StageCommand Cmd(string name, params string[] args) => new(name, args, "test.yarn:1");

    private static List<CommandSpecBase> Parse(StageCommandParser parser, StageCommand cmd)
    {
        List<CommandSpecBase> output = new();
        Assert.IsTrue(parser.TryParse(cmd, output), $"{cmd}를 몰라서는 안 된다");
        return output;
    }

    private static StageCommandParser NewParser() => new(null, null);

    [Test]
    public void 표에_있는_이름과_프레임_별칭을_안다()
    {
        Assert.IsTrue(StageCommandParser.Knows("move_by"));
        Assert.IsTrue(StageCommandParser.Knows("1fr"));
        Assert.IsTrue(StageCommandParser.Knows($"{StageCommandParser.FramePauseAliasMaxFrame}fr"));
        Assert.IsFalse(StageCommandParser.Knows($"{StageCommandParser.FramePauseAliasMaxFrame + 1}fr"));
        Assert.IsFalse(StageCommandParser.Knows("box_hide"), "대사창은 무대 커맨드가 아니다");
        Assert.IsFalse(StageCommandParser.Knows(null));
    }

    [Test]
    public void 모르는_이름은_false이고_아무것도_내지_않는다()
    {
        List<CommandSpecBase> output = new();

        Assert.IsFalse(NewParser().TryParse(Cmd("nope", "c1"), output));
        Assert.IsEmpty(output);
    }

    [Test]
    public void 생략한_인자는_표의_생략값으로_채운다()
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("pause"));

        Assert.AreEqual(1, specs.Count);
        Assert.AreEqual(0.18f, ((WaitCommandSpec)specs[0]).duration, 1e-6f);

        specs = Parse(NewParser(), Cmd("24fr"));
        Assert.AreEqual(1f, ((WaitCommandSpec)specs[0]).duration, 1e-6f, "24fps 기준 24fr = 1초");
    }

    [Test]
    public void 빈_문자열_인자도_생략으로_친다()
    {
        // Yarn의 생략 인자가 빈 문자열로 오는 경우가 있다 — StageCommand.Arg 규약과 같다.
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("move_by", "c1", "", "", "", ""));

        MoveByCommandSpecCharR move = (MoveByCommandSpecCharR)specs[0];

        Assert.AreEqual(Vector2.zero, move.delta);
        Assert.AreEqual(0.4f, move.duration, 1e-6f);
    }

    [Test]
    public void 한_커맨드가_스펙_여럿으로_풀린다()
    {
        // show = 앵커 리셋 · 초상 · 리그 페이드 · 초상 페이드, 이 순서.
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("show", "c1"));

        Assert.AreEqual(4, specs.Count);
        Assert.IsInstanceOf<SetAnchorCommandSpecCharR>(specs[0]);
        Assert.IsInstanceOf<SetPortraitSpriteCommandSpecCharR>(specs[1]);
        Assert.IsInstanceOf<FadeInCommandSpecCharR>(specs[2]);
        Assert.IsInstanceOf<FadeInCommandSpecCharR>(specs[3]);
        Assert.AreEqual(CharacterRigTarget.RigRoot, ((FadeInCommandSpecCharR)specs[2]).target);
        Assert.AreEqual(CharacterRigTarget.CharacterPortraitSprite_Root, ((FadeInCommandSpecCharR)specs[3]).target);
    }

    [Test]
    public void 숫자가_아닌_숫자_인자는_경고하고_생략값으로_간다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("StageCommandParser.*숫자가 아니다"));

        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("shot_zoom", "abc"));

        Assert.AreEqual(1f, ((ShotZoomCommandSpec)specs[0]).zoom, 1e-6f);
    }

    [Test]
    public void 출력_리스트_뒤에_붙인다()
    {
        List<CommandSpecBase> output = new() { new WaitCommandSpec() };

        NewParser().TryParse(Cmd("fade_in", "c1"), output);

        Assert.AreEqual(2, output.Count);
        Assert.IsInstanceOf<FadeInCommandSpecCharR>(output[1]);
    }

    [Test]
    public void motion은_저장소의_클립을_그대로_싣고_모르는_이름은_오류_후_아무것도_내지_않는다()
    {
        const string json = @"{ ""schema"": ""motions/2"", ""motions"": [ { ""name"": ""nod"", ""rig"": ""character"", ""duration"": 0.6, ""end"": ""restore"",
            ""channels"": [ { ""node"": ""CharacterPortrait_Track_Move_Y"", ""kind"": ""position"",
            ""y"": [ { ""t"": 0, ""v"": 0 }, { ""t"": 0.6, ""v"": 0 } ] } ] } ] }";

        StageCommandParser parser = new(null, null, null, MotionLibrary.Parse(json, "test"));

        List<CommandSpecBase> specs = Parse(parser, Cmd("motion", "c1", "nod"));
        MotionCommandSpecCharR spec = (MotionCommandSpecCharR)specs[0];

        Assert.AreEqual("c1", spec.slotKey);
        Assert.AreEqual("nod", spec.clip.Name);
        Assert.AreEqual(0.6f, spec.clip.Duration, 0f);
        Assert.AreEqual(MotionChannelMode.Relative, spec.mode, "생략한 mode는 relative");

        MotionCommandSpecCharR absolute = (MotionCommandSpecCharR)Parse(parser, Cmd("motion", "c1", "nod", "absolute"))[0];
        Assert.AreEqual(MotionChannelMode.Absolute, absolute.mode);

        LogAssert.Expect(LogType.Error, new Regex("StageCommandParser.*Unknown motion"));
        Assert.IsEmpty(Parse(parser, Cmd("motion", "c1", "nope")), "기본 모션은 없다 — 아무것도 내지 않는다");

        LogAssert.Expect(LogType.Error, new Regex("StageCommandParser.*mode"));
        Assert.IsEmpty(Parse(parser, Cmd("motion", "c1", "nod", "offset")), "모르는 mode 낱말은 재생하지 않는다");
    }
}
