using System.Collections.Generic;
using Lingyan.Core.Calendar;
using Lingyan.Core.Reputation;
using Lingyan.Core.Saves;
using Lingyan.Core.Social;

namespace Lingyan.Core.Dialogue
{
    /// <summary>
    /// 对话运行器：条件过滤选项、选择后效果统一落档（OutcomeApplier 口径）、
    /// 剧情旗标写入 SaveData.StoryFlags——分支后果持久化，后续剧情读得到。
    /// </summary>
    public sealed class DialogueRunner
    {
        private readonly DialogueTree _tree;
        private readonly SaveData _save;
        private readonly NpcProfile _profile;
        private readonly NpcArchetype _archetype;

        public DialogueLine Current { get; private set; }

        public bool Finished { get { return Current == null; } }

        public DialogueRunner(
            DialogueTree tree, SaveData save, NpcProfile profile, NpcArchetype archetype)
        {
            _tree = tree;
            _save = save;
            _profile = profile;
            _archetype = archetype;
            Current = tree.Line(tree.EntryId);
        }

        /// <summary>当前可见选项（条件全部满足）。</summary>
        public List<DialogueChoice> AvailableChoices()
        {
            var list = new List<DialogueChoice>();
            if (Current == null) { return list; }
            foreach (DialogueChoice choice in Current.Choices)
            {
                if (Passes(choice)) { list.Add(choice); }
            }
            return list;
        }

        public bool Passes(DialogueChoice choice)
        {
            foreach (DialogueCondition condition in choice.Conditions)
            {
                if (!Evaluate(condition)) { return false; }
            }
            return true;
        }

        private bool Evaluate(DialogueCondition condition)
        {
            switch (condition.Kind)
            {
                case ConditionKind.MinAffinity:
                    return AffinityTotal() >= condition.Value;
                case ConditionKind.MinGuanSheng:
                    return _save.Reputation.GuanSheng >= condition.Value;
                case ConditionKind.MinMinWang:
                    return _save.Reputation.MinWang >= condition.Value;
                case ConditionKind.MinJiangHu:
                    return _save.Reputation.JiangHu >= condition.Value;
                case ConditionKind.FlagSet:
                    return _save.StoryFlags.TryGetValue(condition.Flag, out bool set) && set;
                case ConditionKind.FlagNotSet:
                    return !_save.StoryFlags.TryGetValue(condition.Flag, out bool set2) || !set2;
                case ConditionKind.HourBetween:
                {
                    int h = _save.Date.HourIndex;
                    int from = condition.Value;
                    int to = condition.Value2;
                    return from < to ? h >= from && h < to : h >= from || h < to;
                }
                case ConditionKind.MinMoneyWen:
                    return _save.MoneyWen >= condition.Value;
                default:
                    return false;
            }
        }

        private int AffinityTotal()
        {
            NpcState state = NpcStateStore.Load(_save, _profile.NpcId);
            var reputation = new ReputationState(
                _save.Reputation.GuanSheng, _save.Reputation.MinWang, _save.Reputation.JiangHu);
            Officials.RobeColor robe = Officials.RobeColors.FromGrade(
                Officials.SanGuanTable.Get(_save.Offices.SanGuanId)?.Grade);
            return AffinityService.Total(_profile, state, reputation, _archetype, robe, null);
        }

        /// <summary>选择：效果落档 → 跳转（goto 为空则结束）。</summary>
        public void Choose(DialogueChoice choice, TangDate date)
        {
            foreach (DialogueEffect effect in choice.Effects)
            {
                Apply(effect, date);
            }
            Current = choice.GotoId == null ? null : _tree.Line(choice.GotoId);
        }

        private void Apply(DialogueEffect effect, TangDate date)
        {
            string source = effect.SourceKey ?? "affinity.src.conversation";
            switch (effect.Kind)
            {
                case EffectKind.Affinity:
                {
                    NpcState state = NpcStateStore.Load(_save, _profile.NpcId);
                    state.Add(effect.Value, source, date.ToStamp());
                    NpcStateStore.Store(_save, _profile.NpcId, state);
                    break;
                }
                case EffectKind.GuanSheng:
                    OutcomeApplier.ApplyReputation(
                        _save, ReputationTrack.GuanSheng, effect.Value, source, date);
                    break;
                case EffectKind.MinWang:
                    OutcomeApplier.ApplyReputation(
                        _save, ReputationTrack.MinWang, effect.Value, source, date);
                    break;
                case EffectKind.JiangHu:
                    OutcomeApplier.ApplyReputation(
                        _save, ReputationTrack.JiangHu, effect.Value, source, date);
                    break;
                case EffectKind.SetFlag:
                    _save.StoryFlags[effect.Flag] = true;
                    break;
                case EffectKind.MoneyWen:
                    OutcomeApplier.ApplyMoney(_save, effect.Value);
                    break;
                case EffectKind.Wanted:
                    OutcomeApplier.ApplyWanted(_save, effect.Value);
                    break;
            }
        }
    }

    /// <summary>对话树完整性校验（数据层红线：坏引用在测试期就红，不进游戏）。</summary>
    public static class DialogueValidator
    {
        public static List<string> Check(
            DialogueTree tree, Localization.LocalizationCatalog catalog)
        {
            var violations = new List<string>();
            if (tree.Line(tree.EntryId) == null)
            {
                violations.Add(tree.Id + ": 入口节点不存在: " + tree.EntryId);
            }
            var seen = new HashSet<string>();
            foreach (DialogueLine line in tree.Lines)
            {
                if (!seen.Add(line.Id))
                {
                    violations.Add(tree.Id + ": 节点 id 重复: " + line.Id);
                }
                if (!catalog.Has(line.TextKey))
                {
                    violations.Add(tree.Id + "/" + line.Id + ": 台词键缺失: " + line.TextKey);
                }
                foreach (DialogueChoice choice in line.Choices)
                {
                    if (!catalog.Has(choice.TextKey))
                    {
                        violations.Add(tree.Id + "/" + line.Id + ": 选项键缺失: " + choice.TextKey);
                    }
                    if (choice.GotoId != null && tree.Line(choice.GotoId) == null)
                    {
                        violations.Add(tree.Id + "/" + line.Id + ": goto 悬空: " + choice.GotoId);
                    }
                    foreach (DialogueCondition condition in choice.Conditions)
                    {
                        bool needsFlag = condition.Kind == ConditionKind.FlagSet
                            || condition.Kind == ConditionKind.FlagNotSet;
                        if (needsFlag && string.IsNullOrEmpty(condition.Flag))
                        {
                            violations.Add(tree.Id + "/" + line.Id + ": 旗标条件缺 flag 字段");
                        }
                    }
                    foreach (DialogueEffect effect in choice.Effects)
                    {
                        if (effect.Kind == EffectKind.SetFlag && string.IsNullOrEmpty(effect.Flag))
                        {
                            violations.Add(tree.Id + "/" + line.Id + ": SetFlag 缺 flag 字段");
                        }
                    }
                }
            }
            return violations;
        }
    }
}
