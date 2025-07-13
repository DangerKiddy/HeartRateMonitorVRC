namespace HeartRateMonitorVRC.Services
{
    internal class VrChatService
    {
        private readonly Osc _osc;

        public VrChatService(Osc osc)
        {
            _osc = osc;
        }

        public void SendHeartRateInfoToVrChat(ProcessedHeartRate heartRate)
        {
            _osc.Send("/avatar/parameters/Heartrate2", heartRate.BpmRangeZeroToOne);
            _osc.Send("/avatar/parameters/HRPercent", heartRate.BpmRangeZeroToOne);
            _osc.Send("/avatar/parameters/FullHRPercent", heartRate.BpmRangeMinusOneToOne);
            _osc.Send("/avatar/parameters/RangePercent", heartRate.BpmRangeLowestToHighest);
            _osc.Send("/avatar/parameters/HR", heartRate.Bpm);

            _osc.Send("/avatar/parameters/onesHR", heartRate.Ones);
            _osc.Send("/avatar/parameters/tensHR", heartRate.Tens);
            _osc.Send("/avatar/parameters/hundredsHR", heartRate.Hundreds);

            _osc.Send("/avatar/parameters/HeartrateLowest", heartRate.LowestBpmRangeZeroToOne);
            _osc.Send("/avatar/parameters/HeartrateHighest", heartRate.HighestBpmRangeZeroToOne);

            _osc.Send("/avatar/parameters/isHRConnected", true);
            _osc.Send("/avatar/parameters/isHRActive", true);
        }
    }
}
