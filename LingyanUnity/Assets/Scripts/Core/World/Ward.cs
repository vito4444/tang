using System;
using System.Collections.Generic;
using System.Linq;
using Lingyan.Core.Calendar;

namespace Lingyan.Core.World
{
    /// <summary>坊内一处地点。</summary>
    public sealed class PlaceDef
    {
        public string Id { get; }

        /// <summary>地点名本地化键。</summary>
        public string NameKey { get; }

        /// <summary>是否坊门（宵禁启闭的对象）。</summary>
        public bool IsGate { get; }

        public PlaceDef(string id, string nameKey, bool isGate = false)
        {
            if (string.IsNullOrEmpty(id)) { throw new ArgumentException("id 不可为空", nameof(id)); }
            Id = id;
            NameKey = nameKey;
            IsGate = isGate;
        }
    }

    /// <summary>
    /// 坊：城市的基本单元，坊墙坊门按时启闭（规格第十节）。
    /// 本类是逻辑模型；三维坊场景在 Unity 视觉层实装时引用同一份数据。
    /// </summary>
    public sealed class WardDef
    {
        public string Id { get; }
        public string NameKey { get; }

        private readonly Dictionary<string, PlaceDef> _places;

        public IReadOnlyCollection<PlaceDef> Places { get { return _places.Values; } }

        public WardDef(string id, string nameKey, IEnumerable<PlaceDef> places)
        {
            Id = id;
            NameKey = nameKey;
            _places = places.ToDictionary(p => p.Id);
        }

        public PlaceDef Place(string id)
        {
            return id != null && _places.TryGetValue(id, out var place) ? place : null;
        }

        /// <summary>
        /// 坊门此刻是否开启。唐制暮鼓闭门、晓鼓启门；
        /// 本作与宵禁时段取同一张表（戌—寅闭，卯—酉开），单一事实来源。
        /// </summary>
        public static bool GatesOpenAt(int hourIndex)
        {
            return !ShiChenTable.All[hourIndex].CurfewHour;
        }

        /// <summary>此刻是否宵禁（武侯巡查、犯夜受笞的时段）。</summary>
        public static bool CurfewAt(int hourIndex)
        {
            return ShiChenTable.All[hourIndex].CurfewHour;
        }
    }
}
