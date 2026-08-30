using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.Services.CloudCode;

namespace CrawfisSoftware.UGS.Economy
{
    /// <summary>
    /// Server-side currency: the client asks a Cloud Code module to move the balance and is told
    /// what the balance became.
    ///    Dependencies: Unity.Services.CloudCode
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>What this buys over <see cref="EconomyCurrencyBackend"/> is that the WRITE happens
    /// with the module's service token rather than the player's, so it keeps working under an access
    /// policy that denies players direct writes to Economy. It costs a deployed module and a round
    /// trip.</para>
    /// <para><b>It does not, by itself, make the amount trustworthy.</b> This backend sends a number
    /// the client computed. A module can only validate it against state the server holds
    /// independently, which means an endpoint built around the game's own rules - see the reference
    /// module's notes. Treating "goes through Cloud Code" as "cannot be forged" is the mistake this
    /// paragraph exists to prevent.</para>
    /// <para>Calls through <c>CallModuleEndpointAsync</c> with request and response types declared
    /// in this package rather than through generated bindings, which are emitted into the
    /// consumer's Assets folder where a package assembly cannot reach them.</para>
    /// </remarks>
    public sealed class CloudCodeCurrencyBackend : ICurrencyBackend
    {
        private readonly CloudCodeCurrencyEndpoints _endpoints;

        /// <summary>The module and endpoint names this backend calls.</summary>
        public CloudCodeCurrencyEndpoints Endpoints => _endpoints;

        public CloudCodeCurrencyBackend(CloudCodeCurrencyEndpoints endpoints)
        {
            if (!endpoints.IsComplete)
            {
                throw new CurrencyBackendException(
                    "CloudCodeCurrencyEndpoints is incomplete - most likely ModuleName, which has no default " +
                    "because this package ships no deployed Cloud Code module. Set PlayerCurrencyManager." +
                    "Instance.CloudCodeEndpoints to the module you deployed before turning UseTrustedClient on. " +
                    "The function names in CloudCodeCurrencyEndpoints.Default match the reference module under " +
                    "CloudCode~/CurrencyModule.");
            }
            _endpoints = endpoints;
        }

        public CloudCodeCurrencyBackend() : this(CloudCodeCurrencyEndpoints.Default)
        {
        }

        /// <inheritdoc />
        public async Task<long> GetBalanceAsync(string currencyId)
        {
            RequireCurrencyId(currencyId);
            var dto = await CallAsync(
                _endpoints.GetCurrencyBalance,
                new Dictionary<string, object> { { "currencyId", currencyId } });
            return dto?.Balance ?? 0L;
        }

        /// <inheritdoc />
        public async Task<long> AddAsync(string currencyId, int amount)
        {
            RequireCurrencyId(currencyId);
            var dto = await CallAsync(
                _endpoints.AddCurrency,
                new Dictionary<string, object>
                {
                    { "currencyId", currencyId },
                    { "amount", amount },
                });
            return dto?.Balance ?? 0L;
        }

        private async Task<CurrencyBalanceDto> CallAsync(string endpoint, Dictionary<string, object> arguments)
        {
            try
            {
                return await CloudCodeService.Instance.CallModuleEndpointAsync<CurrencyBalanceDto>(
                    _endpoints.ModuleName, endpoint, arguments);
            }
            catch (Exception e)
            {
                throw Wrap(endpoint, e);
            }
        }

        private static void RequireCurrencyId(string currencyId)
        {
            if (string.IsNullOrEmpty(currencyId))
                throw new CurrencyBackendException("A currency id is required.");
        }

        private CurrencyBackendException Wrap(string endpoint, Exception inner) =>
            new CurrencyBackendException(
                $"Cloud Code call '{_endpoints.ModuleName}/{endpoint}' failed. " +
                "Check that the module is deployed and that the endpoint name matches.", inner);
    }
}
