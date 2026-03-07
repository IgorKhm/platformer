namespace Util
{
    public class Timer
    {
        public float Duration { get; private set; }
        public float TimeRemaining { get; private set; }
        public bool IsRunning { get; private set; }

        public void Start(float duration)
        {
            Duration = duration;
            TimeRemaining = duration;
            IsRunning = true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;

            TimeRemaining -= deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                IsRunning = false;
            }
        }

        public void Stop()
        {
            TimeRemaining = 0f;
            IsRunning = false;
        }
    }
}
