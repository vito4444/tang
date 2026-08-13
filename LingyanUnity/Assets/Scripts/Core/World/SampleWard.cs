using System.Collections.Generic;
using Lingyan.Core.Reputation;

namespace Lingyan.Core.World
{
    /// <summary>
    /// 阶段 2 示例坊：槐里坊（虚构坊名；槐取三槐九棘之意）。
    /// 布局遵守初唐坊市制：坊内无市——饼师白日出坊赴西市鬻饼，
    /// 暮鼓归坊；武侯昼在铺当值、夜巡诸巷。作息本身就是制度的演示。
    /// </summary>
    public static class SampleWard
    {
        public static readonly WardDef Ward = new WardDef(
            "huaili", "ward.huaili",
            new[]
            {
                new PlaceDef("home_kang", "place.home_kang"),
                new PlaceDef("home_huan", "place.home_huan"),
                new PlaceDef("well", "place.well"),
                new PlaceDef("south_gate", "place.south_gate", isGate: true),
                new PlaceDef("wuhou_post", "place.wuhou_post"),
                new PlaceDef("ward_lanes", "place.ward_lanes"),
                new PlaceDef("west_market", "place.west_market")
            });

        /// <summary>
        /// 时辰序：子0 丑1 寅2 卯3 辰4 巳5 午6 未7 申8 酉9 戌10 亥11。
        /// </summary>
        public static readonly IReadOnlyList<NpcScheduleDef> Npcs = new[]
        {
            // 饼师康三（粟特人）：寅时起炉，卯时候坊门，白日西市鬻饼，暮鼓归坊
            new NpcScheduleDef("kang_san", "npc.kang_san", NpcArchetype.Merchant, new[]
            {
                new ScheduleEntry(2, 3, "home_kang", "activity.baking"),
                new ScheduleEntry(3, 4, "south_gate", "activity.await_gate"),
                new ScheduleEntry(4, 9, "west_market", "activity.selling"),
                new ScheduleEntry(9, 10, "south_gate", "activity.return_ward"),
                new ScheduleEntry(10, 2, "home_kang", "activity.sleeping")
            }),

            // 武侯郑五：昼在武侯铺当值，宵禁巡夜，寅时交更
            new NpcScheduleDef("zheng_wu", "npc.zheng_wu", NpcArchetype.Official, new[]
            {
                new ScheduleEntry(3, 10, "wuhou_post", "activity.on_duty"),
                new ScheduleEntry(10, 2, "ward_lanes", "activity.night_patrol"),
                new ScheduleEntry(2, 3, "wuhou_post", "activity.handover")
            }),

            // 桓夫子：晨汲水闲话，日课徒校书，入夜即眠
            new NpcScheduleDef("huan_fuzi", "npc.huan_fuzi", NpcArchetype.Commoner, new[]
            {
                new ScheduleEntry(3, 4, "well", "activity.drawing_water"),
                new ScheduleEntry(4, 6, "home_huan", "activity.teaching"),
                new ScheduleEntry(6, 7, "well", "activity.cooling"),
                new ScheduleEntry(7, 9, "home_huan", "activity.collating"),
                new ScheduleEntry(9, 3, "home_huan", "activity.sleeping")
            })
        };
    }
}
