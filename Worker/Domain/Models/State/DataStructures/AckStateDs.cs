using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Application.Dtos.Ack;
using Domain.Models.Result;

namespace Domain.Models.State.DataStructures;

/// <summary>
/// Memory-Optimized AckState
/// - Single string[] بدل two Dictionaries للـ MsgId mapping
/// - BitArray للـ count tracking بدل List<int>
/// - Packed short[] للـ user entries بدل Dictionary<string, DualEntry>
/// </summary>
public sealed class AckStateDs : IDisposable
{
    private readonly int _memberCount;
    private readonly int _capacity;          // حجم ثابت للـ buffer
    private readonly int _mask;

    // Circular buffer for msgIds
    private readonly string?[] _msgIdRegistry;
    private readonly short[] _msgDeliveryCounts;
    private readonly short[] _msgReadCounts;

    // Dictionary للـ msgId → int index
    private readonly Dictionary<string, int> _msgIdToInt;

    // User state
    private readonly int[] _watermarkDelivery;
    private readonly int[] _watermarkRead;
    private readonly Dictionary<string, int> _userIndex;
    private int _userCount;

    // Circular buffer state
    private int _writeIndex = 1;
    private int _nextMsgId; // Logical ID

    // Global mins
    private int _currentDeliveryMin;
    private int _currentReadMin;
    private string? _cachedDeliveryMinStr;
    private string? _cachedReadMinStr;

    public AckStateDs(int memberCount, int bufferCapacity = 131072)
    {
        if ((bufferCapacity & (bufferCapacity - 1)) != 0)
            throw new ArgumentException("Capacity must be power of 2", nameof(bufferCapacity));

        _memberCount = memberCount;
        _capacity = bufferCapacity;
        _mask = bufferCapacity - 1;

        _msgIdRegistry = new string?[bufferCapacity];
        _msgDeliveryCounts = new short[bufferCapacity];
        _msgReadCounts = new short[bufferCapacity];
        _msgIdToInt = new Dictionary<string, int>(bufferCapacity);

        _watermarkDelivery = new int[memberCount];
        _watermarkRead = new int[memberCount];
        _userIndex = new Dictionary<string, int>(memberCount);

        _nextMsgId = 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetOrAddUserIndex(string userId)
    {
        if (_userIndex.TryGetValue(userId, out var idx)) return idx;

        idx = _userCount++;
        _userIndex[userId] = idx;
        return idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetOrAddMsgId(string msgId)
    {
        if (_msgIdToInt.TryGetValue(msgId, out var idx)) return idx;

        // Assign next index in circular buffer
        idx = _writeIndex;
        _writeIndex = (idx + 1) & _mask;

        // Remove old msgId from dictionary if overwrite happens
        var oldMsgId = _msgIdRegistry[idx];
        if (oldMsgId != null) _msgIdToInt.Remove(oldMsgId);

        // Write new msgId into buffer
        _msgIdRegistry[idx] = msgId;
        _msgDeliveryCounts[idx] = 0;
        _msgReadCounts[idx] = 0;

        // Add to dictionary
        _msgIdToInt[msgId] = idx;

        _nextMsgId++;
        return idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string? GetMsgIdString(int idx) =>
        idx == -1 ? null : _msgIdRegistry[idx];

    public AckResult UpdateDelivery(string userId, string msgId)
    {
        int msgIdx = GetOrAddMsgId(msgId);
        int userIdx = GetOrAddUserIndex(userId);

        if (_watermarkDelivery[userIdx] == msgIdx)
            return new AckResult(userId, msgId, null, _cachedDeliveryMinStr, false, AckType.Delivery);

        if (_watermarkRead[userIdx] >= msgIdx)
            return new AckResult(userId, msgId, null, _cachedDeliveryMinStr, false, AckType.Delivery);

        _watermarkDelivery[userIdx] = msgIdx;
        int threshold = _memberCount - 1;
        bool changed = false;

        if (++_msgDeliveryCounts[msgIdx] >= threshold && msgIdx > _currentDeliveryMin)
        {
            _currentDeliveryMin = msgIdx;
            _cachedDeliveryMinStr = GetMsgIdString(msgIdx);
            changed = true;
        }

        return new AckResult(userId, msgId, null, _cachedDeliveryMinStr, changed, AckType.Delivery);
    }
    public AckResult UpdateRead(string userId, string msgId)
    {
        int msgIdx = GetOrAddMsgId(msgId);
        int userIdx = GetOrAddUserIndex(userId);

        if (_watermarkRead[userIdx] == msgIdx)
            return new AckResult(userId, msgId, null, _cachedReadMinStr, false, AckType.Seen);

        bool changed = false;
        int threshold = _memberCount - 1;
       
        // Auto-delivery if not yet delivered
        if (_watermarkDelivery[userIdx] != msgIdx)
        {
            _watermarkDelivery[userIdx] = msgIdx;
            if (++_msgDeliveryCounts[msgIdx] >= threshold && msgIdx > _currentDeliveryMin)
            {
                _currentDeliveryMin = msgIdx;
                _cachedDeliveryMinStr = GetMsgIdString(msgIdx);
                changed = true;
            }
        }
        _watermarkRead[userIdx] = msgIdx;
        if (++_msgReadCounts[msgIdx] >= threshold && msgIdx > _currentReadMin)
        {
            _currentReadMin = msgIdx;
            _cachedReadMinStr = GetMsgIdString(msgIdx);
            changed = true;
        }

        return new AckResult(userId, msgId, null, _cachedReadMinStr, changed, AckType.Seen);
    }
    public (string? DeliveryMin, string? ReadMin) GetGlobalMins() =>
        (_cachedDeliveryMinStr, _cachedReadMinStr);
    public bool IsFullyDeliveredUpTo(string msgId) {
        return  _msgIdToInt.TryGetValue(msgId, out var logical) && _currentDeliveryMin >= logical; }
    public bool IsFullyReadUpTo(string msgId) {
        return _msgIdToInt.TryGetValue(msgId, out var logical) && _currentReadMin >= logical; }
    public void Dispose()
    {
        _msgIdToInt.Clear();
        _userIndex.Clear();
        Array.Clear(_msgIdRegistry);
        Array.Clear(_msgDeliveryCounts);
        Array.Clear(_msgReadCounts);
        Array.Clear(_watermarkDelivery);
        Array.Clear(_watermarkRead);
    }
}