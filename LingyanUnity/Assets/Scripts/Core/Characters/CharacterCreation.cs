using System;

namespace Lingyan.Core.Characters
{
    public enum AllocationError
    {
        None = 0,

        /// <summary>该主角为固定预设，只读展示。</summary>
        NotAllocatable = 1,

        /// <summary>自由点数已用尽。</summary>
        PoolExhausted = 2,

        /// <summary>已达单项上限。</summary>
        AtMax = 3,

        /// <summary>已达单项下限。</summary>
        AtMin = 4
    }

    /// <summary>角色创建草稿。仅白身（ProtagonistId.BaiShen）允许改动四维。</summary>
    public sealed class CharacterDraft
    {
        public ProtagonistDef Def { get; }
        public string Name { get; set; }
        public AttributeSet Attributes { get; }
        public EntryPath? EntryPath { get; set; }

        internal CharacterDraft(ProtagonistDef def)
        {
            Def = def;
            Name = def.DefaultNameZh;
            Attributes = def.Preset.Clone();
            if (def.Id == ProtagonistId.BaiShen)
            {
                EntryPath = Characters.EntryPath.KejuMingJing;
            }
        }
    }

    public static class CharacterCreationRules
    {
        /// <summary>白身基础值（每维）。</summary>
        public const int BasePerAttribute = 4;

        /// <summary>白身自由分配点数池。</summary>
        public const int FreePool = 12;

        /// <summary>分配时单项上限。</summary>
        public const int MaxPerAttribute = 12;

        /// <summary>分配时单项下限（不得低于基础值）。</summary>
        public const int MinPerAttribute = BasePerAttribute;

        public static CharacterDraft NewDraft(ProtagonistId id)
        {
            return new CharacterDraft(ProtagonistCatalog.Get(id));
        }

        public static int RemainingPool(CharacterDraft draft)
        {
            if (!draft.Def.FreeAllocation) { return 0; }
            int spent = draft.Attributes.Total - BasePerAttribute * 4;
            return FreePool - spent;
        }

        public static AllocationError TryIncrease(CharacterDraft draft, AttributeId attr)
        {
            if (!draft.Def.FreeAllocation) { return AllocationError.NotAllocatable; }
            if (RemainingPool(draft) <= 0) { return AllocationError.PoolExhausted; }
            if (draft.Attributes.Get(attr) >= MaxPerAttribute) { return AllocationError.AtMax; }
            draft.Attributes.Set(attr, draft.Attributes.Get(attr) + 1);
            return AllocationError.None;
        }

        public static AllocationError TryDecrease(CharacterDraft draft, AttributeId attr)
        {
            if (!draft.Def.FreeAllocation) { return AllocationError.NotAllocatable; }
            if (draft.Attributes.Get(attr) <= MinPerAttribute) { return AllocationError.AtMin; }
            draft.Attributes.Set(attr, draft.Attributes.Get(attr) - 1);
            return AllocationError.None;
        }

        /// <summary>定稿前校验；返回本地化错误键，null 表示通过。</summary>
        public static string ValidateFinal(CharacterDraft draft)
        {
            if (string.IsNullOrWhiteSpace(draft.Name))
            {
                return "creation.error.name_empty";
            }
            if (draft.Def.Id == ProtagonistId.BaiShen)
            {
                if (draft.EntryPath == null) { return "creation.error.entry_path_missing"; }
                if (RemainingPool(draft) != 0) { return "creation.error.points_unspent"; }
            }
            return null;
        }
    }
}
