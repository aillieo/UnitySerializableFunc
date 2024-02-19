// -----------------------------------------------------------------------
// <copyright file="TestFuncs.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc.Sample
{
    using UnityEngine;

    public class TestFuncs : MonoBehaviour
    {
        public int GetRandomNumber()
        {
            var n = Random.Range(0, 100);
            UnityEngine.Debug.Log($"n={n}");
            return n;
        }

        public int Double(int a)
        {
            UnityEngine.Debug.Log($"a={a}");
            return a + a;
        }

        public int SumOf2(int a, int b)
        {
            UnityEngine.Debug.Log($"a={a},b={b}");
            return a + b;
        }

        public int SumOf3(int a, int b, int c)
        {
            UnityEngine.Debug.Log($"a={a},b={b},c={c}");
            return a + b + c;
        }

        public int SumOf4(int a, int b, int c, int d)
        {
            UnityEngine.Debug.Log($"a={a},b={b},c={c},d={d}");
            return a + b + c + d;
        }

        public UnityFunc<int> unityFunc0;
        public UnityFunc<int, int> unityFunc1;
        public UnityFunc<int, int, int> unityFunc2;
        public UnityFunc<int, int, int, int> unityFunc3;
        public UnityFunc<int, int, int, int, int> unityFunc4;

        public UnityFunc<string, bool> unityFunc5;
        public UnityFunc<bool> unityFunc6;
        public UnityFunc<string> unityFunc7;

        [ContextMenu(nameof(InvokeAll))]
        public void InvokeAll()
        {
            this.unityFunc0?.Invoke();
            this.unityFunc1?.Invoke(-1);
            this.unityFunc2?.Invoke(-1, -1);
            this.unityFunc3?.Invoke(-1, -1, -1);
            this.unityFunc4?.Invoke(-1, -1, -1, -1);

            this.unityFunc5?.Invoke(null);
            this.unityFunc6?.Invoke();
            this.unityFunc7?.Invoke();
        }

        [ContextMenu(nameof(Invoke0))]
        public void Invoke0()
        {
            this.unityFunc0?.Invoke();
        }

        [ContextMenu(nameof(Invoke1))]
        public void Invoke1()
        {
            this.unityFunc1?.Invoke(-1);
        }

        [ContextMenu(nameof(Invoke2))]
        public void Invoke2()
        {
            this.unityFunc2?.Invoke(-1, -1);
        }

        [ContextMenu(nameof(Invoke3))]
        public void Invoke3()
        {
            this.unityFunc3?.Invoke(-1, -1, -1);
        }

        [ContextMenu(nameof(Invoke4))]
        public void Invoke4()
        {
            this.unityFunc4?.Invoke(-1, -1, -1, -1);
        }

        public int StringToInt(string v)
        {
            Debug.Log(v);
            return 0;
        }

        public int ObjToInt(UnityEngine.Object v)
        {
            Debug.Log(v);
            return 0;
        }
    }
}
