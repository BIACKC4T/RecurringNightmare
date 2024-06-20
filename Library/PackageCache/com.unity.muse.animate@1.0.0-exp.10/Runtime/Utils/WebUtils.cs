using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.DeepPose.Cloud;

using UnityEngine;

namespace Unity.Muse.Animate
{
    static class WebUtils
    {
        public static void SendRequestWithAuthHeaders<TRequest, TResponse>(this WebAPI webAPI,
            TRequest request,
            Action<TResponse> onSuccess,
            Action<Exception> onError,
            string method = "post")
            where TRequest : ISerializable
            where TResponse : IDeserializable<JObject>, new()
        {
            Application.Instance.GetAuthHeaders(headers =>
            {
                webAPI.SendRequest(request, onSuccess, onError, headers.ToList(), method);
            });
        }
        
        public static void SendRequestWithAuthHeaders<TRequest>(this WebAPI webAPI,
            TRequest request,
            Action<byte[]> onSuccess,
            Action<Exception> onError)
            where TRequest : ISerializable
        {
            Application.Instance.GetAuthHeaders(headers =>
            {
                webAPI.SendRequest(request, onSuccess, onError, headers.ToList());
            });
        }

        public static void SendJobRequestWithAuthHeaders<TRequest, TResponse>(this WebAPI webAPI,
            TRequest request,
            Action<TResponse> onSuccess,
            Action<Exception> onError)
            where TRequest : ISerializable
            where TResponse : IDeserializable<JObject>, new()
        {
            WebAPI statusJobAPI = null;
            WebAPI resultJobAPI = null;
            string jobGuid = "";

            Application.Instance.GetAuthHeaders(headers =>
            {
                webAPI.SendRequest<TRequest, JobAPI.Response>(
                    request,
                    response => HandleJobRequest(response),
                    onError,
                    headers.ToList()
                );
            });

            async void HandleJobRequest(JobAPI.Response response) {
                if (response.Guid != null) {
                    if (response.Status == null) {
                        jobGuid = response.Guid;

                        statusJobAPI = new WebAPI(ApplicationConstants.CloudInferenceHost, $"{JobAPI.ApiStatusName}{jobGuid}");
                        resultJobAPI = new WebAPI(ApplicationConstants.CloudInferenceHost, $"{JobAPI.ApiStatusName}{jobGuid}{JobAPI.ApiResultName}");
                    } else {
                        if (response.Status == JobAPI.JobStatus.done.ToString()) {
                            resultJobAPI.SendRequestWithAuthHeaders<JobAPI.Request, TResponse>(new JobAPI.Request(),
                                onSuccess,
                                onError,
                                "get");

                            return;
                        } else if (response.Status == JobAPI.JobStatus.failed.ToString()) {
                            onError?.Invoke(new Exception("Compute job failed"));

                            return;
                        }
                    }

                    await Task.Delay(500);

                    statusJobAPI.SendRequestWithAuthHeaders<JobAPI.Request, JobAPI.Response>(new JobAPI.Request(),
                        response => HandleJobRequest(response),
                        onError,
                        "get");
                } else {
                    onError?.Invoke(new Exception("No job GUID"));
                }
            }
        }

        public static string BackendUrl => Locator.TryGet(out IBackendSettings settings)
            ? settings.Url
            : ApplicationConstants.CloudInferenceHost;
    }
}
