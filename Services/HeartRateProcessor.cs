using System;
using System.Threading.Tasks;

namespace HeartRateMonitorVRC.Services
{
    internal class HeartRateProcessor
    {
        private const int DefaultNextBeatDelay = 100;
        private const int OneMillisecond = 1000;
        private const int OneSecond = 60;

        public delegate void BeatEmulationHandler(bool isBeat, float millisecondsBeforeNextBeat);
        public event BeatEmulationHandler OnHeartBeatEmulationUpdate;
        public event Action<ProcessedHeartRate> OnHeartRateProcessed;

        private int _currentBpm = 120;
        private int _lowestBpm = int.MaxValue;
        private int _highestBpm;

        private ProcessedHeartRate _processedHeartRate;

        private bool _shouldEmulateBeatEffect;

        public HeartRateProcessor()
        {
            _processedHeartRate = new ProcessedHeartRate();

            EmulateBeatEffect();
        }

        public void UpdateBpm(int bpm)
        {
            _shouldEmulateBeatEffect = true;

            _currentBpm = bpm;
            SetLowestAndHighestBpm();

            ExplodeBpmToDigits(out int ones, out int tens, out int hundreds);

            _processedHeartRate.Bpm = _currentBpm;
            _processedHeartRate.BpmRangeZeroToOne = _currentBpm / 255f;
            _processedHeartRate.BpmRangeMinusOneToOne = Remap(_currentBpm, 0, 255, -1, 1);
            _processedHeartRate.Ones = ones;
            _processedHeartRate.Tens = tens;
            _processedHeartRate.Hundreds = hundreds;

            _processedHeartRate.LowestBpm = _lowestBpm;
            _processedHeartRate.LowestBpmRangeZeroToOne = _lowestBpm / 255f;

            _processedHeartRate.HighestBpm = _highestBpm;
            _processedHeartRate.HighestBpmRangeZeroToOne = _highestBpm / 255f;

            OnHeartRateProcessed?.Invoke(_processedHeartRate);
        }

        private void SetLowestAndHighestBpm()
        {
            if (_currentBpm == 0)
                return;

            if (_currentBpm < _lowestBpm)
                _lowestBpm = _currentBpm;

            if (_currentBpm > _highestBpm)
                _highestBpm = _currentBpm;
        }

        private void ExplodeBpmToDigits(out int ones, out int tens, out int hundreds)
        {
            var hrAsStr = _currentBpm.ToString();

            ones = 0;
            tens = 0;
            hundreds = 0;

            if (_currentBpm < 100)
            {
                ones = int.Parse(hrAsStr[1].ToString());
                tens = int.Parse(hrAsStr[0].ToString());
            }
            else
            {
                ones = int.Parse(hrAsStr[2].ToString());
                tens = int.Parse(hrAsStr[1].ToString());
                hundreds = int.Parse(hrAsStr[0].ToString());
            }
        }

        private static float Remap(float input, float inputStart, float inputEnd, float outputStart, float outputEnd)
        {
            return outputStart + (outputEnd - outputStart) / (inputEnd - inputStart) * (input - inputStart);
        }

        private async void EmulateBeatEffect()
        {
            while (true)
            {
                int nextBeat = _currentBpm == 0 ? DefaultNextBeatDelay : (int)(1f / _currentBpm * OneMillisecond) * OneSecond;

                if (_shouldEmulateBeatEffect)
                {
                    OnHeartBeatEmulationUpdate?.Invoke(true, nextBeat);

                    int beatOtherPart = nextBeat / 3;
                    nextBeat -= beatOtherPart;

                    await Task.Delay(beatOtherPart);

                    OnHeartBeatEmulationUpdate?.Invoke(false, nextBeat);
                }

                await Task.Delay(nextBeat);
            }
        }
    }
}
