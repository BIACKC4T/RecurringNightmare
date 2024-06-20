using UnityEngine;

namespace Unity.Muse.Animate.Editor
{
    [CreateAssetMenu(fileName = "MuseAnimateSession", menuName = "Muse/Animate Generator", order = 0)]
    class MuseAnimateSession : ScriptableObject
    {
        [SerializeField]
        byte[] m_JsonData;

        string m_Json;

        public string JsonData
        {
            get
            {
                if (m_Json == null)
                    m_Json = System.Text.Encoding.UTF8.GetString(m_JsonData);
                return m_Json;
            }
        }

        public void SetData(StageModel stageModel)
        {
            m_Json = stageModel.Save();
            m_JsonData = System.Text.Encoding.UTF8.GetBytes(m_Json);
        }
    }
}