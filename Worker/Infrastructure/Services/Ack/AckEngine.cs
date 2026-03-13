using Application.Dtos.Ack;
using Domain.Models;
using Domain.Models.Result;
using Domain.Models.State;
using Domain.Models.State.DataStructures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Ack
{

    /// <summary>
    /// Dual-state engine managing both Delivery and Read acks
    /// </summary>
    public sealed class AckEngine : IDisposable
    {
        // Fast in-memory (hot path) - YOUR UltraFastAckState
        private readonly AckStateDs _fastState;
        // Reference to Orleans persistent state
        private readonly ChatAckState _persistentState;
        private readonly ConcurrentDictionary<string, string> _pendingDelivery;
        private readonly ConcurrentDictionary<string, string> _pendingRead;

        public AckEngine(
            ChatAckState persistentState,
            int memberCount)
        {
            _persistentState = persistentState;
            _fastState = new AckStateDs(memberCount);
            _pendingDelivery = new ConcurrentDictionary<string, string>();
            _pendingRead = new ConcurrentDictionary<string, string>();
            HydrateFromPersistent();
        }

        private void HydrateFromPersistent()
        {
            //// Load delivery watermarks
            //foreach (var (userId, msgId) in _persistentState.DeliveryWatermarks)
            //{
            //    _fastState.UpdateDelivery(userId, msgId);
            //}

            //// Load read watermarks
            //foreach (var (userId, msgId) in _persistentState.ReadWatermarks)
            //{
            //    _fastState.UpdateRead(userId, msgId);
            //}
        }

        /// <summary>
        /// O(1) amortized - Ultra fast with automatic persistence
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AckResult UpdateDelivery(string userId, string msgId)
        {
            var result = _fastState.UpdateDelivery(userId, msgId);

            // ✅ O(1) - Overwrite if exists
            _pendingDelivery[userId] = msgId;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AckResult UpdateRead(string userId, string msgId)
        {
            var result = _fastState.UpdateRead(userId, msgId);

            // ✅ O(1) - Overwrite if exists
            _pendingRead[userId] = msgId;

            return result;
        }


        /// <summary>
        /// O(u) - u = unique users, already deduped!
        /// </summary>
        public async Task FlushAsync(IPersistentState<ChatAckState> persistentState)
        {
            if (_pendingDelivery.IsEmpty && _pendingRead.IsEmpty) return;

            var batchD = new List<KeyValuePair<string, string>>();
            var batchR = new List<KeyValuePair<string, string>>();

            // Safely extract and clear
            foreach (var key in _pendingDelivery.Keys)
            {
                if (_pendingDelivery.TryRemove(key, out var msgId))
                {
                    batchD.Add(new KeyValuePair<string, string>(key, msgId));
                }
            }

            foreach (var key in _pendingRead.Keys)
            {
                if (_pendingRead.TryRemove(key, out var msgId))
                {
                    batchR.Add(new KeyValuePair<string, string>(key, msgId));
                }
            }

            foreach (var kvp in batchD)
                persistentState.State.DeliveryWatermarks[kvp.Key] = kvp.Value;

            foreach (var kvp in batchR)
                persistentState.State.ReadWatermarks[kvp.Key] = kvp.Value;

            var (dMin, rMin) = _fastState.GetGlobalMins();
            persistentState.State.GlobalDeliveryMin = dMin;
            persistentState.State.GlobalReadMin = rMin;
            persistentState.State.LastUpdated = DateTime.UtcNow;

            await persistentState.WriteStateAsync();
        }

        /// <summary>
        /// Emergency sync flush (for deactivation)
        /// </summary>
        public async Task EmergencyFlushAsync(IPersistentState<ChatAckState> persistentState, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            await FlushAsync(persistentState);
        }

        // Pass-through to fast state
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

        private readonly record struct PendingAck(
            string UserId,
            string MsgId,
            AckType Type,
            DateTime Timestamp
        );
    }
}