using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

using DocumentFormat.OpenXml.Drawing;

namespace BenchmarkTest;

[SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net90)]
public class Program
{
    static void Main()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<Program>();
    }

    private Random random;

    [Params(10, 100, 1000, 10000)]
    public int N;

    private Point[] pointArray;
    private List<Point> pointList;
    private int pointSize;
    private FieldInfo listBackingStoreFieldInfo;

    private struct Point
    {
        public double X;
        public double Y;
        public string Name;
    }

    [GlobalSetup]
    public void Setup()
    {
        random = new Random();

        pointSize = Marshal.SizeOf<Point>();

        var myPoint = new Point()
        {
            Name = "My Point",
            X = 1.0,
            Y = 2.0
        };

        pointArray = new Point[N];
        Array.Fill(pointArray, myPoint);
        pointList = [.. Enumerable.Repeat(myPoint, N)];

        listBackingStoreFieldInfo = typeof(List<Point>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
        if (listBackingStoreFieldInfo is null)
        {
            throw new InvalidOperationException();
        }
    }

    [Benchmark]
    public void ArrayLinqCopy()
    {
        var newArray = pointArray.ToArray();
    }
    [Benchmark]
    public void ArraySpanWrite()
    {
        var newArray = new Point[N];
        pointArray.AsSpan().CopyTo(newArray);
    }
    [Benchmark]
    public void ArrayCopy()
    {
        var newArray = new Point[N];
        Array.Copy(pointArray, newArray, N);
    }
    [Benchmark]
    public void ArrayCopyTo()
    {
        var newArray = new Point[N];
        pointArray.CopyTo(newArray, N);
    }
    [Benchmark]
    public unsafe void UnsafeArrayDuplicate()
    {
        var newArray = new Point[N];
        fixed (void* oldPtr = &pointArray[0])
        fixed (void* newPtr = &newArray[0])
        {
            Unsafe.CopyBlock(newPtr, oldPtr, (uint)(pointSize * N));
        }
    }

    [Benchmark]
    public void ListCopyBuiltIn()
    {
        List<Point> newList = [];
        newList.AddRange(collection: pointList);
    }
    [Benchmark]
    public void ListCopyGetRange()
    {
        var newList = pointList.GetRange(0, N);
    }
    [Benchmark]
    public void ListCopyReflectedSpanExtension()
    {
        List<Point> newList = [];
        var backingStore = (Point[])listBackingStoreFieldInfo.GetValue(pointList);
        newList.AddRange(source: backingStore.AsSpan());
    }
}
