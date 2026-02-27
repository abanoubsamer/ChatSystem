using Domain.Models.State.DataStructures;

public class AckStateDsTests
{
    [Fact]
    public void GroupStressTest_1000Users_100MessagesEach()
    {
        int members = 1000;
        int messagesPerUser = 100;
        var ack = new AckStateDs(memberCount: members, bufferCapacity: 1 << 20); // buffer كبير power of 2

        var users = Enumerable.Range(1, members).Select(i => $"u{i}").ToArray();
        int msgCounter = 1;

        for (int round = 0; round < messagesPerUser; round++)
        {
            foreach (var sender in users)
            {
                string msgId = $"m{msgCounter++}";

                // قبل ما أي receiver يعمل read
                var (prevDeliveryMin, prevReadMin) = ack.GetGlobalMins();

                int count = 0;
                foreach (var receiver in users)
                {
                    if (receiver == sender) continue;

                    var res = ack.UpdateRead(receiver, msgId);
                    count++;

                    // لما يكون آخر receiver
                    if (count == members-1)
                    {

                        // Global min لازم يتغير
                        Assert.True(res.IsGlobalChanged, $"Expected global min to change for message {msgId}");

                        var (currDeliveryMin, currReadMin) = ack.GetGlobalMins();

                        // تأكد إنهم مش null
                        Assert.NotNull(currDeliveryMin);
                        Assert.NotNull(currReadMin);

                        // Global min فعليًا تقدمت للرسالة الحالية
                        Assert.Equal(msgId, currDeliveryMin);
                        Assert.Equal(msgId, currReadMin);

                        // Global min السابق لازم يكون مختلف
                        if (prevDeliveryMin != null)
                            Assert.NotEqual(prevDeliveryMin, currDeliveryMin);
                        if (prevReadMin != null)
                            Assert.NotEqual(prevReadMin, currReadMin);
                    }
                }
            }
        }

        // بعد كل الرسائل
        var (finalDeliveryMin, finalReadMin) = ack.GetGlobalMins();
        Assert.Equal($"m{members * messagesPerUser}", finalDeliveryMin);
        Assert.Equal($"m{members * messagesPerUser}", finalReadMin);
    }
}