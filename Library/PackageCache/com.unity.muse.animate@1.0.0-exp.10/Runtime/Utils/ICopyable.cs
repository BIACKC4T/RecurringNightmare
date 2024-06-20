using System;

namespace Unity.Muse.Animate
{
    interface ICopyable<T>
    {
        void CopyTo(T other);
        T Clone();
    }
}
