using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Muse.Animate
{
    class TextToMotionService
    {
        public bool HasRequests => m_Requests.Count > 0;
        public BakedTimelineModel Output => m_TextToMotionOutput;
        
        List<TextToMotionRequest> m_Requests = new();
        
        bool m_IsBusy;

        public enum Status
        {
            Unknown,
            Loading,
            Ready,
            Failed
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

        public BakingLogic Baking => m_BakingLogic;
        public ITimelineBakerTextToMotion Baker => m_TextToMotionBaker;
        
        #pragma warning disable 67
        // FIXME: Why are these events not used?
        public delegate void StateChanged(TextToMotionService service, Status state);
        public event StateChanged OnStateChanged;

        public delegate void RequestStarted(TextToMotionRequest request);
        public event RequestStarted OnRequestStarted;

        public delegate void RequestCanceled(TextToMotionRequest request);
        public event RequestCanceled OnRequestCanceled;

        public delegate void RequestCompleted(TextToMotionRequest request);
        public event RequestCompleted OnRequestCompleted;

        public delegate void RequestProgressed(TextToMotionRequest request, float overallProgress);
        public event RequestProgressed OnRequestProgressed;

        public delegate void RequestFailed(TextToMotionRequest request, string error);
        public event RequestFailed OnRequestFailed;
        #pragma warning restore 67
        
        Status m_State;
        CameraModel m_CameraModel;
        TextToMotionRequest m_ActiveRequest;
        
        readonly BakedTimelineModel m_TextToMotionOutput;
        readonly ITimelineBakerTextToMotion m_TextToMotionBaker;
        readonly BakingLogic m_BakingLogic;

        public TextToMotionService()
        {
            // Text to Motion
            m_TextToMotionBaker = new TimelineBakerTextToMotionCloud();
            m_TextToMotionOutput = new BakedTimelineModel();
            m_BakingLogic = new BakingLogic(null, bakedTimeline: m_TextToMotionOutput, null, (TimelineBakerBase)m_TextToMotionBaker);
        }

        public void AddEntity(EntityID entityID, ArmatureMappingComponent referencePhysicsArmature, ArmatureMappingComponent referenceMotionArmature, PhysicsEntityType physicsEntityType)
        {
            m_BakingLogic.AddEntity(entityID, referencePhysicsArmature, referenceMotionArmature, physicsEntityType);
        }

        public void RemoveEntity(EntityID entityID)
        {
            m_BakingLogic.RemoveEntity(entityID);
        }
        
        public TextToMotionRequest Request(TextToMotionTake target)
        {
            if (TryGetRequest(target, out var recycledRequest))
            {
                QueueRequest(recycledRequest);
                return recycledRequest;
            }
            
            var newRequest = new TextToMotionRequest(this, target);
            QueueRequest(newRequest);
            return newRequest;
        }

        public void Update(float delta, bool throttle = false)
        {
            if(m_ActiveRequest == null)
                StartNextRequest();

            m_BakingLogic.Update(delta, throttle);
        }
        
        /// <summary>
        /// Cancel all active requests. 
        /// </summary>
        public void Stop()
        {
            if (m_ActiveRequest != null)
            {
                m_ActiveRequest.Cancel();
                m_ActiveRequest = null;
            }

            foreach (var request in m_Requests)
            {
                request.Cancel();
            }
            
            m_Requests.Clear();
        }

        void StartNextRequest()
        {
            if (m_Requests.Count == 0)
                return;

            StartRequest(m_Requests[0]);
            m_Requests.RemoveAt(0);
        }

        void StartRequest(in TextToMotionRequest request)
        {
            if (request.Target != null)
            {
                m_ActiveRequest = request;
                RegisterToActiveRequest();
                m_ActiveRequest.Start();
            }
            else
            {
                throw new Exception("Missing Target in Request.");
            }
        }

        bool TryGetRequest(TextToMotionTake target, out TextToMotionRequest result)
        {
            for (var i = 0; i < m_Requests.Count; i++)
            {
                var request = m_Requests[i];

                if (request.Target != target)
                    continue;
                result = m_Requests[i];
                return true;
            }

            result = null;
            return false;
        }
        
        void QueueRequest(TextToMotionRequest request)
        {
            if (request.IsActive)
            {
                request.Cancel();
            }
            
            if (m_Requests.Contains(request))
            {
                m_Requests.Remove(request);
            }
            
            m_Requests.Add(request);
        }

        public void RemoveRequest(TextToMotionTake target)
        {
            for(var i = 0; i < m_Requests.Count; i++)
            {
                var request = m_Requests[i];

                if (request.Target != target)
                    continue;

                m_Requests[i].Cancel();
                m_Requests.RemoveAt(i);
                break;
            }
        }

        void RegisterToActiveRequest()
        {
            m_ActiveRequest.OnStarted += OnActiveRequestStarted;
            m_ActiveRequest.OnCompleted += OnActiveRequestCompleted;
            m_ActiveRequest.OnFailed += OnActiveRequestFailed;
            m_ActiveRequest.OnProgressed += OnActiveRequestProgressed;
            m_ActiveRequest.OnCanceled += OnActiveRequestCanceled;
        }

        void UnregisterFromActiveRequest()
        {
            m_ActiveRequest.OnStarted -= OnActiveRequestStarted;
            m_ActiveRequest.OnCompleted -= OnActiveRequestCompleted;
            m_ActiveRequest.OnFailed -= OnActiveRequestFailed;
            m_ActiveRequest.OnProgressed -= OnActiveRequestProgressed;
            m_ActiveRequest.OnCanceled -= OnActiveRequestCanceled;
        }
        
        void OnActiveRequestStarted(TextToMotionRequest request)
        {
            
        }

        void OnActiveRequestProgressed(TextToMotionRequest request, float overallprogress)
        {
            OnRequestProgressed?.Invoke(request, overallprogress);
        }

        void OnActiveRequestCompleted(TextToMotionRequest request)
        {
            UnregisterFromActiveRequest();
            m_ActiveRequest = null;
        }
        
        void OnActiveRequestFailed(TextToMotionRequest request, string errorMessage)
        {
            UnregisterFromActiveRequest();
            m_ActiveRequest = null;
            OnRequestFailed?.Invoke(request, errorMessage);
        }
        
        void OnActiveRequestCanceled(TextToMotionRequest request)
        {
            UnregisterFromActiveRequest();
            m_ActiveRequest = null;
        }
    }
}
