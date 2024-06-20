using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Muse.Animate
{
    [Serializable]
    class ThumbnailModel
    {
        public static Texture2D BlendingTexture { get; set; }

        public int[] Shape = { 256, 256 };

        public Texture2D Texture
        {
            get => m_Texture;
            set
            {
                if (m_Texture)
                {
                    GameObjectUtils.Destroy(m_Texture);
                }
                m_Texture = value;
                OnChanged?.Invoke();
            }
        }

        public Vector3 Position
        {
            get => m_Position;
            set
            {
                m_Position = value;
                OnChanged?.Invoke();
            }
        }

        public Quaternion Rotation
        {
            get => m_Rotation;
            set
            {
                m_Rotation = value;
                OnChanged?.Invoke();
            }
        }

        Texture2D m_Texture;

        [SerializeField]
        Vector3 m_Position;
        
        [SerializeField]
        Quaternion m_Rotation;

        public event Action OnChanged;

        public void CopyTo(ThumbnailModel other)
        {
            Assert.AreEqual(Shape.Length, other.Shape.Length);
            Shape.CopyTo(other.Shape, 0);

            other.Position = Position;
            other.Rotation = Rotation;

            if (Texture == null)
            {
                other.Texture = null;
            }
            else
            {
                other.ValidateTexture(Texture.width, Texture.height);
                PlatformUtils.CopyTexture(Texture, other.Texture);
            }
            
            other.OnChanged?.Invoke();
        }

        public void Read(RenderTexture renderTexture)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            ValidateTexture(renderTexture.width, renderTexture.height);
            
            Texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, true);
            Texture.Apply();

            Shape[0] = renderTexture.width;
            Shape[1] = renderTexture.height;

            RenderTexture.active = previous;

            OnChanged?.Invoke();
        }

        public void Read(Texture2D texture)
        {
            ValidateTexture(texture.width, texture.height);
            PlatformUtils.CopyTexture(texture, Texture);

            Shape[0] = texture.width;
            Shape[1] = texture.height;

            OnChanged?.Invoke();
        }

        public void Blend(RenderTexture blend, float opacity = 0.05f)
        {
            var original = Texture;
            ValidateBlendingTexture(blend.width, blend.height);

            var previous = RenderTexture.active;

            RenderTexture.active = blend;

            BlendingTexture.ReadPixels(new Rect(0, 0, blend.width, blend.height), 0, 0);
            BlendingTexture.Apply(true);

            for (var y = 0; y < original.height; y++)
            {
                for (var x = 0; x < original.width; x++)
                {
                    original.SetPixel(
                        x, y, 
                        ColorUtils.MergeBlend(
                            original.GetPixel(x, y), 
                            BlendingTexture.GetPixel(x, y), 
                            opacity)
                        );
                }
            }

            original.Apply();
            RenderTexture.active = previous;
        }

        void ValidateTexture(int width, int height)
        {
            if (Texture == null || Texture.width != width || Texture.height != height)
            {
                GameObjectUtils.Destroy(Texture);
                Texture = new Texture2D(width, height);
                // Setting this flags means that Unity will not clean this up when the scene changes, which
                // is useful when we're in the editor
                if (!UnityEngine.Application.isPlaying)
                {
                    Texture.hideFlags = HideFlags.DontSaveInEditor;
                }
            }
        }

        public static void ValidateBlendingTexture(int width, int height)
        {
            if (BlendingTexture == null || BlendingTexture.width != width || BlendingTexture.height != height)
            {
                GameObjectUtils.Destroy(BlendingTexture);
                BlendingTexture = new Texture2D(width, height);
                
                if (!UnityEngine.Application.isPlaying)
                {
                    BlendingTexture.hideFlags = HideFlags.DontSaveInEditor;
                }
            }
        }
    }
}
