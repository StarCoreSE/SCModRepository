using VRage.Collections;
using VRage.Library.Threading;

namespace DefenseShields.Support
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Sandbox.Game.Entities;
    using VRageMath;

    internal static class ConcurrentQueueExtensions
    {
        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            T item;
            while (queue.TryDequeue(out item)) { }
        }
    }

    class FiniteFifoQueueSet<T1, T2>
    {
        private readonly T1[] _nodes;
        private readonly Dictionary<T1, T2> _backingDict;
        private int _nextSlotToEvict;

        public FiniteFifoQueueSet(int size)
        {
            _nodes = new T1[size];
            _backingDict = new Dictionary<T1, T2>(size + 1);
            _nextSlotToEvict = 0;
        }

        public void Enqueue(T1 key, T2 value)
        {
            try
            {
                _backingDict.Remove(_nodes[_nextSlotToEvict]);
                _nodes[_nextSlotToEvict] = key;
                _backingDict.Add(key, value);

                _nextSlotToEvict++;
                if (_nextSlotToEvict >= _nodes.Length) _nextSlotToEvict = 0;
            }
            catch (Exception ex) { Log.Line($"Exception in Enqueue: {ex}"); }
        }

        public bool Contains(T1 value)
        {
            return _backingDict.ContainsKey(value);
        }

        public bool TryGet(T1 value, out T2 hostileEnt)
        {
            return _backingDict.TryGetValue(value, out hostileEnt);
        }
    }

    public class DsUniqueListFastRemove<T>
    {
        private List<T> _list = new List<T>();
        private Dictionary<T, int> _dictionary = new Dictionary<T, int>();
        private int _index;

        /// <summary>O(1)</summary>
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        /// <summary>O(1)</summary>
        public T this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        /// <summary>O(1)</summary>
        public bool Add(T item)
        {
            if (_dictionary.ContainsKey(item))
                return false;
            _dictionary.Add(item, _index);
            _list.Add(item);
            _index++;
            return true;
        }

        /// <summary>O(1)</summary>
        public bool Remove(T item)
        {
            if (!_dictionary.ContainsKey(item)) return false;

            var oldIndex = _dictionary[item];
            _dictionary.Remove(item);
            if (_index != oldIndex)
            {
                _list[oldIndex - 1] = _list[_index - 1];
                _list.RemoveAt(_index - 1);
            }
            else _list.RemoveAt(_index - 1);

            _index--;

            return true;
        }

        public void Clear()
        {
            _list.Clear();
            _dictionary.Clear();
        }

        /// <summary>O(1)</summary>
        public bool Contains(T item)
        {
            return _dictionary.ContainsKey(item);
        }

        public UniqueListReader<T> Items
        {
            get
            {
                return new UniqueListReader<T>();
            }
        }

        public ListReader<T> ItemList
        {
            get
            {
                return new ListReader<T>(_list);
            }
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

    public class DsUniqueList<T>
    {
        private List<T> _list = new List<T>();
        private HashSet<T> _hashSet = new HashSet<T>();

        /// <summary>O(1)</summary>
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        /// <summary>O(1)</summary>
        public T this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        /// <summary>O(1)</summary>
        public bool Add(T item)
        {
            if (!_hashSet.Add(item))
                return false;
            _list.Add(item);
            return true;
        }

        /// <summary>O(n)</summary>
        public bool Insert(int index, T item)
        {
            if (_hashSet.Add(item))
            {
                _list.Insert(index, item);
                return true;
            }
            _list.Remove(item);
            _list.Insert(index, item);
            return false;
        }

        /// <summary>O(n)</summary>
        public bool Remove(T item)
        {
            if (!_hashSet.Remove(item))
                return false;
            _list.Remove(item);
            return true;
        }

        public void Clear()
        {
            _list.Clear();
            _hashSet.Clear();
        }

        /// <summary>O(1)</summary>
        public bool Contains(T item)
        {
            return _hashSet.Contains(item);
        }

        public UniqueListReader<T> Items
        {
            get
            {
                return new UniqueListReader<T>();
            }
        }

        public ListReader<T> ItemList
        {
            get
            {
                return new ListReader<T>(_list);
            }
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

    public class DsConcurrentUniqueList<T>
    {
        private List<T> _list = new List<T>();
        private HashSet<T> _hashSet = new HashSet<T>();
        private SpinLockRef _lock = new SpinLockRef();

        /// <summary>O(1)</summary>
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        /// <summary>O(1)</summary>
        public T this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        /// <summary>O(1)</summary>
        public bool Add(T item)
        {
            using (_lock.Acquire())
            {
                if (!_hashSet.Add(item))
                    return false;
                _list.Add(item);
                return true;
            }
        }

        /// <summary>O(n)</summary>
        public bool Insert(int index, T item)
        {
            using (_lock.Acquire())
            {
                if (_hashSet.Add(item))
                {
                    _list.Insert(index, item);
                    return true;
                }
                _list.Remove(item);
                _list.Insert(index, item);
                return false;
            }
        }

        /// <summary>O(n)</summary>
        public bool Remove(T item)
        {
            using (_lock.Acquire())
            {
                if (!_hashSet.Remove(item))
                    return false;
                _list.Remove(item);
                return true;
            }
        }

        public void Clear()
        {
            _list.Clear();
            _hashSet.Clear();
        }

        /// <summary>O(1)</summary>
        public bool Contains(T item)
        {
            return _hashSet.Contains(item);
        }

        public UniqueListReader<T> Items
        {
            get
            {
                return new UniqueListReader<T>();
            }
        }

        public ListReader<T> ItemList
        {
            get
            {
                return new ListReader<T>(_list);
            }
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

    internal class DSUtils
    {
        private double _last;
        public Stopwatch Sw { get; } = new Stopwatch();

        public void StopWatchReport(string message, float log)
        {
            Sw.Stop();
            var ticks = Sw.ElapsedTicks;
            var ns = 1000000000.0 * ticks / Stopwatch.Frequency;
            var ms = ns / 1000000.0;
            var s = ms / 1000;
            if (log <= -1) Log.Line($"{message} ms:{(float)ms} last-ms:{(float)_last} s:{(int)s}");
            else
            {
                if (ms >= log) Log.Line($"{message} ms:{(float)ms} last-ms:{(float)_last} s:{(int)s}");
            }
            _last = ms;
            Sw.Reset();
        }
        /*
        internal static BoundingSphereD CreateFromPointsList(List<Vector3D> points)
        {
            Vector3D current;
            Vector3D Vector3D_1 = current = points[0];
            Vector3D Vector3D_2 = current;
            Vector3D Vector3D_3 = current;
            Vector3D Vector3D_4 = current;
            Vector3D Vector3D_5 = current;
            Vector3D Vector3D_6 = current;
            foreach (Vector3D Vector3D_7 in points)
            {
                if (Vector3D_7.X < Vector3D_6.X)
                    Vector3D_6 = Vector3D_7;
                if (Vector3D_7.X > Vector3D_5.X)
                    Vector3D_5 = Vector3D_7;
                if (Vector3D_7.Y < Vector3D_4.Y)
                    Vector3D_4 = Vector3D_7;
                if (Vector3D_7.Y > Vector3D_3.Y)
                    Vector3D_3 = Vector3D_7;
                if (Vector3D_7.Z < Vector3D_2.Z)
                    Vector3D_2 = Vector3D_7;
                if (Vector3D_7.Z > Vector3D_1.Z)
                    Vector3D_1 = Vector3D_7;
            }
            double result1;
            Vector3D.Distance(ref Vector3D_5, ref Vector3D_6, out result1);
            double result2;
            Vector3D.Distance(ref Vector3D_3, ref Vector3D_4, out result2);
            double result3;
            Vector3D.Distance(ref Vector3D_1, ref Vector3D_2, out result3);
            Vector3D result4;
            double num1;
            if (result1 > result2)
            {
                if (result1 > result3)
                {
                    Vector3D.Lerp(ref Vector3D_5, ref Vector3D_6, 0.5f, out result4);
                    num1 = result1 * 0.5f;
                }
                else
                {
                    Vector3D.Lerp(ref Vector3D_1, ref Vector3D_2, 0.5f, out result4);
                    num1 = result3 * 0.5f;
                }
            }
            else if (result2 > result3)
            {
                Vector3D.Lerp(ref Vector3D_3, ref Vector3D_4, 0.5f, out result4);
                num1 = result2 * 0.5f;
            }
            else
            {
                Vector3D.Lerp(ref Vector3D_1, ref Vector3D_2, 0.5f, out result4);
                num1 = result3 * 0.5f;
            }
            foreach (Vector3D Vector3D_7 in points)
            {
                Vector3D Vector3D_8;
                Vector3D_8.X = Vector3D_7.X - result4.X;
                Vector3D_8.Y = Vector3D_7.Y - result4.Y;
                Vector3D_8.Z = Vector3D_7.Z - result4.Z;
                double num2 = Vector3D_8.Length();
                if (num2 > num1)
                {
                    num1 = (num1 + num2) * 0.5;
                    result4 += (1.0 - (num1 / num2)) * Vector3D_8;
                }
            }
            BoundingSphereD boundingSphereD;
            boundingSphereD.Center = result4;
            boundingSphereD.Radius = num1;
            return boundingSphereD;
        }
    */
    }

    public class RunningAverageCalculator
    {
        private readonly int _maxEntries;
        private readonly Queue<float> _leftValues = new Queue<float>();
        private readonly Queue<float> _rightValues = new Queue<float>();
        private float _leftSum = 0;
        private float _rightSum = 0;
        private float _minValue = float.MaxValue;
        private float _maxValue = float.MinValue;

        public RunningAverageCalculator(int maxEntries)
        {
            _maxEntries = maxEntries;
        }

        public void UpdateAverage(float value1, float value2, bool rapidExpunge = false)
        {
            _minValue = Math.Min(Math.Min(value1, value2), _minValue);
            _maxValue = Math.Max(Math.Max(value1, value2), _maxValue);

            _leftSum += value1;
            _rightSum += value2;
            _leftValues.Enqueue(value1);
            _rightValues.Enqueue(value2);

            var max = !rapidExpunge ? _maxEntries : _maxEntries / 20;
            if (_leftValues.Count > max)
            {
                float leftDequeued = _leftValues.Dequeue();
                float rightDequeued = _rightValues.Dequeue();
                _leftSum -= leftDequeued;
                _rightSum -= rightDequeued;

                // If min or max was in the dequeued values, recalculate them
                if (_minValue == leftDequeued || _minValue == rightDequeued || _maxValue == leftDequeued || _maxValue == rightDequeued)
                {
                    _minValue = Math.Min(Math.Min(_leftValues.Min(), _rightValues.Min()), _minValue);
                    _maxValue = Math.Max(Math.Max(_leftValues.Max(), _rightValues.Max()), _maxValue);
                }
            }
        }

        public float GetDifferenceRatio()
        {
            if (_leftValues.Count == 0 && _rightValues.Count == 0)
                return 0;

            float totalSum = _leftSum + _rightSum;

            if (totalSum == 0)
                return 0;

            float leftContribution = _leftSum / totalSum;

            // If only left values exist, leftContribution will be 1
            // So, diffRatio will be 2 * 1 - 1 = 1
            // If only right values exist, leftContribution will be 0
            // So, diffRatio will be 2 * 0 - 1 = -1
            // If left and right values are the same, leftContribution will be 0.5
            // So, diffRatio will be 2 * 0.5 - 1 = 0
            float diffRatio = 2 * leftContribution - 1;

            return diffRatio;
        }

        public float GetAverage()
        {
            if (_leftValues.Count == 0)
                return 0;

            float averageLeft = _leftSum / _leftValues.Count;
            float averageRight = _rightSum / _rightValues.Count;

            return (averageLeft + averageRight) / 2;
        }

        public void Clear()
        {
            _leftValues.Clear();
            _rightValues.Clear();
            _leftSum = 0;
            _rightSum = 0;
            _minValue = 0;
            _maxValue = 0;
        }
    }

    internal class RunningAverage
    {
        private readonly int _size;
        private readonly int[] _values;
        private int _valuesIndex;
        private int _valueCount;
        private int _sum;

        internal RunningAverage(int size)
        {
            _size = Math.Max(size, 1);
            _values = new int[_size];
        }

        internal int Add(int newValue)
        {
            // calculate new value to add to sum by subtracting the 
            // value that is replaced from the new value; 
            var temp = newValue - _values[_valuesIndex];
            _values[_valuesIndex] = newValue;
            _sum += temp;

            _valuesIndex++;
            _valuesIndex %= _size;

            if (_valueCount < _size)
                _valueCount++;

            return _sum / _valueCount;
        }

        internal void Clear()
        {
            for (int i = 0; i < _values.Length; i++)
                _values[i] = 0;
        }
    }
}
