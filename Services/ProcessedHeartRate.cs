namespace HeartRateMonitorVRC.Services
{
    internal struct ProcessedHeartRate
    {
        public int Bpm;
        public float BpmRangeZeroToOne;
        public float BpmRangeMinusOneToOne;
        public int Ones;
        public int Tens;
        public int Hundreds;
        public int LowestBpm;
        public float LowestBpmRangeZeroToOne;
        public int HighestBpm;
        public float HighestBpmRangeZeroToOne;
    }
}
