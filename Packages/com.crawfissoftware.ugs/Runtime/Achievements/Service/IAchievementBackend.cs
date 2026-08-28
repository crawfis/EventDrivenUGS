using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Where achievement state lives, expressed as operations rather than as storage.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>Operation-level on purpose. A client-authoritative implementation reads and writes the
    /// player's saved data directly; a server-authoritative one calls a Cloud Code module that
    /// decides whether the unlock is legitimate. Those have nothing in common at the storage level
    /// and everything in common here.</para>
    /// <para>Every mutating call returns the authoritative record, which the service applies to its
    /// in-memory copy. That way a server that clamps or rejects a change corrects the UI, instead
    /// of the UI optimistically showing something the server never accepted.</para>
    /// </remarks>
    public interface IAchievementBackend
    {
        /// <summary>Definitions joined to this player's records.</summary>
        Task<IReadOnlyList<Achievement>> GetAchievementsAsync(string playerId);

        /// <summary>Unlock one achievement and return its resulting record.</summary>
        Task<AchievementRecordDto> UnlockAsync(string achievementId);

        /// <summary>Set absolute progress for one achievement and return its resulting record.</summary>
        Task<AchievementRecordDto> SetProgressAsync(string achievementId, int progressCount);

        /// <summary>Clear every record for the current player.</summary>
        Task ResetAllAsync();
    }
}
