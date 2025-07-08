namespace HeartRateMonitorVRC.Services
{
    internal struct ProcessedHeartRate
    {
        public int BPM;
        public float BPMRange01;
        public float BPMRange_MinusOne_One;
        public int Ones;
        public int Tens;
        public int Hundreds;

        public int LowestBPM;
        public float LowestBPMRange01;

        public int HighestBPM;
        public float HighestBPMRange01;
    }
}
