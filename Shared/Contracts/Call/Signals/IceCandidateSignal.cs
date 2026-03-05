namespace Contracts.Call.Signals
{
    public class IceCandidateSignal
    {
        public string TargetUserId { get; set; }
        public string Candidate { get; set; }
        public string SdpMid { get; set; }
        public int? SdpMLineIndex { get; set; }
    }
}
