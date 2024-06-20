using System.Diagnostics;
using UnityEngine;

namespace Unity.Muse.Animate
{
    class TextToMotionRequest
    {
        Status m_State;

        public enum Status
        {
            Stopped,
            WaitingForBaking,
            Baking,
            Completed,
            Failed,
            Canceled
        }

        public Status State
        {
            get => m_State;
            set
            {
                if (m_State == value)
                    return;

                m_State = value;
                OnStateChanged?.Invoke(this, m_State);
            }
        }

        public bool IsActive => State == Status.Baking || State == Status.WaitingForBaking;

        public delegate void StateChanged(TextToMotionRequest service, Status state);

        public event StateChanged OnStateChanged;

        public delegate void RequestStarted(TextToMotionRequest request);

        public event RequestStarted OnStarted;

        public delegate void RequestCanceled(TextToMotionRequest request);

        public event RequestCanceled OnCanceled;

        public delegate void RequestCompleted(TextToMotionRequest request);

        public event RequestCompleted OnCompleted;

        public delegate void RequestProgressed(TextToMotionRequest request, float overallProgress);

        public event RequestProgressed OnProgressed;

        public delegate void RequestFailed(TextToMotionRequest request, string error);

        public event RequestFailed OnFailed;

        public TextToMotionService Service { get; }
        public TextToMotionTake Target { get; }

        public TextToMotionRequest(TextToMotionService service, TextToMotionTake target)
        {
            Service = service;
            Target = target;
            target.TrackRequest(this);
        }

        public void Start()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TextToMotionRequest -> Start()");

            Service.Baker.Prompt = Target.Prompt;
            Service.Baker.Seed = Target.RequestedSeed;
            Service.Baker.Temperature = Target.RequestTemperature;
            Service.Baker.Length = Target.Length;
            Service.Baker.ModelType = Target.Model;

            RegisterToBaking();

            State = Status.WaitingForBaking;
            OnStarted?.Invoke(this);

            Service.Baking.QueueBaking(false);
        }

        public void Cancel()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TextToMotionRequest -> Cancel()");

            Stop();
        }

        void Stop()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TextToMotionRequest -> Stop()");

            if (Service.Baking.IsRunning)
            {
                // Stop the baking logic. This will trigger the OnBakingCanceled event.
                Service.Baking.Cancel();
            }
            else
            {
                // We are not baking, so we need to manually trigger the OnBakingCanceled event.
                OnBakingCanceled(Service.Baking);
            }
        }

        void RegisterToBaking()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TextToMotionRequest -> RegisterToBaking()");

            Service.Baking.OnBakingStarted += OnBakingStarted;
            Service.Baking.OnBakingCanceled += OnBakingCanceled;
            Service.Baking.OnBakingFailed += OnBakingFailed;
            Service.Baking.OnBakingCompleted += OnBakingCompleted;
            Service.Baking.OnBakingProgressed += OnBakingProgressed;
        }

        void UnregisterFromBaking()
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TextToMotionRequest -> UnregisterFromBaking()");

            Service.Baking.OnBakingStarted -= OnBakingStarted;
            Service.Baking.OnBakingCanceled -= OnBakingCanceled;
            Service.Baking.OnBakingFailed -= OnBakingFailed;
            Service.Baking.OnBakingCompleted -= OnBakingCompleted;
            Service.Baking.OnBakingProgressed -= OnBakingProgressed;
        }

        void OnBakingStarted(BakingLogic logic)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TextToMotionRequest -> OnBakingStarted()");

            State = Status.Baking;
            OnStarted?.Invoke(this);
        }

        void OnBakingProgressed(BakingLogic logic, float overallProgress)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TextToMotionRequest -> OnBakingProgressed(" + overallProgress + ")");

            State = Status.Baking;
            OnProgressed?.Invoke(this, overallProgress);
        }

        void OnBakingCompleted(BakingLogic logic)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TextToMotionRequest -> OnBakingCompleted()");

            UnregisterFromBaking();
            State = Status.Completed;

            Service.Output.CopyTo(Target.BakedTimelineModel);
            
            // Depending on the API version, we may or may not receive seed and temperature values from the server.
            Target.Seed = Service.Baker.Seed ?? Target.Seed;
            Target.Temperature = Service.Baker.Temperature ?? Target.Temperature;
            
            OnCompleted?.Invoke(this);
        }

        void OnBakingFailed(BakingLogic logic, string error)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TextToMotionRequest -> OnBakingFailed(" + error + ")");

            UnregisterFromBaking();
            State = Status.Failed;
            OnFailed?.Invoke(this, error);
        }

        void OnBakingCanceled(BakingLogic logic)
        {
            DevLogger.LogSeverity(TraceLevel.Verbose, "TextToMotionRequest -> OnBakingCanceled()");

            UnregisterFromBaking();
            State = Status.Canceled;
            OnCanceled?.Invoke(this);
        }
    }
}
