using Lingyan.Core.Saves;

namespace Lingyan.Core.Social
{
    /// <summary>NpcState（领域）与 SaveNpcState（存档 DTO）互转。</summary>
    public static class NpcStateStore
    {
        /// <summary>从存档取出（无记录则给全新状态）。</summary>
        public static NpcState Load(SaveData save, string npcId)
        {
            var state = new NpcState();
            if (!save.NpcStates.TryGetValue(npcId, out SaveNpcState dto))
            {
                return state;
            }
            state.Met = dto.Met;
            state.LastGreetDay = dto.LastGreetDay;
            foreach (SaveAffinityEntry entry in dto.Ledger)
            {
                state.Ledger.Add(new AffinityEntry
                {
                    Delta = entry.Delta,
                    SourceKey = entry.SourceKey,
                    SourceParam = entry.SourceParam,
                    DateStamp = entry.DateStamp
                });
            }
            state.Flags.AddRange(dto.Flags);
            return state;
        }

        /// <summary>写回存档。</summary>
        public static void Store(SaveData save, string npcId, NpcState state)
        {
            var dto = new SaveNpcState
            {
                Met = state.Met,
                LastGreetDay = state.LastGreetDay
            };
            foreach (AffinityEntry entry in state.Ledger)
            {
                dto.Ledger.Add(new SaveAffinityEntry
                {
                    Delta = entry.Delta,
                    SourceKey = entry.SourceKey,
                    SourceParam = entry.SourceParam,
                    DateStamp = entry.DateStamp
                });
            }
            dto.Flags.AddRange(state.Flags);
            save.NpcStates[npcId] = dto;
        }
    }
}
