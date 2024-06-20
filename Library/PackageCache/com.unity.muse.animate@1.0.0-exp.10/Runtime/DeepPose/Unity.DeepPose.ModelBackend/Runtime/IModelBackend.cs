using System;
using System.Collections;
using Unity.Sentis;

namespace Unity.DeepPose.ModelBackend
{
    interface IModelBackend: IDisposable
    {
        ModelDefinition ModelDefinition { get; }
        void SetInput(string inputName, Tensor inputTensor);
        void Execute();
        Tensor PeekOutput(string outputName);
        IEnumerator StartManualSchedule();
        float scheduleProgress { get; }
    }
}
