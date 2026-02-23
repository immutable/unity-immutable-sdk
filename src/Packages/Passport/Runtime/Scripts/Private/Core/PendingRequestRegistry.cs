using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Immutable.Passport.Core
{
    /// <summary>
    /// Tracks in-flight game bridge requests by mapping request IDs to their completion sources.
    /// </summary>
    internal class PendingRequestRegistry
    {
        private readonly Dictionary<string, UniTaskCompletionSource<string>> _requests = new Dictionary<string, UniTaskCompletionSource<string>>();

        /// <summary>
        /// Registers a new pending request and returns its completion source.
        /// </summary>
        internal UniTaskCompletionSource<string> Register(string requestId)
        {
            var completion = new UniTaskCompletionSource<string>();
            _requests.Add(requestId, completion);
            return completion;
        }

        /// <summary>
        /// Returns true if a pending request exists for the given ID.
        /// </summary>
        internal bool Contains(string requestId)
        {
            return _requests.ContainsKey(requestId);
        }

        /// <summary>
        /// Retrieves the completion source for a pending request.
        /// </summary>
        internal UniTaskCompletionSource<string> Get(string requestId)
        {
            return _requests[requestId];
        }

        /// <summary>
        /// Removes a completed request from the registry.
        /// </summary>
        internal void Remove(string requestId)
        {
            _requests.Remove(requestId);
        }
    }
}
