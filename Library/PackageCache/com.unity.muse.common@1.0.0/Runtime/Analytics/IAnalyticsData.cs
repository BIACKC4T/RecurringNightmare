using System;

namespace Unity.Muse.Common.Analytics
{
    interface IAnalyticsData
    {
        string EventName { get; }
        int Version { get; }
    }
}
