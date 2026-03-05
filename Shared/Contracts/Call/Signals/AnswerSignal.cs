namespace Contracts.Call.Signals
{
    public class AnswerSignal
    {
        public string TargetUserId { get; set; }
        public string Sdp { get; set; }
        public string SessionId { get; set; } // مطلوب
    }
}
