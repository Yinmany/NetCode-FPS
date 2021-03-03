using AOT;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;

namespace MyGameLib.NetCode.Tests
{
    [TestFixture]
    public class PortableFunctionPointerTest
    {
        delegate void TestDelegate(int i);

        delegate void TestStrDelegate(FixedString32 name);

        struct State
        {
            public PortableFunctionPointer<TestDelegate> Test;
            public PortableFunctionPointer<TestStrDelegate> TestStr;
        }

        [Test]
        public void IntAndStrInvokeTest()
        {
            State state = new State
            {
                Test = new PortableFunctionPointer<TestDelegate>(OnTest),
                TestStr = new PortableFunctionPointer<TestStrDelegate>(OnTestStr)
            };

            state.Test.Ptr.Invoke(1);
            state.TestStr.Ptr.Invoke("123");
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(TestDelegate))]
        private static void OnTest(int i)
        {
            Assert.AreEqual(i, 1);
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(TestStrDelegate))]
        private static void OnTestStr(FixedString32 s)
        {
            Assert.AreEqual(s, "123");
        }
    }
}