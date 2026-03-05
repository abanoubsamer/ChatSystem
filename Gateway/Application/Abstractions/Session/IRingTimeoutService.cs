using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Session
{
    /// <summary>
    /// Manages ring timeout timers for calls.
    /// If no one joins within the timeout period, the call ends automatically.
    /// </summary>
    public interface IRingTimeoutService
    {
        /// <summary>
        /// Starts a ring timer for a session.
        /// If no participant joins before timeout, the cancellation callback fires.
        /// </summary>
        void StartRingTimer(string sessionId, TimeSpan timeout, Func<Task> onTimeoutAsync);

        /// <summary>
        /// Cancels the ring timer (call was answered).
        /// </summary>
        void CancelRingTimer(string sessionId);
    }
}
