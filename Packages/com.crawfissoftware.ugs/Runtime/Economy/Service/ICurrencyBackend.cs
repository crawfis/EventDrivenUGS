using System.Threading.Tasks;

namespace CrawfisSoftware.UGS.Economy
{
    /// <summary>
    /// Where a player's lifetime soft-currency balance is read and changed.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>Two members, because a balance only ever needs reading and moving. Deliberately no
    /// "set" - an absolute write is the one operation that cannot be made safe against a second
    /// device, and neither backend can offer it honestly.</para>
    /// <para>Both members return the resulting authoritative balance rather than void, so the
    /// caller's cached number comes from the service that owns it instead of from local
    /// arithmetic. That is what keeps two devices from drifting apart.</para>
    /// </remarks>
    public interface ICurrencyBackend
    {
        /// <summary>This player's current balance of one currency. Zero when they have none yet.</summary>
        Task<long> GetBalanceAsync(string currencyId);

        /// <summary>
        /// Move the balance by <paramref name="amount"/> - negative to spend - and return the
        /// resulting balance.
        /// </summary>
        Task<long> AddAsync(string currencyId, int amount);
    }
}
