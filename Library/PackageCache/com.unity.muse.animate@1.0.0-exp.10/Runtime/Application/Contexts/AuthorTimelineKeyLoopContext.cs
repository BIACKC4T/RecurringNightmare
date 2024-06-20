using System;
using UnityEngine.UIElements;

namespace Unity.Muse.Animate
{
    class AuthorTimelineKeyLoopContext
    {
        public LoopAuthoringLogic LoopAuthoringLogic => m_LoopAuthoringLogic;
        public AuthoringModel AuthoringModel => m_AuthoringModel;
        public TimelineModel Timeline => m_TimelineModel;
        public SelectionModel<TimelineModel.SequenceKey> KeySelection => m_KeySelectionModel;
        public SelectionModel<EntityID> EntitySelection => m_EntitySelectionModel;
        public CameraMovementModel CameraMovement => m_CameraContext.CameraMovement;
        public CameraContext CameraContext => m_CameraContext;
        public CameraModel Camera => m_CameraContext.Camera;
        public LoopKeyToolbarView LoopKeyToolbar => m_LoopKeyToolbar;
        public UndoRedoToolbarView UndoRedoToolbar => m_UndoRedoToolbar;

        readonly LoopAuthoringLogic m_LoopAuthoringLogic;
        readonly AuthoringModel m_AuthoringModel;
        readonly TimelineModel m_TimelineModel;
        readonly CameraContext m_CameraContext;

        readonly SelectionModel<EntityID> m_EntitySelectionModel;
        readonly SelectionModel<TimelineModel.SequenceKey> m_KeySelectionModel;

        LoopKeyToolbarView m_LoopKeyToolbar;
        UndoRedoToolbarView m_UndoRedoToolbar;

        public AuthorTimelineKeyLoopContext(VisualElement uiRoot, LoopAuthoringLogic loopAuthoringLogic, AuthoringModel authoringModel, TimelineModel timelineModel,
            SelectionModel<TimelineModel.SequenceKey> keySelectionModel, SelectionModel<EntityID> entitySelectionModel,
            CameraContext cameraContext)
        {
            m_LoopAuthoringLogic = loopAuthoringLogic;
            m_AuthoringModel = authoringModel;
            m_TimelineModel = timelineModel;
            m_CameraContext = cameraContext;

            m_EntitySelectionModel = entitySelectionModel;
            m_KeySelectionModel = keySelectionModel;

            m_LoopKeyToolbar = uiRoot.Q<LoopKeyToolbarView>(LoopKeyToolbarView.defaultName);
            m_UndoRedoToolbar = uiRoot.Q<UndoRedoToolbarView>(UndoRedoToolbarView.defaultName);
        }
    }
}
