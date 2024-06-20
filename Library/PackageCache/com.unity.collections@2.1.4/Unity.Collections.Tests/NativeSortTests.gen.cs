using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.Tests;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using System.Collections;

internal class NativeSortTests : CollectionsTestCommonBase
{
    struct DescendingComparer<T> : IComparer<T> where T : IComparable<T>
    {
        public int Compare(T x, T y) => y.CompareTo(x);
    }

    [Test]
    public void NativeArraySlice_BinarySearch()
    {
        var init = new int[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53 };
        var container = new NativeArray<int>(16, Allocator.Persistent);
        var slice = new NativeSlice<int>(container, 0, container.Length);
        var arrayRo = container.AsReadOnly();
        container.CopyFrom(init);

        for (int i = 0, num = container.Length; i < num; ++i)
        {
            Assert.AreEqual(i, container.BinarySearch(container[i]));
            Assert.AreEqual(i, slice.BinarySearch(container[i]));
            Assert.AreEqual(i, arrayRo.BinarySearch(container[i]));
        }

        container.Dispose();
    }

    struct BinarySearch_Job : IJob
    {
        [ReadOnly]
        public NativeArray<int> array;

        [ReadOnly]
        public NativeSlice<int> slice;

        [ReadOnly]
        public NativeArray<int>.ReadOnly arrayRo;

        [ReadOnly]
        public NativeList<int> nativeList;

        public void Execute()
        {
            for (int i = 0, num = array.Length; i < num; ++i)
            {
                Assert.AreEqual(i, array.BinarySearch(array[i]));
                Assert.AreEqual(i, slice.BinarySearch(array[i]));
                Assert.AreEqual(i, arrayRo.BinarySearch(array[i]));
                Assert.AreEqual(i, nativeList.BinarySearch(array[i]));
            }
        }
    }

    [Test]
    public void BinarySearch_From_Job()
    {
        var init = new int[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53 };
        var container = new NativeArray<int>(16, Allocator.Persistent);
        var slice = new NativeSlice<int>(container, 0, container.Length);
        var arrayRo = container.AsReadOnly();
        container.CopyFrom(init);

        var nativeList = new NativeList<int>(16, Allocator.Persistent);
        nativeList.CopyFrom(container);

        new BinarySearch_Job
        {
            array = container,
            slice = slice,
            arrayRo = arrayRo,
            nativeList = nativeList,

        }.Run();

        container.Dispose();
        nativeList.Dispose();
    }

    [Test]
    public void NativeArraySlice_BinarySearch_NotFound()
    {
        {
            var container = new NativeArray<int>(1, Allocator.Temp);
            var slice = new NativeSlice<int>(container, 0, container.Length);
            var arrayRo = container.AsReadOnly();

            Assert.AreEqual(container.Length, 1);
            Assert.AreEqual(-2, container.BinarySearch(1));
            Assert.AreEqual(-2, slice.BinarySearch(1));
            Assert.AreEqual(-2, arrayRo.BinarySearch(1));

            slice[0] = 1;

            Assert.AreEqual(0, container.BinarySearch(1));
            Assert.AreEqual(0, slice.BinarySearch(1));
            Assert.AreEqual(0, arrayRo.BinarySearch(1));

            Assert.AreEqual(-1, container.BinarySearch(-2));
            Assert.AreEqual(-1, slice.BinarySearch(-2));
            Assert.AreEqual(-1, arrayRo.BinarySearch(-2));

            Assert.AreEqual(-2, container.BinarySearch(2));
            Assert.AreEqual(-2, slice.BinarySearch(2));
            Assert.AreEqual(-2, arrayRo.BinarySearch(2));

            container.Dispose();
        }

        {
            var init = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            var container = new NativeArray<int>(16, Allocator.Temp);
            var slice = new NativeSlice<int>(container, 0, container.Length);
            var arrayRo = container.AsReadOnly();

            container.CopyFrom(init);

            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~container.Length, container.BinarySearch(i + 16));
                Assert.AreEqual(~slice.Length, slice.BinarySearch(i + 16));
                Assert.AreEqual(~arrayRo.Length, arrayRo.BinarySearch(i + 16));
            }

            container.Dispose();
        }

        {
            var init = new int[] { 0, 2, 4, 6, 8, 10, 12, 14 };
            var container = new NativeArray<int>(8, Allocator.Temp);
            var slice = new NativeSlice<int>(container, 0, container.Length);
            var arrayRo = container.AsReadOnly();

            container.CopyFrom(init);

            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, container.BinarySearch(i * 2 + 1));
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, slice.BinarySearch(i * 2 + 1));
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, arrayRo.BinarySearch(i * 2 + 1));
            }

            container.Dispose();
        }
    }

#if !UNITY_DOTSRUNTIME
    [Test]
    public void NativeArraySlice_BinarySearch_NotFound_Reference_ArrayList()
    {
        {
            var reference = new ArrayList();
            reference.Add(0);
            var container = new NativeArray<int>(1, Allocator.Temp);
            var slice = new NativeSlice<int>(container, 0, container.Length);
            var arrayRo = container.AsReadOnly();

            Assert.AreEqual(container.Length, 1);
            Assert.AreEqual(-2, reference.BinarySearch(1));
            Assert.AreEqual(-2, container.BinarySearch(1));
            Assert.AreEqual(-2, slice.BinarySearch(1));
            Assert.AreEqual(-2, arrayRo.BinarySearch(1));

            reference[0] = 1;
            slice[0] = 1;

            Assert.AreEqual(0, reference.BinarySearch(1));
            Assert.AreEqual(0, container.BinarySearch(1));
            Assert.AreEqual(0, slice.BinarySearch(1));
            Assert.AreEqual(0, arrayRo.BinarySearch(1));

            Assert.AreEqual(-1, reference.BinarySearch(-2));
            Assert.AreEqual(-1, container.BinarySearch(-2));
            Assert.AreEqual(-1, slice.BinarySearch(-2));
            Assert.AreEqual(-1, arrayRo.BinarySearch(-2));

            Assert.AreEqual(-2, reference.BinarySearch(2));
            Assert.AreEqual(-2, container.BinarySearch(2));
            Assert.AreEqual(-2, slice.BinarySearch(2));
            Assert.AreEqual(-2, arrayRo.BinarySearch(2));
        }

        {
            var init = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            var container = new NativeArray<int>(16, Allocator.Temp);
            var slice = new NativeSlice<int>(container, 0, container.Length);
            var arrayRo = container.AsReadOnly();

            container.CopyFrom(init);
            var reference = new ArrayList(init);

            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~reference.Count, reference.BinarySearch(i + 16));
                Assert.AreEqual(~container.Length, container.BinarySearch(i + 16));
                Assert.AreEqual(~slice.Length, slice.BinarySearch(i + 16));
                Assert.AreEqual(~arrayRo.Length, arrayRo.BinarySearch(i + 16));
            }
        }

        {
            var init = new int[] { 0, 2, 4, 6, 8, 10, 12, 14 };
            var container = new NativeArray<int>(8, Allocator.Temp);
            var slice = new NativeSlice<int>(container, 0, container.Length);
            var arrayRo = container.AsReadOnly();

            container.CopyFrom(init);
            var reference = new ArrayList(init);

            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, reference.BinarySearch(i * 2 + 1));
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, container.BinarySearch(i * 2 + 1));
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, slice.BinarySearch(i * 2 + 1));
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, arrayRo.BinarySearch(i * 2 + 1));
            }
        }
    }
#endif



    [Test]
    public void NativeList_BinarySearch()
    {
        using (var container = new NativeList<int>(16, Allocator.Persistent) { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53 })
        {
            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(i, container.BinarySearch(container[i]));
            }
        }
    }

    [Test]
    public void NativeList_BinarySearch_NotFound()
    {
        {
            var container = new NativeList<int>(1, Allocator.Temp);
            Assert.AreEqual(-1, container.BinarySearch(1));

            container.Add(1);

            Assert.AreEqual(0, container.BinarySearch(1));
            Assert.AreEqual(-1, container.BinarySearch(-2));
            Assert.AreEqual(-2, container.BinarySearch(2));
        }

        using (var container = new NativeList<int>(16, Allocator.Temp) { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 })
        {
            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~container.Length, container.BinarySearch(i + 16));
            }
        }

        using (var container = new NativeList<int>(8, Allocator.Temp) { 0, 2, 4, 6, 8, 10, 12, 14 })
        {
            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, container.BinarySearch(i * 2 + 1));
            }
        }
    }

#if !UNITY_DOTSRUNTIME
    [Test]
    public void NativeList_BinarySearch_NotFound_Reference_ArrayList()
    {
        {
            var reference = new ArrayList();
            var container = new NativeList<int>(1, Allocator.Temp);
            Assert.AreEqual(-1, reference.BinarySearch(1));
            Assert.AreEqual(-1, container.BinarySearch(1));

            reference.Add(1);
            container.Add(1);

            Assert.AreEqual(0, reference.BinarySearch(1));
            Assert.AreEqual(0, container.BinarySearch(1));

            Assert.AreEqual(-1, reference.BinarySearch(-2));
            Assert.AreEqual(-1, container.BinarySearch(-2));

            Assert.AreEqual(-2, reference.BinarySearch(2));
            Assert.AreEqual(-2, container.BinarySearch(2));
        }

        using (var container = new NativeList<int>(16, Allocator.Temp) { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 })
        {
            var reference = new ArrayList() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~reference.Count, reference.BinarySearch(i + 16));
                Assert.AreEqual(~container.Length, container.BinarySearch(i + 16));
            }
        }

        using (var container = new NativeList<int>(8, Allocator.Temp) { 0, 2, 4, 6, 8, 10, 12, 14 })
        {
            var reference = new ArrayList() { 0, 2, 4, 6, 8, 10, 12, 14 };

            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, reference.BinarySearch(i * 2 + 1));
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, container.BinarySearch(i * 2 + 1));
            }
        }
    }
#endif

    [Test]
    public void NativeList_GenericSortJob()
    {
        using (var container = new NativeList<int>(5, Allocator.Persistent))
        {
            for (var i = 0; i < 5; ++i)
            {
                container.Add(4 - i);
            }

            container.Sort();

            for (var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(i, container[i]);
            }
        }

        using (var container = new NativeList<int>(5, Allocator.Persistent))
        {
            for (var i = 0; i < 5; ++i)
            {
                container.Add(4 - i);
            }

            container.SortJob().Schedule().Complete();

            for (var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(i, container[i]);
            }
        }
    }

    [Test]
    public void NativeList_GenericSortJobCustomComparer()
    {
        using (var container = new NativeList<int>(5, Allocator.Persistent))
        {
            for (var i = 0; i < 5; ++i)
            {
                container.Add(i);
            }

            container.Sort(new DescendingComparer<int>());

            for (var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(4 - i, container[i]);
            }
        }

        using (var container = new NativeList<int>(5, Allocator.Persistent))
        {
            for (var i = 0; i < 5; ++i)
            {
                container.Add(i);
            }

            container.SortJob(new DescendingComparer<int>()).Schedule().Complete();

            for (var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(4 - i, container[i]);
            }
        }
    }

    [Test]
    public void UnsafeList_BinarySearch()
    {
        using (var container = new UnsafeList<int>(16, Allocator.Persistent) { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53 })
        {
            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(i, container.BinarySearch(container[i]));
            }
        }
    }

    [Test]
    public void UnsafeList_BinarySearch_NotFound()
    {
        {
            var container = new UnsafeList<int>(1, Allocator.Temp);
            Assert.AreEqual(-1, container.BinarySearch(1));

            container.Add(1);

            Assert.AreEqual(0, container.BinarySearch(1));
            Assert.AreEqual(-1, container.BinarySearch(-2));
            Assert.AreEqual(-2, container.BinarySearch(2));
        }

        using (var container = new UnsafeList<int>(16, Allocator.Temp) { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 })
        {
            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~container.Length, container.BinarySearch(i + 16));
            }
        }

        using (var container = new UnsafeList<int>(8, Allocator.Temp) { 0, 2, 4, 6, 8, 10, 12, 14 })
        {
            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, container.BinarySearch(i * 2 + 1));
            }
        }
    }

#if !UNITY_DOTSRUNTIME
    [Test]
    public void UnsafeList_BinarySearch_NotFound_Reference_ArrayList()
    {
        {
            var reference = new ArrayList();
            var container = new UnsafeList<int>(1, Allocator.Temp);
            Assert.AreEqual(-1, reference.BinarySearch(1));
            Assert.AreEqual(-1, container.BinarySearch(1));

            reference.Add(1);
            container.Add(1);

            Assert.AreEqual(0, reference.BinarySearch(1));
            Assert.AreEqual(0, container.BinarySearch(1));

            Assert.AreEqual(-1, reference.BinarySearch(-2));
            Assert.AreEqual(-1, container.BinarySearch(-2));

            Assert.AreEqual(-2, reference.BinarySearch(2));
            Assert.AreEqual(-2, container.BinarySearch(2));
        }

        using (var container = new UnsafeList<int>(16, Allocator.Temp) { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 })
        {
            var reference = new ArrayList() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~reference.Count, reference.BinarySearch(i + 16));
                Assert.AreEqual(~container.Length, container.BinarySearch(i + 16));
            }
        }

        using (var container = new UnsafeList<int>(8, Allocator.Temp) { 0, 2, 4, 6, 8, 10, 12, 14 })
        {
            var reference = new ArrayList() { 0, 2, 4, 6, 8, 10, 12, 14 };

            for (int i = 0, num = container.Length; i < num; ++i)
            {
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, reference.BinarySearch(i * 2 + 1));
                Assert.AreEqual(~(i + 1) /* ~index of first greatest value searched */, container.BinarySearch(i * 2 + 1));
            }
        }
    }
#endif

    [Test]
    public void UnsafeList_GenericSortJob()
    {
        using (var container = new UnsafeList<int>(5, Allocator.Persistent))
        {
            for (var i = 0; i < 5; ++i)
            {
                container.Add(4 - i);
            }

            container.Sort();

            for (var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(i, container[i]);
            }
        }

        using (var container = new UnsafeList<int>(5, Allocator.Persistent))
        {
            for (var i = 0; i < 5; ++i)
            {
                container.Add(4 - i);
            }

            container.SortJob().Schedule().Complete();

            for (var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(i, container[i]);
            }
        }
    }

    [Test]
    public void UnsafeList_GenericSortJobCustomComparer()
    {
        using (var container = new UnsafeList<int>(5, Allocator.Persistent))
        {
            for (var i = 0; i < 5; ++i)
            {
                container.Add(i);
            }

            container.Sort(new DescendingComparer<int>());

            for (var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(4 - i, container[i]);
            }
        }

        using (var container = new UnsafeList<int>(5, Allocator.Persistent))
        {
            for (var i = 0; i < 5; ++i)
            {
                container.Add(i);
            }

            container.SortJob(new DescendingComparer<int>()).Schedule().Complete();

            for (var i = 0; i < 5; ++i)
            {
                Assert.AreEqual(4 - i, container[i]);
            }
        }
    }


    [Test]
    public void FixedList32Bytes_GenericSort()
    {
        var container = new FixedList32Bytes<int>();

        for (var i = 0; i < 5; ++i)
        {
            container.Add(i);
        }

        container.Sort(new DescendingComparer<int>());

        for (var i = 0; i < 5; ++i)
        {
            Assert.AreEqual(4 - i, container[i]);
        }
    }


    [Test]
    public void FixedList64Bytes_GenericSort()
    {
        var container = new FixedList64Bytes<int>();

        for (var i = 0; i < 5; ++i)
        {
            container.Add(i);
        }

        container.Sort(new DescendingComparer<int>());

        for (var i = 0; i < 5; ++i)
        {
            Assert.AreEqual(4 - i, container[i]);
        }
    }


    [Test]
    public void FixedList128Bytes_GenericSort()
    {
        var container = new FixedList128Bytes<int>();

        for (var i = 0; i < 5; ++i)
        {
            container.Add(i);
        }

        container.Sort(new DescendingComparer<int>());

        for (var i = 0; i < 5; ++i)
        {
            Assert.AreEqual(4 - i, container[i]);
        }
    }


    [Test]
    public void FixedList512Bytes_GenericSort()
    {
        var container = new FixedList512Bytes<int>();

        for (var i = 0; i < 5; ++i)
        {
            container.Add(i);
        }

        container.Sort(new DescendingComparer<int>());

        for (var i = 0; i < 5; ++i)
        {
            Assert.AreEqual(4 - i, container[i]);
        }
    }


    [Test]
    public void FixedList4096Bytes_GenericSort()
    {
        var container = new FixedList4096Bytes<int>();

        for (var i = 0; i < 5; ++i)
        {
            container.Add(i);
        }

        container.Sort(new DescendingComparer<int>());

        for (var i = 0; i < 5; ++i)
        {
            Assert.AreEqual(4 - i, container[i]);
        }
    }

}
