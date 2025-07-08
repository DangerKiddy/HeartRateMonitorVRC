using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeartRateMonitorVRC.Services
{
    internal class VRChatService
    {
        private OSC _osc;

        public VRChatService(OSC osc)
        {
            _osc = osc;
        }

        public void SendHeartRateInfoToVRChat(ProcessedHeartRate heartRate)
        {
            _osc.Send("/avatar/parameters/Heartrate2", heartRate.BPMRange01);
            _osc.Send("/avatar/parameters/HRPercent", heartRate.BPMRange01);
            _osc.Send("/avatar/parameters/FullHRPercent", heartRate.BPMRange_MinusOne_One);
            _osc.Send("/avatar/parameters/HR", heartRate.BPM);

            _osc.Send("/avatar/parameters/onesHR", heartRate.Ones);
            _osc.Send("/avatar/parameters/tensHR", heartRate.Tens);
            _osc.Send("/avatar/parameters/hundredsHR", heartRate.Hundreds);

            _osc.Send("/avatar/parameters/HeartrateLowest", heartRate.LowestBPMRange01);
            _osc.Send("/avatar/parameters/HeartrateHighest", heartRate.HighestBPMRange01);

            _osc.Send("/avatar/parameters/isHRConnected", true);
            _osc.Send("/avatar/parameters/isHRActive", true);
        }
    }
}
