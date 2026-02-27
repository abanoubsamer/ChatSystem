using BenchmarkDotNet.Attributes;
using Domain.Models.State.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class AckStateBench
    { private AckStateDs _state;

    private string[] _users;
    private string[] _messages;

    [Params(3, 10, 100,1000)]
    public int MemberCount;

    [GlobalSetup]
    public void Setup()
    {
        _state = new AckStateDs(MemberCount);

        _users = Enumerable.Range(1, MemberCount)
                           .Select(i => $"user{i}")
                           .ToArray();

        _messages = Enumerable.Range(1, 1000)
                              .Select(i => $"msg{i}")
                              .ToArray();
    }

    [Benchmark]
    public void DeliveryFlow()
    {
        foreach (var msg in _messages)
        {
            foreach (var user in _users)
            {
                _state.UpdateDelivery(user, msg);
            }
        }
    }

    [Benchmark]
    public void ReadFlow()
    {
        foreach (var msg in _messages)
        {
            foreach (var user in _users)
            {
                _state.UpdateRead(user, msg);
            }
        }
    }
    }
}
