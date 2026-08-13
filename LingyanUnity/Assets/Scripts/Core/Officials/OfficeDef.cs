using System;

namespace Lingyan.Core.Officials
{
    /// <summary>四轨官制的轨道。</summary>
    public enum OfficeTrack
    {
        /// <summary>职事官：实际职务，决定权限。</summary>
        ZhiShi = 0,

        /// <summary>散官：品阶身份，决定俸禄与服色。</summary>
        SanGuan = 1,

        /// <summary>勋官：军功十二转。</summary>
        XunGuan = 2,

        /// <summary>爵：封爵，影响结亲门第。</summary>
        Jue = 3
    }

    /// <summary>职业线。</summary>
    public enum CareerLine
    {
        None = 0,

        /// <summary>文官·法司线。</summary>
        CivilJudicial = 1,

        /// <summary>武官·折冲府线。</summary>
        Military = 2,

        /// <summary>内廷·女官线（武周特有）。</summary>
        Palace = 3,

        /// <summary>商路·情报线。</summary>
        Trade = 4,

        /// <summary>白身：入仕途径二选一后再定线。</summary>
        Undecided = 5
    }

    /// <summary>
    /// 一个官职/官号的静态定义。英文名不直接存放在此，
    /// 统一经 <c>GlossaryId</c> 从术语表取得，保证全局一致（Hucker 1985 锁定）。
    /// </summary>
    public sealed class OfficeDef
    {
        public string Id { get; }

        public string Zh { get; }

        /// <summary>术语表词条 id，英文名的唯一来源。</summary>
        public string GlossaryId { get; }

        /// <summary>品阶。差遣（同平章事、行军总管）无本品，为 null。</summary>
        public RankGrade? Grade { get; }

        public OfficeTrack Track { get; }

        public CareerLine Line { get; }

        /// <summary>在所属迁转序列中的位置（0 起）；不在序列中为 -1。</summary>
        public int LadderIndex { get; }

        /// <summary>是否差遣/使职（无本品，以他官充任）。</summary>
        public bool IsCommission { get; }

        public OfficeDef(
            string id,
            string zh,
            string glossaryId,
            RankGrade? grade,
            OfficeTrack track,
            CareerLine line,
            int ladderIndex,
            bool isCommission = false)
        {
            if (string.IsNullOrEmpty(id)) { throw new ArgumentException("id 不可为空", nameof(id)); }
            if (string.IsNullOrEmpty(zh)) { throw new ArgumentException("zh 不可为空", nameof(zh)); }
            if (string.IsNullOrEmpty(glossaryId)) { throw new ArgumentException("glossaryId 不可为空", nameof(glossaryId)); }
            Id = id;
            Zh = zh;
            GlossaryId = glossaryId;
            Grade = grade;
            Track = track;
            Line = line;
            LadderIndex = ladderIndex;
            IsCommission = isCommission;
        }
    }
}
