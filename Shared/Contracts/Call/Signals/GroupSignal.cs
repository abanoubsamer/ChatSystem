namespace Contracts.Call.Signals
{
    public class GroupSignal
    {
        public string SessionId { get; set; } 
        public string? PeerId { get; set; }
        public string? PeerName { get; set; }
        public string SignalType { get; set; } = null!; // "offer", "answer", "ice_candidate"
        public string? sdp { get; set; }
        public string? Candidate { get; set; }
        public string? sdpMid { get; set; }
        public int? SdpMLineIndex { get; set; }

        // Legacy support
        public string? SignalData { get; set; }
    }
}
