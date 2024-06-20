using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Muse.Animate
{
    class ThumbnailsService
    {
        public bool HasRequests => m_Requests.Count > 0;

        List<ThumbnailRequest> m_Requests = new();
        bool m_IsBusy;

        readonly TimelineKeyViewLogic m_TimelineKeyViewLogic;
        readonly BakedTimelineViewLogic m_BakedTimelineViewLogic;

        readonly CameraContext m_CameraContext;
        readonly CameraMovementModel m_CameraMovementModel;
        readonly CameraModel m_CameraModel;

        struct ThumbnailRequest
        {
            public ThumbnailModel Owner { get; set; }
            public KeyModel Key { get; set; }
            public BakedTimelineModel BakedTimeline { get; set; }
            public int Frame { get; set; }
            public Vector3 CameraPosition { get; set; }
            public Quaternion CameraRotation { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public float PrevSteps { get; set; }
            public float PrevStepSize { get; set; }
            public float NextSteps { get; set; }
            public float NextStepSize { get; set; }
        }

        public ThumbnailsService(Camera thumbnailCamera)
        {
            m_CameraContext = new CameraContext(thumbnailCamera);
            m_TimelineKeyViewLogic = new TimelineKeyViewLogic();
            m_BakedTimelineViewLogic = new BakedTimelineViewLogic("Thumbnail Service");
            m_CameraModel = new CameraModel(thumbnailCamera);
            m_CameraMovementModel = new CameraMovementModel(m_CameraModel);
            thumbnailCamera.cullingMask = ApplicationLayers.LayerMaskThumbnail;
        }

        public void AddEntity(EntityID entityID, ArmatureMappingComponent referenceViewArmature)
        {
            m_TimelineKeyViewLogic.AddEntity(entityID, referenceViewArmature, ApplicationLayers.LayerThumbnail);
            m_BakedTimelineViewLogic.AddEntity(entityID, referenceViewArmature, ApplicationLayers.LayerThumbnail);
        }

        public void RemoveEntity(EntityID entityID)
        {
            m_TimelineKeyViewLogic.RemoveEntity(entityID);
            m_BakedTimelineViewLogic.RemoveEntity(entityID);
        }

        public void RequestThumbnail(ThumbnailModel target, KeyModel key, Vector3 position, Quaternion rotation)
        {
            // Save the last position and rotation of the thumbnail camera on the key itself
            // Used later when re-rendering the same key without changing the camera
            key.SetThumbnailCameraTransform(position, rotation);

            // Look into the existing requests if one already is present for this key
            if (UpdateExistingRequest(target, key))
                return;

            // If found no matching entries, create a new request
            CreateNewRequest(target, key);
        }

        public void RequestThumbnail(ThumbnailModel target, BakedTimelineModel timeline, int frame, Vector3 position, Quaternion rotation, int trailPrev, int trailPrevSize, int trailNext, int trailNextSize)
        {
            // Look into the existing requests if one already is present for this key
            if (UpdateExistingRequest(target, timeline, frame, position, rotation, trailPrev, trailPrevSize, trailNext, trailNextSize))
                return;

            // If found no matching entries, create a new request
            CreateNewRequest(target, timeline, frame, position, rotation, trailPrev, trailPrevSize, trailNext, trailNextSize);
        }

        public void Update()
        {
            DoNextRequest();
        }

        void DoNextRequest()
        {
            if (m_Requests.Count == 0)
                return;

            var request = m_Requests[0];
            m_Requests.RemoveAt(0);
            
            if (request.Key == null && request.BakedTimeline == null)
            {
                throw new MissingReferenceException("Missing both Key and BakedTimeline in Thumbnail Request.");
            }
            
            if (request.Key != null)
            {
                DoRequestKey(ref request);
                return;
            }
            
            if (request.BakedTimeline != null)
            {
                DoRequestBakedTimeline(ref request);
                return;
            }
        }

        void DoRequestKey(ref ThumbnailRequest request)
        {
            // Show the visuals and update the scene to represent the pose(s) on the key
            m_TimelineKeyViewLogic.IsVisible = true;
            m_TimelineKeyViewLogic.ApplyKey(request.Key);
            
            // Calculate the bounds of the shown poses and frame the camera
            var bounds = m_TimelineKeyViewLogic.GetBounds();
            FrameCamera(bounds, request.CameraPosition, request.CameraRotation);
            
            // Render the camera to a temporary render texture and update the thumbnail
            var renderTexture = RenderTexture.GetTemporary(request.Width, request.Height, 24);
            m_CameraContext.Camera.RenderToTexture(renderTexture, false);
            request.Owner.Read(renderTexture);
            RenderTexture.ReleaseTemporary(renderTexture);
            
            // Hide the visuals
            m_TimelineKeyViewLogic.IsVisible = false;
        }

        void DoRequestBakedTimeline(ref ThumbnailRequest request)
        {
            // Show the visuals and update the scene to represent the pose(s) on the baked timeline frame
            m_BakedTimelineViewLogic.IsVisible = true;
            m_BakedTimelineViewLogic.SetModel(request.BakedTimeline);
            
            // Render the main frame
            RenderBakedTimelineFrame(ref request, request.Frame);
            
            // Render the trails
            // TODO: Improve this trail rendering system to use a shader for the blending
            for (var i = 0; i < request.PrevSteps; i++)
            {
                var prevFrame = request.Frame - (int)(i * request.PrevStepSize);
                
                if (prevFrame >= 0)
                {
                    RenderBakedTimelineFrame(ref request, prevFrame, true);
                }
            }
            
            for (var i = 0; i < request.NextSteps; i++)
            {
                var nextFrame = request.Frame + (int)(i * request.NextStepSize);
                
                if (nextFrame >= 0 && nextFrame < request.BakedTimeline.FramesCount)
                {
                    RenderBakedTimelineFrame(ref request, nextFrame, true);
                }
            }

            m_BakedTimelineViewLogic.IsVisible = false;
        }

        void RenderBakedTimelineFrame(ref ThumbnailRequest request, int frame, bool blend = false)
        {
            m_BakedTimelineViewLogic.DisplayFrame(request.BakedTimeline.GetFrame(frame));

            if (blend)
            {
                var blendRenderTexture = RenderTexture.GetTemporary(request.Width, request.Height, 24);
                m_CameraContext.Camera.RenderToTexture(blendRenderTexture, false);
                request.Owner.Blend(blendRenderTexture);
                RenderTexture.ReleaseTemporary(blendRenderTexture);
            }
            else
            {
                var bounds = m_BakedTimelineViewLogic.GetBounds();
                FrameCamera(bounds, request.CameraPosition, request.CameraRotation, 0.8f);
                var renderTexture = RenderTexture.GetTemporary(request.Width, request.Height, 24);
                m_CameraContext.Camera.RenderToTexture(renderTexture, false);
                request.Owner.Read(renderTexture);
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        void FrameCamera(in Bounds bounds, in Vector3 position, in Quaternion rotation, float margin = 1.1f)
        {
            m_CameraMovementModel.SetPivotAndOrbit(position, rotation);
            m_CameraMovementModel.Frame(bounds, margin, true);
        }
        
        bool UpdateExistingRequest(ThumbnailModel target, KeyModel key)
        {
            // Look into the existing requests if one already is present for this key,
            // if yes, update the request and keep it as is.
            for (var i = 0; i < m_Requests.Count; i++)
            {
                var request = m_Requests[i];

                if (request.Key != key)
                    continue;

                if (request.Owner != target)
                    continue;

                request.CameraPosition = key.Thumbnail.Position;
                request.CameraRotation = key.Thumbnail.Rotation;
                request.Width = key.Thumbnail.Shape[0];
                request.Height = key.Thumbnail.Shape[1];

                m_Requests[i] = request;
                return true;
            }

            return false;
        }

        void CreateNewRequest(ThumbnailModel target, KeyModel key)
        {
            m_Requests.Add(new ThumbnailRequest()
            {
                Owner = target,
                Key = key,
                CameraPosition = key.Thumbnail.Position,
                CameraRotation = key.Thumbnail.Rotation,
                Width = key.Thumbnail.Shape[0],
                Height = key.Thumbnail.Shape[1]
            });
        }

        bool UpdateExistingRequest(ThumbnailModel target, BakedTimelineModel timeline, int frame, Vector3 position, Quaternion rotation, int prevSteps, int prevStepSize, int nextSteps, int nextStepSize)
        {
            // Look into the existing requests if one already is present for this key,
            // if yes, update the request and keep it as is.
            for (var i = 0; i < m_Requests.Count; i++)
            {
                var request = m_Requests[i];

                if (request.Frame != frame)
                    continue;

                if (request.Owner != target)
                    continue;

                request.CameraPosition = position;
                request.CameraRotation = rotation;
                request.Width = 128;
                request.Height = 128;
                request.NextSteps = nextSteps;
                request.NextStepSize = nextStepSize;
                request.PrevSteps = prevSteps;
                request.PrevStepSize = prevStepSize;
                
                m_Requests[i] = request;
                return true;
            }

            return false;
        }

        void CreateNewRequest(ThumbnailModel target, BakedTimelineModel timeline, int frame, Vector3 position, Quaternion rotation, int prevSteps, int prevStepSize, int nextSteps, int nextStepSize)
        {
            m_Requests.Add(new ThumbnailRequest()
            {
                Owner = target,
                BakedTimeline = timeline,
                Frame = frame,
                CameraPosition = position,
                CameraRotation = rotation,
                Width = 128,
                Height = 128,
                NextSteps = nextSteps,
                NextStepSize = nextStepSize,
                PrevSteps = prevSteps,
                PrevStepSize = prevStepSize
            });
        }

        public void CancelRequestOf(ThumbnailModel target)
        {
            for (var i = 0; i < m_Requests.Count; i++)
            {
                var request = m_Requests[i];

                if (request.Owner != target)
                    continue;

                m_Requests.RemoveAt(i);
                break;
            }
        }
    }
}
