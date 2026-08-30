using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.Services.Economy;
using Unity.Services.Economy.Model;

namespace CrawfisSoftware.UGS.Economy
{
    /// <summary>
    /// Client-authoritative currency: the player's device credits and debits its own balance
    /// through the Economy service.
    ///    Dependencies: Unity.Services.Economy
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>Appropriate for single-player progression, where a determined player cheating
    /// themselves costs nobody else anything. Use <see cref="CloudCodeCurrencyBackend"/> wherever
    /// the balance buys something a competitor can see.</para>
    /// <para>The credit itself is safe from the usual concurrency hazard without any locking on our
    /// side: Increment and Decrement are server-side arithmetic on the stored value, not a
    /// read-modify-write of it, so two devices crediting at once both land. That is why this
    /// backend never reads before it writes.</para>
    /// <para><b>A missing balance row is not an error.</b> A player who has never held the currency
    /// has no row, and the honest answer for them is zero, not a failure. But zero is also exactly
    /// what a mistyped currency id returns forever, silently - so before reporting zero this asks
    /// the Economy configuration whether the currency exists at all, and fails loudly if it does
    /// not. That extra call happens only for a player with no row, which is once per new player.</para>
    /// </remarks>
    public sealed class EconomyCurrencyBackend : ICurrencyBackend
    {
        /// <summary>
        /// Balances fetched per page. Unity's default is 20 and a game has a handful of currencies,
        /// so this exists only so the paging loop below has a stated size rather than an implied one.
        /// </summary>
        private const int ItemsPerFetch = 20;

        /// <inheritdoc />
        public async Task<long> GetBalanceAsync(string currencyId)
        {
            RequireCurrencyId(currencyId);

            try
            {
                var page = await EconomyService.Instance.PlayerBalances.GetBalancesAsync(
                    new GetBalancesOptions { ItemsPerFetch = ItemsPerFetch });

                while (page != null)
                {
                    List<PlayerBalance> balances = page.Balances;
                    if (balances != null)
                    {
                        for (int i = 0; i < balances.Count; i++)
                        {
                            if (balances[i] != null && balances[i].CurrencyId == currencyId)
                                return balances[i].Balance;
                        }
                    }

                    if (!page.HasNext) break;
                    page = await page.GetNextAsync(ItemsPerFetch);
                }
            }
            catch (Exception e)
            {
                throw Wrap($"read the '{currencyId}' balance", e);
            }

            // No row. Either a new player, or an id this project does not define - and those two
            // must not look the same to the caller.
            await RequireCurrencyDefinedAsync(currencyId);
            return 0L;
        }

        /// <inheritdoc />
        public async Task<long> AddAsync(string currencyId, int amount)
        {
            RequireCurrencyId(currencyId);

            // Negating int.MinValue overflows, and the negation below is how a debit is expressed.
            if (amount == int.MinValue)
                throw new CurrencyBackendException($"An amount of {int.MinValue} cannot be applied.");

            if (amount == 0) return await GetBalanceAsync(currencyId);

            try
            {
                PlayerBalance result = amount > 0
                    ? await EconomyService.Instance.PlayerBalances.IncrementBalanceAsync(currencyId, amount)
                    : await EconomyService.Instance.PlayerBalances.DecrementBalanceAsync(currencyId, -amount);

                return result?.Balance ?? 0L;
            }
            catch (Exception e)
            {
                // The amount is deliberately NOT interpolated here. This message is one of the
                // things IsAccessDenied can end up scanning, and a credit of 403 coins reading as
                // an access denial is exactly the kind of silent misdiagnosis this class is trying
                // to prevent elsewhere. The amount is in the caller's own log line.
                string what = amount > 0 ? "credit" : "debit";
                throw Wrap($"{what} '{currencyId}'", e);
            }
        }

        /// <summary>
        /// Throw a named failure when the project does not define this currency. Called only when a
        /// player has no balance row, to tell "new player" apart from "wrong id".
        /// </summary>
        private static async Task RequireCurrencyDefinedAsync(string currencyId)
        {
            try
            {
                // The whole configuration has to be pulled into the SDK cache before any lookup:
                // the single-id fetch that would have done this in one call is deprecated.
                await EconomyService.Instance.Configuration.SyncConfigurationAsync();
            }
            catch (Exception e)
            {
                // A sync that could not complete says nothing about whether the currency exists,
                // so it must not be reported as though the id were wrong.
                throw Wrap($"check whether '{currencyId}' is a currency this project defines", e);
            }

            if (EconomyService.Instance.Configuration.GetCurrency(currencyId) == null)
                throw NotFound(currencyId, null);
        }

        private static void RequireCurrencyId(string currencyId)
        {
            if (string.IsNullOrEmpty(currencyId))
                throw new CurrencyBackendException("A currency id is required.");
        }

        private static CurrencyBackendException NotFound(string currencyId, Exception inner) =>
            new CurrencyBackendException(
                $"The Economy configuration for this project defines no currency '{currencyId}'. " +
                "Create it in the Unity Dashboard under Economy > Currencies, or point " +
                $"{nameof(PlayerCurrencyManager)}.{nameof(PlayerCurrencyManager.CurrencyId)} at the id you did create.",
                inner)
            { IsCurrencyNotFound = true };

        /// <summary>
        /// Wrap an SDK failure, carrying over what the SDK itself said the cause was.
        /// </summary>
        /// <remarks>
        /// The reason is read from <see cref="EconomyException.Reason"/> rather than inferred from
        /// message text. A status code is what the service actually returned; a substring is a
        /// guess that caller data can corrupt.
        /// </remarks>
        private static CurrencyBackendException Wrap(string what, Exception inner)
        {
            var wrapped = new CurrencyBackendException($"Economy could not {what}.", inner);

            if (inner is EconomyException economyException)
            {
                switch (economyException.Reason)
                {
                    case EconomyExceptionReason.Forbidden:
                    case EconomyExceptionReason.Unauthorized:
                        wrapped.IsAccessDenied = true;
                        break;
                    case EconomyExceptionReason.EntityNotFound:
                        wrapped.IsCurrencyNotFound = true;
                        break;
                }
            }

            return wrapped;
        }
    }
}
