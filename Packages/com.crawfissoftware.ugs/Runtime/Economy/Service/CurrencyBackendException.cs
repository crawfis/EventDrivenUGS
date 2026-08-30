using System;

namespace CrawfisSoftware.UGS.Economy
{
    /// <summary>
    /// A currency operation failed in the backing service.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// Two causes are worth telling apart from everything else, because both are project
    /// configuration rather than code, and both otherwise present as "the number never changes":
    /// an access policy that denies the write, and a currency id that does not exist in the
    /// project's Economy configuration.
    /// </remarks>
    public sealed class CurrencyBackendException : Exception
    {
        public CurrencyBackendException(string message, Exception inner = null)
            : base(message, inner)
        {
        }

        /// <summary>
        /// True when the failure was an access-policy denial. A project whose policy denies player
        /// writes to <c>urn:ugs:economy:*</c> fails every credit with a 403, and the fix is a
        /// project configuration change rather than a code change.
        /// </summary>
        /// <remarks>
        /// Set explicitly by a backend that can read a status code from the SDK. The text fallback
        /// below exists only for backends that cannot - Cloud Code surfaces its failures as
        /// messages - and it deliberately skips this exception's own message, scanning only the
        /// wrapped originals. Scanning our own message once made any credit of 403 coins look like
        /// a denial, because the amount was interpolated into it.
        /// </remarks>
        public bool IsAccessDenied
        {
            get => _isAccessDenied || InnerMessagesContain("403", "Forbidden");
            internal set => _isAccessDenied = value;
        }

        /// <summary>
        /// True when the currency id does not exist in the project's Economy configuration. This is
        /// the failure a typo in the currency id produces, and it is worth naming because the id is
        /// a wire contract checked by nothing at compile time.
        /// </summary>
        public bool IsCurrencyNotFound { get; internal set; }

        private bool _isAccessDenied;

        private bool InnerMessagesContain(params string[] needles)
        {
            for (Exception e = InnerException; e != null; e = e.InnerException)
            {
                string message = e.Message;
                if (message == null) continue;
                for (int i = 0; i < needles.Length; i++)
                {
                    if (message.Contains(needles[i])) return true;
                }
            }
            return false;
        }
    }
}
