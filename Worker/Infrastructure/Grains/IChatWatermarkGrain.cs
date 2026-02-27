using Application.Abstractions.Grain;
using Application.Dtos.Ack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Infrastructure.Grains
{
    //{
    //    //private readonly List<Acked> _buffer = new();
    //    //private IDisposable? _timer;
    //    //private const int BATCH_SIZE = 100;

    //    //public override Task OnActivateAsync(CancellationToken ct)
    //    //{
    //    //    // ✅ Timer بدل Reminder - بيشتغل في نفس الـ Grain
    //    //    _timer = RegisterTimer(
    //    //        FlushAsync,
    //    //        null,
    //    //        TimeSpan.FromMilliseconds(50),  // بعد كام تبدأ
    //    //        TimeSpan.FromMilliseconds(50));  // كل كام تتكرر

    //    //    return Task.CompletedTask;
    //    //}

    //    //public Task ReceiveAckAsync(Acked ack)
    //    //{
    //    //    _buffer.Add(ack);
    //    //    if (_buffer.Count >= BATCH_SIZE)
    //    //        return FlushAsync(null);
    //    //    return Task.CompletedTask;
    //    //}

    //    //private async Task FlushAsync(object? _)
    //    //{
    //    //    if (_buffer.Count == 0) return;

    //    //    var batch = _buffer.ToList();
    //    //    _buffer.Clear();

    //    //    var watermarkGrain = GrainFactory.GetGrain<IChatWatermarkGrain>(
    //    //        this.GetPrimaryKeyString());
    //    //    await watermarkGrain.ProcessBatchAsync(batch);
    //    //}

    //    //public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    //    //{
    //    //    _timer?.Dispose();
    //    //    return Task.CompletedTask;
    //    //}
    //}
}
