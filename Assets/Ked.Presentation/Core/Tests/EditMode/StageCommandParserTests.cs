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

    // (리그 프리팹, 이징 곡선). 배경 리그 프리팹 자리는 2026-10-29에 없어졌다 — 배경도 캐릭터 리그다.
    private static StageCommandParser NewParser() => new(null, null);

    [Test]
    public void 표에_있는_이름과_프레임_별칭을_안다()
    {
        Assert.IsTrue(StageCommandParser.Knows("move_by"));
        Assert.IsTrue(StageCommandParser.Knows("tx"));
        Assert.IsTrue(StageCommandParser.Knows("pause"));
        Assert.IsTrue(StageCommandParser.Knows("1fr"));
        Assert.IsTrue(StageCommandParser.Knows($"{StageCommandParser.FramePauseAliasMaxFrame}fr"));
        Assert.IsFalse(StageCommandParser.Knows($"{StageCommandParser.FramePauseAliasMaxFrame + 1}fr"));
        Assert.IsFalse(StageCommandParser.Knows("surface_reset"), "대사창은 무대 커맨드가 아니다");
        Assert.IsFalse(StageCommandParser.Knows(null));
    }

    // 2026-11-03: 인자 하나를 이름에 박아 다시 부르던 이름은 걷었다.
    // 예외(소유자 결정): place_left·center·right 셋, 프레임 대기 <<1fr>>~<<48fr>>, char_visual_<효과> 일곱.
    [TestCase("slot01")]
    [TestCase("char_to_s1")]
    [TestCase("bg_slot01")]
    [TestCase("screen_blur_s1")]
    [TestCase("place_tl")]
    [TestCase("place_inner_br")]
    [TestCase("size_close")]
    [TestCase("tx_daze_in")]
    [TestCase("tx_hstrip_open")]
    [TestCase("ui_patch")]
    public void 걷은_별칭은_모른다(string name)
    {
        Assert.IsFalse(StageCommandParser.Knows(name));
    }

    [TestCase("place_left")]
    [TestCase("place_center")]
    [TestCase("place_right")]
    [TestCase("char_visual")]
    [TestCase("char_visual_focus")]
    [TestCase("char_visual_defocus")]
    [TestCase("char_visual_dim")]
    [TestCase("char_visual_silhouette")]
    [TestCase("char_visual_inner_rim")]
    [TestCase("char_visual_outer_rim")]
    [TestCase("char_visual_clear")]
    public void 남긴_예외는_안다(string name)
    {
        Assert.IsTrue(StageCommandParser.Knows(name));
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

        specs = Parse(NewParser(), Cmd("pause", "24fr"));
        Assert.AreEqual(1f, ((WaitCommandSpec)specs[0]).duration, 1e-6f, "24fps 기준 24fr = 1초 — 다른 커맨드와 같은 시간 토큰");

        specs = Parse(NewParser(), Cmd("pause", "0.5"));
        Assert.AreEqual(0.5f, ((WaitCommandSpec)specs[0]).duration, 1e-6f, "맨 숫자는 초");
    }

    [TestCase("tl", ScreenFocusPoint.TopLeft)]
    [TestCase("bottom", ScreenFocusPoint.Bottom)]
    [TestCase("inner_tl", ScreenFocusPoint.ThirdsUpperLeft)]
    [TestCase("inner_br", ScreenFocusPoint.ThirdsLowerRight)]
    public void place는_셋째_인자로_화면_지점을_받는다_코어와_같은_이름(string point, ScreenFocusPoint expected)
    {
        // 옛 place_tl · place_inner_tl … 은 이 인자로 내려왔다. 코어 FocusStageReductions의 이름과 같아야
        // 재생과 정지 프레임이 같은 자리를 본다(전에는 inner_tl을 런타임만 몰라 조용히 가운데로 갔다).
        PlaceCharacterFocusCommandSpecCharR place =
            (PlaceCharacterFocusCommandSpecCharR)Parse(NewParser(), Cmd("place", "c1", "face", point))[0];

        Assert.AreEqual(expected, place.screenPoint);
    }

    [Test]
    public void place의_모르는_화면_지점은_경고하고_가운데로()
    {
        LogAssert.Expect(LogType.Warning, new Regex("화면 지점 'nowhere'을 모른다"));

        PlaceCharacterFocusCommandSpecCharR place =
            (PlaceCharacterFocusCommandSpecCharR)Parse(NewParser(), Cmd("place", "c1", "face", "nowhere"))[0];

        Assert.AreEqual(ScreenFocusPoint.Center, place.screenPoint);
    }

    [Test]
    public void tx는_프리셋과_무대와_시간을_받는다()
    {
        StageMaskMotionCommandSpec a = (StageMaskMotionCommandSpec)Parse(NewParser(), Cmd("tx", "daze_close", "00"))[0];
        Assert.AreEqual("daze_close", a.presetKey);
        Assert.AreEqual(PresentationStageKey.Stage00, a.stage);
        Assert.AreEqual(-1f, a.durationOverride, "시간이 비면 프리셋 자체 시간");

        StageMaskMotionCommandSpec b = (StageMaskMotionCommandSpec)Parse(NewParser(), Cmd("tx", "iris_in", "", "12fr"))[0];
        Assert.AreEqual(PresentationStageKey.Stage01, b.stage, "무대 생략값은 01");
        Assert.AreEqual(0.5f, b.durationOverride, 1e-6f);
    }

    [Test]
    public void tx는_프리셋이_없으면_경고하고_아무것도_내지_않는다()
    {
        LogAssert.Expect(LogType.Warning, new Regex("<<tx>>에 프리셋이 없다"));

        List<CommandSpecBase> output = new();
        Assert.IsTrue(NewParser().TryParse(Cmd("tx"), output), "이름은 안다");
        Assert.AreEqual(0, output.Count);
    }

    [Test]
    public void screen_blur는_셋째_인자로_무대를_받는다()
    {
        StageLayerBlurCommandSpec a = (StageLayerBlurCommandSpec)Parse(NewParser(), Cmd("screen_blur", "back", "0.5"))[0];
        Assert.AreEqual(PresentationStageKey.Stage00, a.stage, "무대 생략값은 stage00");
        Assert.AreEqual(PresentationDepthLayerKey.Back, a.layer);
        Assert.AreEqual(0.5f, a.amount, 1e-6f);

        StageLayerBlurCommandSpec b = (StageLayerBlurCommandSpec)Parse(NewParser(), Cmd("screen_blur", "mid", "1", "stage02"))[0];
        Assert.AreEqual(PresentationStageKey.Stage02, b.stage);
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

        StageCommandParser parser = new(null, null, MotionLibrary.Parse(json, "test"));

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

    // ── 배경 = 캐스팅하지 않은 캐릭터 슬롯 ─────────────────────────────
    // 코어 StageReducer.Background와 같은 노드·같은 값이어야 등가성 하네스가 맞는다.

    [Test]
    public void bg_spawn은_far에_배경으로_세우고_그림을_넣는다_보이게는_bg_fade_in이()
    {
        List<CommandSpecBase> specs = Parse(NewParser(), Cmd("bg_spawn", "bg_main", "class_day"));

        Assert.AreEqual(2, specs.Count, "캐릭터 slot처럼 숨은 채 선다 — 가시성 스펙은 없다");

        SetupCharRigCommandSpec setup = (SetupCharRigCommandSpec)specs[0];
        Assert.AreEqual("bg_main", setup.roleKey);
        Assert.AreEqual(PresentationStageKey.Stage00, setup.stage);
        Assert.AreEqual(PresentationDepthLayerKey.Far, setup.layer);
        Assert.IsTrue(setup.asBackground, "배역 있는 슬롯을 덮으면 배역을 푸는 경로");

        SetSpriteCommandSpecCharR sprite = (SetSpriteCommandSpecCharR)specs[1];
        Assert.AreEqual("bg_main", sprite.slotKey);
        Assert.AreEqual(CharacterRigTarget.CharacterPortraitSprite_Image, sprite.target);
        Assert.AreEqual(ResourcesSpriteResolver.BackgroundsRoot, sprite.resourcesRoot);
        Assert.AreEqual("class_day", sprite.spriteKey);
        Assert.AreEqual(CharRigImageSizingMode.HeightFitPreserveAspect, sprite.sizingMode);
        Assert.IsFalse(sprite.raycastTarget, "화면 전체를 덮는 그림은 클릭을 받지 않는다");
    }

    [Test]
    public void bg_spawn은_셋째_넷째_인자로_무대와_층을_받는다()
    {
        SetupCharRigCommandSpec a = (SetupCharRigCommandSpec)Parse(NewParser(), Cmd("bg_spawn", "bg_a", "class_day", "stage01"))[0];
        Assert.AreEqual(PresentationStageKey.Stage01, a.stage);
        Assert.AreEqual(PresentationDepthLayerKey.Far, a.layer, "층 생략값은 far");

        SetupCharRigCommandSpec b = (SetupCharRigCommandSpec)Parse(NewParser(), Cmd("bg_spawn", "bg_b", "class_day", "stage02", "mid"))[0];
        Assert.AreEqual(PresentationStageKey.Stage02, b.stage);
        Assert.AreEqual(PresentationDepthLayerKey.Mid, b.layer);
    }

    [Test]
    public void bg_fade_move_scale은_캐릭터와_같은_노드를_잡는다()
    {
        FadeOutCommandSpecCharR fade = (FadeOutCommandSpecCharR)Parse(NewParser(), Cmd("bg_fade_out", "bg_main"))[0];
        Assert.AreEqual(CharacterRigTarget.CharacterPortraitSprite_Root, fade.target);
        Assert.AreEqual(10f / 24f, fade.duration, 1e-5f, "생략값 10fr");

        MoveByCommandSpecCharR move = (MoveByCommandSpecCharR)Parse(NewParser(), Cmd("bg_move", "bg_main", "2u", "-1u"))[0];
        Assert.AreEqual(CharacterRigTarget.CharSlot_Track, move.target, "move_by와 같은 축");
        Assert.IsFalse(move.useAbsolutePosition);
        Assert.Less(move.delta.y, 0f, "부호 있는 단위");

        ScaleToCommandSpecCharR scale = (ScaleToCommandSpecCharR)Parse(NewParser(), Cmd("bg_scale", "bg_main", "1.2"))[0];
        Assert.AreEqual(CharacterRigTarget.CharacterPortrait_ActingScale, scale.target, "가운데 피벗 — CharSlot_Scale은 발 피벗");
        Assert.IsFalse(scale.relativeToCurrent);
        Assert.AreEqual(1.2f, scale.toScale.x, 1e-6f);
    }

    [Test]
    public void 지운_bg_size와_bg_place는_모른다()
    {
        Assert.IsFalse(StageCommandParser.Knows("bg_size"));
        Assert.IsFalse(StageCommandParser.Knows("bg_place"));

        SetSpriteCommandSpecCharR sprite = (SetSpriteCommandSpecCharR)Parse(
            NewParser(), Cmd("bg_sprite", "bg_main", "studio_evening", "back"))[0];
        Assert.AreEqual("studio_evening", sprite.spriteKey, "셋째 인자는 더 읽지 않는다");
    }
}
