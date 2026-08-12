using System;
using System.Collections.Generic;

namespace Lingyan.Core.Reputation
{
    /// <summary>三轨名誉。三者可互相冲突，是抉择机制的核心。</summary>
    public enum ReputationTrack
    {
        /// <summary>官声：朝廷评价。影响升迁、上级对话。</summary>
        GuanSheng = 0,

        /// <summary>民望：百姓评价。影响物价、线索获取、市井 NPC 态度。</summary>
        MinWang = 1,

        /// <summary>江湖名望：影响侠客、赌坊、黑市。</summary>
        JiangHu = 2
    }

    /// <summary>名誉变动的一条账目（来源 + 时间可回溯，供悬停面板复用）。</summary>
    public sealed class ReputationLedgerEntry
    {
        public ReputationTrack Track { get; set; }
        public int Delta { get; set; }

        /// <summary>来源本地化键，UI 取词展示。</summary>
        public string SourceKey { get; set; }

        /// <summary>发生时的游戏日期（存档序列化字符串，如 chuigong:4:3:17:5）。</summary>
        public string DateStamp { get; set; }
    }

    public sealed class ReputationState
    {
        public const int Min = 0;
        public const int Max = 100;

        public int GuanSheng { get; private set; }
        public int MinWang { get; private set; }
        public int JiangHu { get; private set; }

        private readonly List<ReputationLedgerEntry> _ledger = new List<ReputationLedgerEntry>();

        public IReadOnlyList<ReputationLedgerEntry> Ledger { get { return _ledger; } }

        public ReputationState() { }

        public ReputationState(int guanSheng, int minWang, int jiangHu)
        {
            GuanSheng = Clamp(guanSheng);
            MinWang = Clamp(minWang);
            JiangHu = Clamp(jiangHu);
        }

        public int Get(ReputationTrack track)
        {
            switch (track)
            {
                case ReputationTrack.GuanSheng: return GuanSheng;
                case ReputationTrack.MinWang: return MinWang;
                case ReputationTrack.JiangHu: return JiangHu;
                default: throw new ArgumentOutOfRangeException(nameof(track));
            }
        }

        /// <summary>应用一次变动并记账。返回实际生效增量（0–100 夹取后）。</summary>
        public int Apply(ReputationTrack track, int delta, string sourceKey, string dateStamp)
        {
            int before = Get(track);
            int after = Clamp(before + delta);
            switch (track)
            {
                case ReputationTrack.GuanSheng: GuanSheng = after; break;
                case ReputationTrack.MinWang: MinWang = after; break;
                case ReputationTrack.JiangHu: JiangHu = after; break;
            }
            _ledger.Add(new ReputationLedgerEntry
            {
                Track = track,
                Delta = after - before,
                SourceKey = sourceKey,
                DateStamp = dateStamp
            });
            return after - before;
        }

        public void RestoreLedger(IEnumerable<ReputationLedgerEntry> entries)
        {
            _ledger.Clear();
            if (entries != null) { _ledger.AddRange(entries); }
        }

        private static int Clamp(int v)
        {
            return v < Min ? Min : v > Max ? Max : v;
        }
    }

    /// <summary>NPC 身份群体：决定三轨名誉在其眼中的权重。</summary>
    public enum NpcArchetype
    {
        Official = 0,
        Commoner = 1,
        Jianghu = 2,
        Palace = 3,
        Merchant = 4
    }

    public static class ReputationWeights
    {
        /// <summary>返回 (官声, 民望, 江湖) 权重，和为 1。</summary>
        public static (double guan, double min, double jianghu) For(NpcArchetype archetype)
        {
            switch (archetype)
            {
                case NpcArchetype.Official: return (0.70, 0.20, 0.10);
                case NpcArchetype.Commoner: return (0.15, 0.70, 0.15);
                case NpcArchetype.Jianghu: return (0.10, 0.20, 0.70);
                case NpcArchetype.Palace: return (0.80, 0.10, 0.10);
                case NpcArchetype.Merchant: return (0.30, 0.45, 0.25);
                default: return (1.0 / 3, 1.0 / 3, 1.0 / 3);
            }
        }

        /// <summary>同一份名誉在不同人眼里的加权观感，0–100。</summary>
        public static int WeightedOpinion(ReputationState state, NpcArchetype archetype)
        {
            var (g, m, j) = For(archetype);
            double v = state.GuanSheng * g + state.MinWang * m + state.JiangHu * j;
            int rounded = (int)Math.Round(v, MidpointRounding.AwayFromZero);
            return rounded < 0 ? 0 : rounded > 100 ? 100 : rounded;
        }
    }
}
