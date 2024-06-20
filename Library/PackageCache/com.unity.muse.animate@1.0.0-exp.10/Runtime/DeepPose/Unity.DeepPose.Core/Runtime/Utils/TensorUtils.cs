using System;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEngine;

namespace Unity.DeepPose.Core
{
    static class TensorUtils
    {
        public static T Alloc<T>(int d0, int d1 = -1, int d2 = -1, int d3 = -1) where T : Tensor
        {
            var shape = CreateShape(d0, d1, d2, d3);
            if (typeof(T) == typeof(TensorFloat))
            {
                return TensorFloat.AllocZeros(shape) as T;
            }

            return TensorInt.AllocZeros(shape) as T;
        }

        static TensorShape CreateShape(int d0, int d1, int d2, int d3)
        {
            if (d3 >= 0)
            {
                return new TensorShape( d0, d1, d2, d3 );
            }

            if (d2 >= 0)
            {
                return new TensorShape(d0, d1, d2);
            }

            if (d1 >= 0)
            {
                return new TensorShape(d0, d1 );
            }

            if (d0 >= 0)
            {
                return new TensorShape(d0);
            }

            Debug.LogError($"Invalid buffer shape: ({d0}, {d1}, {d2})");

            return new TensorShape(0);
        }

        public static TensorFloat NewTensorFloat(TensorShape shape)
        {
            var tensor = new TensorFloat(shape, new float[shape.length]);
            return tensor;
        }

        public static TensorInt NewTensorInt(TensorShape shape)
        {
            var tensor = new TensorInt(shape, new int[shape.length]);
            return tensor;
        }

        public static void Fill(TensorFloat t, float value)
        {
            for (var i = 0; i < t.shape.length; ++i)
            {
                t[i] = value;
            }
        }

        public static void Fill(TensorInt t, int value)
        {
            for (var i = 0; i < t.shape.length; ++i)
            {
                t[i] = value;
            }
        }

        /// <summary>
        /// Asynchronously read the value of the tensor (e.g. at end of model execution).
        /// </summary>
        /// <param name="tensor">The tensor holding the result of the operation.</param>
        /// <param name="type"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static async Task<T> ReadDataAsync<T>(this T tensor, BackendType type = BackendType.CPU) where T : Tensor
        {
            var success = await tensor.CompleteOperationsAndDownloadAsync();
            if (!success)
            {
                throw new InvalidOperationException("Async readback failed.");
            }
            return tensor;
        }
    }
}