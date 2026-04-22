using System;
using System.Collections.Concurrent;
using System.Threading;
using AjenticWebRTC;
using Xunit;

namespace AjenticWebRTC.Tests;

public class WorkQueueTests
{
    [Fact]
    public void Enqueue_SingleAction_Executes()
    {
        using var q = new WorkQueue();
        using var done = new ManualResetEventSlim();
        q.Enqueue(() => done.Set());
        Assert.True(done.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void Enqueue_Multiple_ExecutesInOrder()
    {
        using var q = new WorkQueue();
        var results = new ConcurrentQueue<int>();
        using var done = new ManualResetEventSlim();
        const int count = 50;
        for (int i = 0; i < count; i++)
        {
            int captured = i;
            q.Enqueue(() => results.Enqueue(captured));
        }
        q.Enqueue(() => done.Set());
        Assert.True(done.Wait(TimeSpan.FromSeconds(2)));

        int expected = 0;
        foreach (var v in results)
        {
            Assert.Equal(expected, v);
            expected++;
        }
        Assert.Equal(count, expected);
    }

    [Fact]
    public void Dispose_StopsQueue()
    {
        var q = new WorkQueue();
        q.Dispose();
        Assert.Throws<ObjectDisposedException>(() => q.Enqueue(() => { }));
    }

    [Fact]
    public void Enqueue_Null_Throws()
    {
        using var q = new WorkQueue();
        Assert.Throws<ArgumentNullException>(() => q.Enqueue(null!));
    }
}
