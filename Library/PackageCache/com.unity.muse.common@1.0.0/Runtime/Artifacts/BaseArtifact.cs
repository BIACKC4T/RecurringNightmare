using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Muse.Common
{
    [Serializable]
    internal abstract class Artifact: IEquatable<Artifact>
    {
        public static readonly string k_InvalidGUID = string.Empty;
        [SerializeField]
        public string Guid = k_InvalidGUID;
        [SerializeField]
        public uint Seed;
        [SerializeReference]
        protected List<IOperator> m_Operators;
        [SerializeReference]
        public List<Artifact> history = new();

        protected Artifact(string guid, uint seed)
        {
            Guid = guid;
            Seed = seed;
            history.Add(this);
        }
        public delegate void ArtifactPreviewDelegate(Texture2D preview, byte[] rawData, string errorMessage);

        /// <summary>
        /// This flag informs if the artifact's generate content is safe.
        /// </summary>
        public virtual bool isSafe => true;

        public abstract void GetPreview(ArtifactPreviewDelegate onDoneCallback, bool useCache);
        /// <summary>
        /// Only called once for a generation group
        /// </summary>
        /// <param name="model">Model used.</param>
        public virtual void StartGenerate(Model model) { }
        public abstract void Generate(Model model);
        public abstract void RetryGenerate(Model model);
        public abstract ArtifactView CreateView();
        public virtual ArtifactView CreateCanvasView()
        {
            var view = CreateView();
            view.UpdateView();
            return view;
        }

        public delegate void ArtifactGenerationDelegate(Artifact artifact, string errorMessage);
        public ArtifactGenerationDelegate OnGenerationDone;
        public List<IOperator> GetOperators()
        {
            return m_Operators;
        }

        public void UnregisterFromEvents(Model model)
        {
            foreach (var op in m_Operators)
            {
                op.UnregisterFromEvents(model);
            }
        }

        public void RegisterToEvents(Model model)
        {
            foreach (var op in m_Operators)
            {
                op.RegisterToEvents(model);
            }
        }

        public bool Equals(Artifact other)
        {
            return ((object)other) != null && Guid == other.Guid;
        }

        public override bool Equals(object obj)
        {
            return obj != null && (obj is Artifact artifact) && Equals(artifact);
        }

        public override int GetHashCode()
        {
            return Guid?.GetHashCode() ?? -1;
        }

        public static bool operator ==(Artifact lhs, Artifact rhs)
        {
            return ReferenceEquals(lhs, rhs) || (lhs != null) && lhs.Equals(rhs);
        }

        public static bool operator !=(Artifact lhs, Artifact rhs)
        {
            return ((object)lhs != null) && !lhs.Equals(rhs);
        }
        //ideally we have to set the operator data in the constructor
        public void SetOperators(IEnumerable<IOperator> operators)
        {
            //need to do a deep copy...
            m_Operators = CloneOperators(operators);
        }

        /// <summary>
        /// Clone the given operators, or if null, this artifact's operators.
        /// </summary>
        /// <param name="operators">The operators to clone.</param>
        /// <returns>The cloned operators.</returns>
        public List<IOperator> CloneOperators(IEnumerable<IOperator> operators = null)
        {
            if (operators == null)
                operators = m_Operators;

            var result = new List<IOperator> ();
            foreach (var op in operators)
            {
                if (!op.IsSavable())
                    continue;
                result.Add(op.Clone());
            }

            return result;
        }

        public virtual Artifact Clone(string mode)
        {
            var artifact = ArtifactFactory.CreateArtifact(mode);
            artifact.Guid = Guid;
            artifact.Seed = Seed;
            artifact.history = history.ToList();

            artifact.SetOperators(m_Operators);

            return artifact;
        }

        public virtual void Variate(List<IOperator> ops)
        {
            return;
        }

        public virtual void Shape(List<IOperator> ops)
        {
            return;
        }
    }
}
