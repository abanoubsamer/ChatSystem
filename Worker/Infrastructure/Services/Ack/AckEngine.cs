using Application.Dtos.Ack;
using Domain.Models;
using Domain.Models.Result;
using Domain.Models.State;
using Domain.Models.State.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Ack
{
    /// <summary>
    /// Dual-state engine managing both Delivery and Read acks.
    /// Refactored to remove manual locks, relying on Orleans Grain's single-threaded execution.
    /// </summary>
    public sealed class AckEngine : IDisposable
    {
        // Fast in-memory (hot path)
        private readonly AckStateDs _fastState;

        private readonly Dictionary<string, string> _pendingDelivery; 
        private readonly Dictionary<string, string> _pendingRead;    

        public AckEngine(ChatAckState persistentState, int memberCount)
        {
            _fastState = new AckStateDs(memberCount);
            _pendingDelivery = new Dictionary<string, string>();
            _pendingRead = new Dictionary<string, string>();

            HydrateFromPersistent(persistentState);
        }

        private void HydrateFromPersistent(ChatAckState persistentState)
        {
            foreach (var (userId, msgId) in persistentState.DeliveryWatermarks)
            {
                _fastState.UpdateDelivery(userId, msgId);
            }

            foreach (var (userId, msgId) in persistentState.ReadWatermarks)
            {
                _fastState.UpdateRead(userId, msgId);
            }
        }

        /// <summary>
        /// O(1) amortized - Ultra fast
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AckResult UpdateDelivery(string userId, string msgId)
        {
            var result = _fastState.UpdateDelivery(userId, msgId);
            _pendingDelivery[userId] = msgId;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AckResult UpdateRead(string userId, string msgId)
        {
            var result = _fastState.UpdateRead(userId, msgId);
            _pendingRead[userId] = msgId;
            return result;
        }

        /// <summary>
        /// Flushes pending changes to the persistent state.
        /// Guaranteed to be called from the Grain's single-threaded context.
        /// </summary>
        public async Task FlushAsync(IPersistentState<ChatAckState> persistentState)
        {
            if (_pendingDelivery.Count == 0 && _pendingRead.Count == 0) return;

            foreach (var (userId, msgId) in _pendingDelivery)
                persistentState.State.DeliveryWatermarks[userId] = msgId;

            foreach (var (userId, msgId) in _pendingRead)
                persistentState.State.ReadWatermarks[userId] = msgId;

            _pendingDelivery.Clear();
            _pendingRead.Clear();

            var (dMin, rMin) = _fastState.GetGlobalMins();
            persistentState.State.GlobalDeliveryMin = dMin;
            persistentState.State.GlobalReadMin = rMin;
            persistentState.State.LastUpdated = DateTime.UtcNow;

            await persistentState.WriteStateAsync();
        }

        public (string? DeliveryMin, string? ReadMin) GetGlobalMins() =>
            _fastState.GetGlobalMins();

        public bool IsFullyDelivered(string msgId) =>
            _fastState.IsFullyDeliveredUpTo(msgId);

        public bool IsFullyRead(string msgId) =>
            _fastState.IsFullyReadUpTo(msgId);

        public void Dispose()
        {
            _fastState.Dispose();
        }
    }
}
