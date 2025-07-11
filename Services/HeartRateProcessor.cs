using System;
using System.Threading.Tasks;

namespace HeartRateMonitorVRC.Services
{
    internal class HeartRateProcessor
    {
        private const int DEFAULT_NEXT_BEAT_DELAY = 100;
        private const int ONE_MILLISECOND = 1000;
        private const int ONE_SECOND = 60;

        public delegate void BeatEmulationHandler(bool isBeat, float millisecondsBeforeNextBeat);
        public event BeatEmulationHandler OnHeartBeatEmulationUpdate;
        public event Action<ProcessedHeartRate> OnHeartRateProcessed;

        private int _currentBpm = 120;
        private int _lowestBpm = int.MaxValue;
        private int _highestBpm = 0;

        private ProcessedHeartRate _processedHeartRate;
        public ProcessedHeartRate ProcessedHeartRate { get => _processedHeartRate; }

        private bool _shouldEmulateBeatEffect = false;

        public HeartRateProcessor()
        {
            _processedHeartRate = new ProcessedHeartRate();

            EmulateBeatEffect();
        }

        public void UpdateBPM(int bpm)
        {
            _shouldEmulateBeatEffect = true;

            _currentBpm = bpm;
            SetLowestAndHighesetBPM();

            ExplodeBPMToDigits(out int ones, out int tens, out int hundreds);

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

        private void SetLowestAndHighesetBPM()
        {
            if (_currentBpm == 0)
                return;

            if (_currentBpm < _lowestBpm)
                _lowestBpm = _currentBpm;

            if (_currentBpm > _highestBpm)
                _highestBpm = _currentBpm;
        }

        private void ExplodeBPMToDigits(out int ones, out int tens, out int hundreds)
        {
            string hrAsStr = _currentBpm.ToString();

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

        private float Remap(float input, float inputStart, float inputEnd, float outputStart, float outputEnd)
        {
            return outputStart + ((outputEnd - outputStart) / (inputEnd - inputStart)) * (input - inputStart);
        }

        private async void EmulateBeatEffect()
        {
            while (true)
            {
                var nextBeat = _currentBpm == 0 ? DEFAULT_NEXT_BEAT_DELAY : (int)((1f / _currentBpm) * ONE_MILLISECOND) * ONE_SECOND;

                if (_shouldEmulateBeatEffect)
                {
                    OnHeartBeatEmulationUpdate?.Invoke(true, nextBeat);

                    var beatOtherPart = nextBeat / 3;
                    nextBeat = nextBeat - beatOtherPart;

                    await Task.Delay(beatOtherPart);

                    OnHeartBeatEmulationUpdate?.Invoke(false, nextBeat);
                }

                await Task.Delay(nextBeat);
            }
        }
    }
}
