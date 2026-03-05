namespace Contracts.Call.Signals
{
    public class OfferSignal
    {
        public string TargetUserId { get; set; }
        public string Sdp { get; set; }
        public string ChatId { get; set; }
        public string GroupName { get; set; } // لو Group جديدة
    }
}
