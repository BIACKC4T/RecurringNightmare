using System;

namespace Unity.Muse.Animate
{
    /// <summary>
    /// A stack that overwrites the oldest elements when it reaches its capacity.
    /// </summary>
    class CircularStack<T>
    {
        readonly int m_DataLength;
        readonly T[] m_Data;

        int m_Head;
        int m_Tail;

        public CircularStack(int capacity)
        {
            m_DataLength = capacity + 1;
            m_Data = new T[m_DataLength];
            m_Head = 0;
            m_Tail = 0;
        }

        public void Push(T item)
        {
            m_Data[m_Head] = item;
            m_Head = (m_Head + 1) % m_DataLength;
            if (m_Head == m_Tail)
            {
                m_Tail = (m_Tail + 1) % m_DataLength;
            }
        }

        public T Pop()
        {
            if (m_Head == m_Tail)
            {
                return default(T);
            }

            if (--m_Head < 0)
            {
                m_Head += m_DataLength;
            }
            return m_Data[m_Head];
        }

        public T Peek()
        {
            if (m_Head == m_Tail)
            {
                return default(T);
            }

            return m_Data[m_Head];
        }

        public void Clear()
        {
            m_Head = 0;
            m_Tail = 0;
        }

        public int Count
        {
            get
            {
                if (m_Head == m_Tail)
                {
                    return 0;
                }

                if (m_Head > m_Tail)
                {
                    return m_Head - m_Tail;
                }

                return m_DataLength - m_Tail + m_Head;
            }
        }
    }
}
