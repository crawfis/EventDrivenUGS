namespace CrawfisSoftware.UGS.Economy
{
    /// <summary>
    /// An authoritative balance, and where it came from.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// The origin is carried because a subscriber can need to treat the two differently and cannot
    /// otherwise tell them apart. <c>CoinBasedAchievements</c> is the case in point: it takes its
    /// baseline from a read, and a credit arriving first - which is what happens when the launch
    /// read fails - must not be mistaken for one, or the threshold that credit just crossed is
    /// swallowed and never announced again.
    /// </remarks>
    public readonly struct CurrencyBalanceUpdate
    {
        public CurrencyBalanceUpdate(string currencyId, long balance, bool fromRead)
        {
            CurrencyId = currencyId;
            Balance = balance;
            FromRead = fromRead;
        }

        /// <summary>Which currency this balance is for.</summary>
        public string CurrencyId { get; }

        /// <summary>The balance the backing service reported.</summary>
        public long Balance { get; }

        /// <summary>True when this came from reading the stored balance; false after a credit or debit.</summary>
        public bool FromRead { get; }
    }
}
