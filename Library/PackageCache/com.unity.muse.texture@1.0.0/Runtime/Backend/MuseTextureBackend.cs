using System;
using System.Linq;
using UnityEngine;
using Unity.Muse.Common;
using UnityEngine.Networking;

namespace Unity.Muse.Texture
{
    abstract class MuseTextureBackend: GenerativeAIBackend
    {
        /// <summary>
        /// Initiate Text to Texture generation on Cloud. It only allocates texture ids and actual generation occurs in background.
        /// Use `RequestStatus` to query progress and `DownloadImage` to download intermediate or final result.
        /// </summary>
        /// <param name="prompt">Text prompt for texture generation</param>
        /// <param name="settings">Additional request settings</param>
        /// <param name="onDone">Callback called when results are received. Callback parameters (TextToImageResponse, string).
        /// <returns>The the reference to the async operation this generates so that it may be cancelled</returns>
        /// In case error occured error string is non-null and other parameters are null</param>
        public static UnityWebRequestAsyncOperation GenerateImage(string prompt,
            TextToImageRequest settings,
            Action<TextToImageResponse, string> onDone)
        {
            return SendJsonRequest(TexturesUrl.generate, new TextToImageItemRequest(prompt, settings),
                RequestHandler<TextToImageResponse>((data, error) =>
                {
                    if (data != null)
                        data.seed = settings.seed;
                    onDone?.Invoke(data, error);
                }));
        }

        /// <summary>
        /// Takes an image artifact and requests a PBR map to be generated from it. Must have one call made for each map type you wish to extract.
        /// </summary>
        /// <param name="fromImage">The image artifact to send for PBR map extraction. When requesting a normal map, the heightmap must be used and not the source image artifact</param>
        /// <param name="mapTypes"></param>
        /// <param name="onDone"></param>
        /// <returns>The the reference to the async operation this generates so that it may be cancelled</returns>
        internal static UnityWebRequestAsyncOperation GenerateBatchPbrMap(ImageArtifact fromImage, PbrMapTypes[] mapTypes, Action<BatchPbrResponse, string> onDone)
        {
            var backendMapTypeNames = mapTypes.Select(GetMapTypeName).ToArray();
            var request = new GenerateBatchPbrMapRequest(fromImage.Guid, backendMapTypeNames);
            return SendJsonRequest(TexturesUrl.pbr, request, RequestHandler(onDone));
        }

        internal static string GetMapTypeName(PbrMapTypes mapType) =>
            mapType switch
            {
                PbrMapTypes.BaseMap => "emission",
                PbrMapTypes.Emission => "emission",
                PbrMapTypes.Height => "height",
                PbrMapTypes.Normal => "normal",
                PbrMapTypes.Metallic => "metallic",
                PbrMapTypes.Smoothness => "roughness",
                PbrMapTypes.AO => "ao",
                PbrMapTypes.Delighted => "delighted",
                _ => string.Empty
            };

        internal static string GetPBRMapGuid(PbrMapGuids guids, PbrMapTypes mapType) =>
            mapType switch
            {
                PbrMapTypes.BaseMap => guids.emission,
                PbrMapTypes.Emission => guids.emission,
                PbrMapTypes.Height => guids.height,
                PbrMapTypes.Normal => guids.normal,
                PbrMapTypes.Metallic => guids.metallic,
                PbrMapTypes.Smoothness => guids.roughness,
                PbrMapTypes.AO => guids.ao,
                PbrMapTypes.Delighted => guids.delighted,
                _ => string.Empty
            };

        /// <summary>
        /// Request image upscale
        /// </summary>
        /// <param name="artifact">Previously generated texture artifact</param>
        /// <param name="onDone">Callback called when results are received. Callback parameters (UpscaleImageResponse, string).
        /// <returns>The the reference to the async operation this generates so that it may be cancelled</returns>
        /// UpscaleImageResponse contains newly generated image guid, you should track it's progress via RequestStatus call.
        /// In case error occured error string is non-null and other parameters are null</param>
        public static UnityWebRequestAsyncOperation UpscaleImage(Artifact artifact, Action<UpscaleImageResponse, string> onDone)
        {
            return SendJsonRequest(TexturesUrl.upscale, new GuidItemRequest(artifact.Guid), RequestHandler(onDone));
        }
    }
}
