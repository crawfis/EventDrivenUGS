using System;

namespace CrawfisSoftware.UGS.Economy
{
    /// <summary>
    /// Names the Cloud Code module and the two endpoints the trusted currency backend calls.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// Data rather than constants, for the same reason as
    /// <see cref="CrawfisSoftware.UGS.Achievements.CloudCodeAchievementEndpoints"/>: a package must
    /// not hard-code a module the consumer may have named differently, merged into a larger module,
    /// or not deployed at all.
    /// </remarks>
    [Serializable]
    public struct CloudCodeCurrencyEndpoints
    {
        public string ModuleName;
        public string GetCurrencyBalance;
        public string AddCurrency;

        /// <summary>
        /// The conventional function names, with <b>no module name</b>.
        /// </summary>
        /// <remarks>
        /// This package ships no deployed Cloud Code module, so there is no honest default for
        /// <see cref="ModuleName"/> and it is deliberately left null: <see cref="IsComplete"/> then
        /// fails and <see cref="CloudCodeCurrencyBackend"/> refuses to construct, naming the thing
        /// to configure. A placeholder would instead fail per call, at runtime, far from the cause.
        /// The function names match the reference module in <c>CloudCode~/CurrencyModule</c>.
        /// </remarks>
        public static CloudCodeCurrencyEndpoints Default => new CloudCodeCurrencyEndpoints
        {
            GetCurrencyBalance = "GetCurrencyBalance",
            AddCurrency = "AddCurrency",
        };

        /// <summary>True when the module name and every endpoint name has been filled in.</summary>
        public bool IsComplete =>
            !string.IsNullOrEmpty(ModuleName) &&
            !string.IsNullOrEmpty(GetCurrencyBalance) &&
            !string.IsNullOrEmpty(AddCurrency);
    }
}
