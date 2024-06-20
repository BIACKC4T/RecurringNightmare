using System;
using UnityEngine;

namespace Unity.Muse.Animate
{
    class AuthoringModel
    {
        // Events meant to be used by both View Models and Authoring States
        public delegate void ModeChanged();
        public delegate void PosingToolChanged();
        public delegate void LoopToolChanged();
        public delegate void TitleChanged();
        
        public event ModeChanged OnModeChanged;
        public event PosingToolChanged OnPosingToolChanged;
        public event LoopToolChanged OnLoopToolChanged;
        public event TitleChanged OnTitleChanged;

        // Events meant to be used by the Authoring View Models
        public delegate void Changed();
        public event Changed OnChanged;

        // Events meant to be used by the Authoring States
        // - 
        
        // - Library (Takes, timelines, poses, etc) interactions
        public delegate void RequestedDeleteSelectedLibraryItems(LibraryModel library, SelectionModel<LibraryItemModel> selectionModel);
        public event RequestedDeleteSelectedLibraryItems OnRequestedDeleteSelectedLibraryItems;
        public delegate void RequestedSelectLibraryItem(LibraryModel library, SelectionModel<LibraryItemModel> selectionModel, LibraryItemModel item);
        public event RequestedSelectLibraryItem OnRequestedSelectLibraryItem;
        public delegate void RequestedDeleteLibraryItem(LibraryModel library, LibraryItemModel item);
        public event RequestedDeleteLibraryItem OnRequestedDeleteLibraryItem;
        public delegate void RequestedDuplicateLibraryItem(LibraryModel library, LibraryItemModel item);
        public event RequestedDuplicateLibraryItem OnRequestedDuplicateLibraryItem;
        public delegate void RequestedEditLibraryItem(LibraryModel library, LibraryItemModel item);
        public event RequestedEditLibraryItem OnRequestedEditLibraryItem;
        public delegate void RequestedScrollToLibraryItem(LibraryModel library, LibraryItemModel item);
        public event RequestedScrollToLibraryItem OnRequestedScrollToLibraryItem;
        public delegate void RequestedExportLibraryItem(LibraryModel library, LibraryItemModel item);
        public event RequestedExportLibraryItem OnRequestedExportLibraryItem;
        
        // - Timeline and Sequence Keys interactions
        public event Action<ThumbnailModel, KeyModel> OnRequestedGenerateKeyThumbnail;
        public event Action<ThumbnailModel, BakedTimelineModel, int> OnRequestedGenerateFrameThumbnail;
        public event Action<TextToMotionTake> OnRequestedConvertMotionToKeys;

        // - Text to motion
        public event Action<string,int?,int, float, ITimelineBakerTextToMotion.Model> OnRequestedTextToMotionGenerate;
        public event Action<string,int?, float?, int, ITimelineBakerTextToMotion.Model> OnRequestedTextToMotionSolve;
        public event Action<string> OnRequestedSetPrompt;
        
        // - Motion to Timeline
        public event Action<float,bool> OnRequestedMotionToTimelineSolve;
        
        /// <summary>
        /// Authoring modes
        /// </summary>
        public enum AuthoringMode
        {
            /// <summary>
            /// No authoring mode set
            /// </summary>
            Unknown,

            /// <summary>
            /// Editing / Viewing a Timeline
            /// </summary>
            Timeline,
            
            /// <summary>
            /// Editing / Viewing a text to motion take
            /// </summary>
            TextToMotionTake,
            
            /// <summary>
            /// Extracting a timeline (keys) from a baked timeline
            /// </summary>
            ConvertMotionToTimeline
        }

        /// <summary>
        /// Authoring full pose tool
        /// </summary>
        public enum PosingToolType
        {
            /// <summary>
            /// No tool set
            /// </summary>
            None,

            /// <summary>
            /// Dragging effectors
            /// </summary>
            Drag,

            /// <summary>
            /// Translating effectors by dragging or through Gizmo
            /// </summary>
            Translate,

            /// <summary>
            /// Rotating effectors
            /// </summary>
            Rotate,

            /// <summary>
            /// Translating and rotation effectors
            /// </summary>
            Universal,

            /// <summary>
            /// Setting effector tolerance
            /// </summary>
            Tolerance
        }

        /// <summary>
        /// Authoring loop tool
        /// </summary>
        public enum LoopToolType
        {
            /// <summary>
            /// No tool set
            /// </summary>
            None,

            /// <summary>
            /// Set loop translation offset
            /// </summary>
            Translate,

            /// <summary>
            /// Set loop rotation offset
            /// </summary>
            Rotate
        }

        public enum SelectionType
        {
            Entity,
            Effector,
            SequenceKey,
            SequenceTransition
        }

        public SelectionType LastSelectionType { get; set; }
        
        public PosingToolType PosingTool
        {
            get => m_PosingTool;
            set
            {
                // Dev note: For the moment we prevent this tool from being activated at all
                if (value == PosingToolType.Universal)
                {
                    return;
                }
                
                if (m_PosingTool != value)
                {
                    m_PosingTool = value;
                    OnPosingToolChanged?.Invoke();
                }
            }
        }

        public LoopToolType LoopTool
        {
            get => m_LoopTool;
            set
            {
                if (m_LoopTool != value)
                {
                    m_LoopTool = value;
                    OnLoopToolChanged?.Invoke();
                }
            }
        }

        public AuthoringMode Mode
        {
            get => m_Mode;
            set
            {
                if (m_Mode == value)
                    return;

                m_Mode = value;
                OnModeChanged?.Invoke();
                OnChanged?.Invoke();
            }
        }

        public string Title
        {
            get => m_Title;
            set
            {
                if (m_Title == value)
                    return;

                m_Title = value;
                OnTitleChanged?.Invoke();
            }
        }
        
        public string TargetName
        {
            get => m_TargetName;
            set
            {
                if (m_TargetName == value)
                    return;

                m_TargetName = value;
                OnChanged?.Invoke();
            }
        }

        public TextToMotionAuthoringModel TextToMotion => m_TextToMotionAuthoringModel;
        public MotionToTimelineAuthoringModel MotionToTimeline => m_MotionToTimelineAuthoringModel;
        public TimelineAuthoringModel Timeline => m_TimelineAuthoringModel;
        
        /// <summary>
        /// The take that corresponds to the active timeline. Note that the take data and the active timeline
        /// data are distinct.
        /// </summary>
        /// <remarks>
        /// If accessing this property from a SelectionChanged event, the TakeModel corresponds to the previous
        /// selection. This is useful if you want to do something with the previous selection before it is lost.
        /// </remarks>
        public TakeModel ActiveTake { get; internal set; }

        PosingToolType m_PosingTool = PosingToolType.None;
        LoopToolType m_LoopTool = LoopToolType.None;
        AuthoringMode m_Mode = AuthoringMode.Unknown;

        bool m_CanCopyPose;
        bool m_CanEstimatePose;
        bool m_CanDeleteSelectedEntities;
        
        string m_Title = "Untitled";
        string m_TargetName = "Undefined";

        readonly TextToMotionAuthoringModel m_TextToMotionAuthoringModel;
        readonly MotionToTimelineAuthoringModel m_MotionToTimelineAuthoringModel;
        readonly TimelineAuthoringModel m_TimelineAuthoringModel;

        public AuthoringModel()
        {
            m_TextToMotionAuthoringModel = new TextToMotionAuthoringModel();
            m_MotionToTimelineAuthoringModel = new MotionToTimelineAuthoringModel();
            m_TimelineAuthoringModel = new TimelineAuthoringModel();
        }

        public void RequestDeleteSelectedLibraryItems(LibraryModel library, SelectionModel<LibraryItemModel> selectionModel)
        {
            OnRequestedDeleteSelectedLibraryItems?.Invoke(library, selectionModel);
        }
        
        public void RequestDeleteLibraryItem(LibraryModel library, LibraryItemModel item)
        {
            OnRequestedDeleteLibraryItem?.Invoke(library, item);
        }
        
        public void RequestEditLibraryItem(LibraryModel library, LibraryItemModel item)
        {
            OnRequestedEditLibraryItem?.Invoke(library, item);
        }
        
        public void RequestScrollToLibraryItem(LibraryModel library, LibraryItemModel item)
        {
            Debug.Log("AuthoringModel -> RequestScrollToLibraryItem()");
            OnRequestedScrollToLibraryItem?.Invoke(library, item);
        }
        
        public void RequestExportLibraryItem(LibraryModel library, LibraryItemModel item)
        {
            OnRequestedExportLibraryItem?.Invoke(library, item);
        }

        public void RequestDuplicateLibraryItem(LibraryModel library, LibraryItemModel item)
        {
            OnRequestedDuplicateLibraryItem?.Invoke(library, item);
        }
        
        public void RequestSelectLibraryItem(LibraryModel library, SelectionModel<LibraryItemModel> selectionModel, LibraryItemModel item)
        {
            OnRequestedSelectLibraryItem?.Invoke(library, selectionModel, item);
        }
        
        public void RequestTextToMotionGenerate(string prompt, int? seed, int takesAmount, float duration, ITimelineBakerTextToMotion.Model model)
        {
            OnRequestedTextToMotionGenerate?.Invoke(prompt, seed, takesAmount, duration, model);
        }
        
        public void RequestTextToMotionSolve(string prompt, int? seed, float? temperature, int length, ITimelineBakerTextToMotion.Model model)
        {
            OnRequestedTextToMotionSolve?.Invoke(prompt, seed, temperature, length, model);
        }
        
        public void RequestMotionToTimelineSolve(float sensitivity, bool useMotionCompletion)
        {
            OnRequestedMotionToTimelineSolve?.Invoke(sensitivity, useMotionCompletion);
        }
        
        public void RequestGenerateKeyThumbnail(ThumbnailModel target, KeyModel key)
        {
            OnRequestedGenerateKeyThumbnail?.Invoke(target, key);
        }
        public void RequestGenerateFrameThumbnail(ThumbnailModel target, BakedTimelineModel timeline, int frame)
        {
            OnRequestedGenerateFrameThumbnail?.Invoke(target, timeline, frame);
        }

        public void RequestConvertMotionToKeys(TextToMotionTake target)
        {
            OnRequestedConvertMotionToKeys?.Invoke(target);
        }
        
        public void RequestSetPrompt(string prompt)
        {
            OnRequestedSetPrompt?.Invoke(prompt);
        }
    }
}
