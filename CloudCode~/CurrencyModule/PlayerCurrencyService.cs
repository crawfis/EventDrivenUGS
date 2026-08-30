using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.Economy.Model;

namespace CurrencyModule;

/// <summary>
/// Server-side currency operations: read one player's balance, and move it.
///
/// Harvested from the currency half of TempleRunUGSCloud's PlayerEconomyService. The inventory,
/// store-pack, loot-box and infinite-heart machinery around it was Match3 sample content and is
/// deliberately not carried over.
///
/// Pairs with CrawfisSoftware.UGS.Economy.CloudCodeCurrencyBackend on the client. The two function
/// names below are the wire contract, and match CloudCodeCurrencyEndpoints.Default.
///
/// WHAT THIS DOES AND DOES NOT GUARANTEE
/// -------------------------------------
/// It moves the WRITE off the player's device and performs it with the service token, so it keeps
/// working under an access policy that denies players direct writes to urn:ugs:economy:*.
///
/// It does NOT validate that the player earned the coins. The amount is supplied by the caller,
/// and this module holds no run state to check it against, so a modified client can call
/// AddCurrency with any value up to MaxAmountPerCall. Calling that "server-authoritative" would be
/// a lie. Making it true means giving the server its own knowledge of the run - an endpoint that
/// takes a run token and recomputes the credit, the way AdRewardsService validates an ad reward -
/// which is a game-design decision, not a porting one.
/// </summary>
public class PlayerCurrencyService
{
    /// <summary>
    /// Ceiling on a single call. It cannot tell a legitimate credit from a forged one - nothing
    /// here can - but it bounds the damage a forged one does to something a moderator can undo,
    /// instead of letting one call mint an unbounded balance.
    /// </summary>
    private const int MaxAmountPerCall = 10_000;

    private readonly IGameApiClient _gameApiClient;
    private readonly ILogger<PlayerCurrencyService> _logger;

    public PlayerCurrencyService(ILogger<PlayerCurrencyService> logger, IGameApiClient gameApiClient)
    {
        _logger = logger;
        _gameApiClient = gameApiClient;
    }

    /// <summary>This player's current balance of one currency. Zero when they hold none yet.</summary>
    [CloudCodeFunction("GetCurrencyBalance")]
    public async Task<CurrencyBalance> GetCurrencyBalance(IExecutionContext context, string currencyId)
    {
        RequireArguments(context, currencyId);

        var (_, balance) = await FindBalanceAsync(context, currencyId);
        return new CurrencyBalance { CurrencyId = currencyId, Balance = balance };
    }

    /// <summary>
    /// Move this player's balance by <paramref name="amount"/> - negative to spend - and return the
    /// resulting balance.
    /// </summary>
    [CloudCodeFunction("AddCurrency")]
    public async Task<CurrencyBalance> AddCurrency(IExecutionContext context, string currencyId, int amount)
    {
        RequireArguments(context, currencyId);

        if (amount == 0)
        {
            return await GetCurrencyBalance(context, currencyId);
        }
        if (Math.Abs((long)amount) > MaxAmountPerCall)
        {
            throw new InvalidOperationException(
                $"Amount {amount} exceeds the per-call limit of {MaxAmountPerCall}.");
        }

        try
        {
            // No read, and no write lock. Increment and Decrement are arithmetic applied by the
            // service to the stored value, so they are already safe against a concurrent writer.
            // Reading a lock first would convert a commutative operation into an optimistic one
            // that FAILS on concurrency instead of composing with it - and cost a round trip to do
            // it. The service the harvest came from read the lock, and also threw when the player
            // had no balance row yet, which is every player before their first coin.
            var request = new CurrencyModifyBalanceRequest(
                currencyId: currencyId,
                amount: Math.Abs((long)amount),
                writeLock: null);

            // The SERVICE token, not the player's access token. With the player's own token the
            // request carries the player as its principal, so a project access policy denying
            // Player writes to urn:ugs:economy:* would deny this too - and the only reason to route
            // a write through a module under such a policy is to escape exactly that.
            var response = amount > 0
                ? await _gameApiClient.EconomyCurrencies.IncrementPlayerCurrencyBalanceAsync(
                    context, context.ServiceToken, context.ProjectId, context.PlayerId!, currencyId, request)
                : await _gameApiClient.EconomyCurrencies.DecrementPlayerCurrencyBalanceAsync(
                    context, context.ServiceToken, context.ProjectId, context.PlayerId!, currencyId, request);

            long newBalance = response.Data.Balance;
            _logger.LogInformation(
                "Currency {CurrencyId} moved by {Amount} for player {PlayerId}. New balance: {NewBalance}",
                currencyId, amount, context.PlayerId, newBalance);

            return new CurrencyBalance { CurrencyId = currencyId, Balance = newBalance };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to move currency {CurrencyId} by {Amount} for player {PlayerId}",
                currencyId, amount, context.PlayerId);

            // Rethrown with the original as inner rather than flattened into a new Exception. The
            // client tells an access-policy denial from an ordinary failure by looking for a 403 in
            // the message chain, and flattening throws that away.
            throw new InvalidOperationException($"Failed to move currency '{currencyId}'.", e);
        }
    }

    /// <summary>
    /// Look up one balance. Returns whether a row existed as well as its value, because a player
    /// who has never held the currency has no row and their honest balance is zero.
    /// </summary>
    /// <remarks>
    /// The SDK's balance type is deliberately never named here - <c>var</c> throughout, as the
    /// service this was harvested from also did. Naming it would pin this file to one version of a
    /// server SDK that is not on disk to check against.
    /// </remarks>
    private async Task<(bool Found, long Balance)> FindBalanceAsync(IExecutionContext context, string currencyId)
    {
        var balances = await _gameApiClient.EconomyCurrencies.GetPlayerCurrenciesAsync(
            context, context.ServiceToken, context.ProjectId, context.PlayerId!);

        var results = balances?.Data?.Results;
        if (results == null) return (false, 0L);

        foreach (var balance in results)
        {
            if (balance.CurrencyId == currencyId) return (true, balance.Balance);
        }
        return (false, 0L);
    }

    private static void RequireArguments(IExecutionContext context, string currencyId)
    {
        if (context.PlayerId == null)
        {
            throw new InvalidOperationException("PlayerId cannot be null.");
        }
        if (string.IsNullOrEmpty(currencyId))
        {
            throw new InvalidOperationException("A currency id is required.");
        }
    }
}

/// <summary>
/// The response shape both endpoints return.
///
/// Wire contract: these two property names are what
/// CrawfisSoftware.UGS.Economy.CurrencyBalanceDto deserializes. Renaming either yields a default
/// on the client with no error anywhere.
/// </summary>
public class CurrencyBalance
{
    public string CurrencyId { get; set; } = string.Empty;
    public long Balance { get; set; }
}
