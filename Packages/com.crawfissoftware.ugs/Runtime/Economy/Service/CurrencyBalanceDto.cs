using System;

namespace CrawfisSoftware.UGS.Economy
{
    /// <summary>
    /// The response shape of the Cloud Code currency endpoints: one currency and the balance the
    /// server holds for this player after the call.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>An object rather than a bare number so the endpoint can say which currency it answered
    /// about. A trusted backend exists precisely so the client stops assuming, and a lone integer
    /// invites the caller to assume it matched the currency asked for.</para>
    /// <para>Declared here rather than taken from generated Cloud Code bindings: those are generated
    /// into the consumer's own Assets under a fixed assembly name, which a package assembly cannot
    /// reference.</para>
    /// <para><b>Wire contract.</b> These two property names are what the deployed module returns.
    /// Renaming either deserializes to a default without any error.</para>
    /// </remarks>
    [Serializable]
    public class CurrencyBalanceDto
    {
        public string CurrencyId { get; set; }
        public long Balance { get; set; }
    }
}
