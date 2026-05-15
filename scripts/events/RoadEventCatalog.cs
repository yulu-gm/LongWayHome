using System.Collections.Generic;
using Godot_V2.Scripts.Gameplay;

namespace Godot_V2.Scripts.Events;

public static class RoadEventCatalog
{
    public static IReadOnlyList<RoadEventDefinition> CreateMvpEvents() =>
    [
        Event(
            "broken-car",
            "抛锚车辆",
            "一辆旧车停在路肩，车主打开引擎盖，向经过的车辆挥手。",
            ["road", "vehicle", "traveler"],
            RoadEventCategory.Road,
            7,
            new RoadEventTrigger(RequiredTags: ["road"]),
            Choice("help", "停下帮忙，得到一点报酬。", Money(24), Energy(-6), Time(25), Flag("helped-broken-car")),
            Choice("leave", "继续赶路，只放慢速度避让。", Time(5))),
        Event(
            "hitchhiker",
            "搭车者",
            "路边有人背着包，举着写着下个镇名的纸牌。",
            ["road", "traveler", "story"],
            RoadEventCategory.Road,
            6,
            new RoadEventTrigger(RequiredTags: ["road"]),
            Choice("pick-up", "载他一段，听一条小镇传闻。", Energy(-4), Time(18), Flag("heard-hitchhiker-rumor")),
            Choice("decline", "礼貌拒绝，继续保持节奏。", Time(3))),
        Event(
            "roadside-vendor",
            "路边商贩",
            "一辆小货车旁摆着折叠桌，卖咖啡、零食和一罐备用油。",
            ["road", "shop", "fuel"],
            RoadEventCategory.Road,
            6,
            new RoadEventTrigger(RequiredTags: ["road"]),
            Choice("buy-supplies", "买些补给。", Money(-18), Energy(10), Flag("bought-roadside-supplies")),
            Choice("buy-fuel-can", "买一罐备用油。", Money(-24), Fuel(8), Flag("bought-fuel-can"))),
        Event(
            "temporary-construction",
            "临时施工",
            "前方道路半幅封闭，临时信号灯让车流一批一批通过。",
            ["road", "construction", "delay"],
            RoadEventCategory.Road,
            5,
            new RoadEventTrigger(RequiredTags: ["road"]),
            Choice("wait", "排队等待。", Time(35), Energy(-3)),
            Choice("rough-shoulder", "从碎石路肩慢慢绕过。", Time(15), Fuel(-2), Part("Suspension", -6))),
        Event(
            "rain-night-detour",
            "雨夜封路",
            "夜里的雨越下越密，导航提示前方低洼路段封闭。",
            ["road", "rain", "night", "visibility"],
            RoadEventCategory.Road,
            8,
            new RoadEventTrigger(RequiredTags: ["rain", "night"], RequiredWeather: TripWeather.Rain, RequiredTimeOfDay: TimeOfDayPhase.Night),
            Choice("detour", "绕行旧路。", Fuel(-5), Energy(-8), Time(40), Flag("took-rain-detour")),
            Choice("wait-out-rain", "靠边等雨小一点。", Energy(4), Time(60))),
        Event(
            "fuel-price-rumor",
            "油价上涨传闻",
            "收音机里说邻近县的油价今晚会涨，路边司机也在讨论这件事。",
            ["fuel", "rumor", "long-term"],
            RoadEventCategory.LongTerm,
            7,
            new RoadEventTrigger(RequiredTags: ["fuel"]),
            Choice("note-rumor", "记下传闻，优先找加油站。", Flag("rumor-fuel-price-rise"), Time(5)),
            Choice("top-up", "先买一小罐备用油。", Money(-20), Fuel(6), Flag("prepared-for-fuel-price-rise"))),
        Event(
            "abandoned-town-rumor",
            "废弃小镇传闻",
            "加油站墙上贴着一张旧地图，有人圈出了公路外的一座废弃小镇。",
            ["old-road", "rumor", "map"],
            RoadEventCategory.LongTerm,
            5,
            new RoadEventTrigger(RequiredTags: ["old-road"]),
            Choice("mark-map", "把位置标到地图上。", Time(10), Flag("rumor-abandoned-town")),
            Choice("ignore-map", "不绕远，继续原计划。", Energy(1))),
        Event(
            "tire-noise",
            "轮胎异响",
            "右后方传来有节奏的轻响，像有什么东西卡进了胎纹。",
            ["road", "vehicle", "tires"],
            RoadEventCategory.Condition,
            6,
            new RoadEventTrigger(RequiredTags: ["road"]),
            Choice("inspect", "停车检查轮胎。", Time(15), Part("Tires", 4)),
            Choice("push-on", "先继续开，等服务点再看。", Part("Tires", -10), Flag("ignored-tire-noise"))),
        Event(
            "lights-flicker",
            "车灯闪烁",
            "远光灯忽明忽暗，雨夜里的白线变得难以判断。",
            ["road", "rain", "night", "visibility", "lights", "vehicle"],
            RoadEventCategory.Condition,
            7,
            new RoadEventTrigger(RequiredTags: ["rain", "night"], RequiredWeather: TripWeather.Rain, RequiredTimeOfDay: TimeOfDayPhase.Night),
            Choice("replace-fuse", "换一枚保险丝。", Money(-12), Time(12), Part("Lights", 8)),
            Choice("drive-slow", "降低速度继续开。", Energy(-8), Time(25), Flag("nursed-flickering-lights"))),
        Event(
            "late-night-radio",
            "深夜电台",
            "电台忽然切进一段深夜节目，主持人慢慢念着明天的天气和一条奇怪的寻物启事。",
            ["night", "cabin", "radio"],
            RoadEventCategory.Cabin,
            5,
            new RoadEventTrigger(RequiredTags: ["night"], RequiredTimeOfDay: TimeOfDayPhase.Night),
            Choice("listen", "听完整段节目。", Energy(5), Time(12), Flag("heard-late-night-radio")),
            Choice("switch-off", "关掉收音机，专心看路。", Energy(-2))),
        Event(
            "motel-stranger",
            "汽车旅馆陌生人",
            "办理入住时，前台旁的陌生人问你是否愿意顺路带一封信到下个镇。",
            ["motel", "traveler", "place"],
            RoadEventCategory.Place,
            5,
            new RoadEventTrigger(RequiredTags: ["motel"]),
            Choice("take-letter", "接下这封信。", Money(10), Flag("carrying-stranger-letter")),
            Choice("refuse-letter", "拒绝卷入陌生人的事。", Energy(3), Time(5))),
        Event(
            "old-tape",
            "旧磁带",
            "清理副驾驶储物格时，你摸到一盒没有标签的旧磁带。",
            ["cabin", "memory", "item"],
            RoadEventCategory.Cabin,
            4,
            new RoadEventTrigger(RequiredTags: ["cabin"]),
            Choice("play-tape", "放进磁带机听听。", Energy(8), Flag("played-old-tape")),
            Choice("trade-tape", "留到路边摊换点东西。", Money(12), Energy(-2), Flag("traded-old-tape")))
    ];

    private static RoadEventDefinition Event(
        string id,
        string title,
        string sceneText,
        IReadOnlyList<string> tags,
        RoadEventCategory category,
        int weight,
        RoadEventTrigger trigger,
        params RoadEventChoice[] choices) =>
        new(
            id,
            title,
            sceneText,
            category,
            weight,
            CooldownLegs: 2,
            IsOneTime: category is RoadEventCategory.LongTerm or RoadEventCategory.Cabin,
            tags,
            trigger,
            choices);

    private static RoadEventChoice Choice(
        string id,
        string text,
        params RoadEventEffect[] effects) =>
        new(id, text, effects);

    private static RoadEventEffect Fuel(float amount) => new(RoadEventEffectKind.FuelLiters, amount);

    private static RoadEventEffect Money(float amount) => new(RoadEventEffectKind.Money, amount);

    private static RoadEventEffect Energy(float amount) => new(RoadEventEffectKind.Energy, amount);

    private static RoadEventEffect Time(float minutes) => new(RoadEventEffectKind.TimeMinutes, minutes);

    private static RoadEventEffect Part(string part, float amount) => new(RoadEventEffectKind.VehiclePartCondition, amount, part);

    private static RoadEventEffect Flag(string flag) => new(RoadEventEffectKind.Flag, 1, flag);
}
