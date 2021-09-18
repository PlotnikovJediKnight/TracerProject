using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Xml;
using TracerProject;
using Newtonsoft.Json;

namespace TestsProject
{

    #region TestClasses
    class D
    {
        public ITracer tracer;
        public D(ITracer tr)
        {
            tracer = tr;
        }

        public void foo()
        {
            tracer.StartTrace();
            tracer.StopTrace();
        }

        public void methodD()
        {
            tracer.StartTrace();
            tracer.StopTrace();
        }
    }

    class C
    {
        public ITracer tracer;
        private D obj;
        public C(ITracer tr)
        {
            tracer = tr;
            obj = new D(tracer);
        }

        public void foo()
        {
            tracer.StartTrace();
            obj.foo();
            tracer.StopTrace();
        }

        public void methodC()
        {
            tracer.StartTrace();
            obj.methodD();
            tracer.StopTrace();
        }
    }

    class B
    {
        public ITracer tracer;
        private C obj;
        public B(ITracer tr)
        {
            tracer = tr;
            obj = new C(tracer);
        }

        public void foo()
        {
            tracer.StartTrace();
            obj.foo();
            tracer.StopTrace();
        }

        public void methodB()
        {
            tracer.StartTrace();
            obj.methodC();
            tracer.StopTrace();
        }
    }

    class A
    {
        public ITracer tracer;
        private B obj;
        public A(ITracer tr)
        {
            tracer = tr;
            obj = new B(tracer);
        }

        public void foo()
        {
            tracer.StartTrace();
            obj.foo();
            tracer.StopTrace();
        }

        public void methodA()
        {
            tracer.StartTrace();
            obj.methodB();
            tracer.StopTrace();
        }

        public void aReallyLongMethod()
        {
            tracer.StartTrace();
            Thread.Sleep(3200);
            tracer.StopTrace();
        }
    }

    class ImaginaryBinaryTree
    {
        public ITracer tracer;
        public ImaginaryBinaryTree(ITracer tr)
        {
            tracer = tr;
        }

        public void m15()
        {
            tracer.StartTrace();
            m10();
            m20();
            tracer.StopTrace();
        }

        public void m10()
        {
            tracer.StartTrace();
            m8();
            m12();
            tracer.StopTrace();
        }

        public void m8()
        {
            tracer.StartTrace();
            tracer.StopTrace();
        }

        public void m12()
        {
            tracer.StartTrace();
            tracer.StopTrace();
        }

        public void m20()
        {
            tracer.StartTrace();
            m17();
            m25();
            tracer.StopTrace();
        }

        public void m17()
        {
            tracer.StartTrace();
            tracer.StopTrace();
        }

        public void m25()
        {
            tracer.StartTrace();
            tracer.StopTrace();
        }
    }


    #endregion 

    public class TestFramework
    {
        delegate void test_method();
        private static ITracer tracer;

        [System.Diagnostics.DebuggerStepThrough]
        private static void AssertEqual<T, U>(T t, U u, string hint)
        {
            if (!t.Equals(u))
            {
                Console.Error.Write("Assertion failed: {0} != {1} ", t, u);
                if (hint.Length > 0)
                {
                    Console.Error.Write(" hint: " + hint);
                }
                throw new SystemException();
            }
        }

        [System.Diagnostics.DebuggerStepThrough]
        private static void Assert<T>(T t, string hint)
        {
            AssertEqual(t, true, hint);
        }

        static void foo(int time) {
            tracer.StartTrace();
            Thread.Sleep(time);
            tracer.StopTrace();
        }

        static void bar(int time) {
            tracer.StartTrace();
            if (time >= 30) Thread.Sleep(time - 30); 
            tracer.StopTrace();
        }

        static void baz(int time)
        {
            foo(time);
            bar(time);
        }

        [System.Diagnostics.DebuggerStepThrough]
        static void TestMultipleThreads()
        {
            tracer = new TimeTracer();

            Task task1 = new Task(()=>baz(0)); task1.Start();
            baz(0);
            Task task2 = new Task(()=>baz(0)); task2.Start();
            task1.Wait();
            task2.Wait();

            AssertEqual(tracer.GetTraceResult().Threads.Count, 3, "Number of threads is 3");
        }

        [System.Diagnostics.DebuggerStepThrough]
        static void TestThreadTime()
        {
            tracer = new TimeTracer();

            Task task1 = new Task(() => baz(200)); task1.Start(); task1.Wait();
            baz(450);
            Task task2 = new Task(() => baz(317)); task2.Start(); task2.Wait();

            long time1 = tracer.GetTraceResult().Threads[0].getLongTime();
            Assert(time1 >= 365 && time1 <= 375, "Task1 is expected to last 370ms");

            long time2 = tracer.GetTraceResult().Threads[1].getLongTime();
            Assert(time2 >= 865 && time2 <= 875, "Main Thread Task is expected to last 870ms");

            long time3 = tracer.GetTraceResult().Threads[2].getLongTime();
            Assert(time3 >= 600 && time3 <= 610, "Main Thread Task is expected to last 604ms");
        }

        [System.Diagnostics.DebuggerStepThrough]
        static void TestClassNames()
        {
            tracer = new TimeTracer();
            A a = new A(tracer);

            a.foo();
            TraceResult result = tracer.GetTraceResult();
            MethodInfo m1 = result.Threads[0].Methods[0];
            MethodInfo m2 = m1.Methods[0];
            MethodInfo m3 = m2.Methods[0];
            MethodInfo m4 = m3.Methods[0];

            AssertEqual(m1.Class, "TestsProject.A", "Class A is expected");
            AssertEqual(m2.Class, "TestsProject.B", "Class B is expected");
            AssertEqual(m3.Class, "TestsProject.C", "Class C is expected");
            AssertEqual(m4.Class, "TestsProject.D", "Class D is expected");
        }

        [System.Diagnostics.DebuggerStepThrough]
        static void TestMethodNames()
        {
            tracer = new TimeTracer();
            A a = new A(tracer);

            a.methodA();
            TraceResult result = tracer.GetTraceResult();
            MethodInfo m1 = result.Threads[0].Methods[0];
            MethodInfo m2 = m1.Methods[0];
            MethodInfo m3 = m2.Methods[0];
            MethodInfo m4 = m3.Methods[0];

            AssertEqual(m1.Name, "methodA", "methodA is expected");
            AssertEqual(m2.Name, "methodB", "methodB is expected");
            AssertEqual(m3.Name, "methodC", "methodC is expected");
            AssertEqual(m4.Name, "methodD", "methodD is expected");
        }

        [System.Diagnostics.DebuggerStepThrough]
        static void TestBinaryTreeLikeStructure()
        {
            /*                       15
             *               10              20
             *              8  12          17  25 
             */
            tracer = new TimeTracer();
            ImaginaryBinaryTree obj = new ImaginaryBinaryTree(tracer);
            obj.m15();

            TraceResult tr = obj.tracer.GetTraceResult();
            MethodInfo i15 = tr.Threads[0].Methods[0];
            AssertEqual(i15.Name, "m15", "Method m15 is expected");

            MethodInfo i10 = i15.Methods[0];
            AssertEqual(i10.Name, "m10", "Method m10 is expected");

            MethodInfo i8 = i10.Methods[0];
            AssertEqual(i8.Name, "m8", "Method m8 is expected");

            MethodInfo i12 = i10.Methods[1];
            AssertEqual(i12.Name, "m12", "Method m12 is expected");

            MethodInfo i20 = i15.Methods[1];
            AssertEqual(i20.Name, "m20", "Method m20 is expected");

            MethodInfo i17 = i20.Methods[0];
            AssertEqual(i17.Name, "m17", "Method m17 is expected");

            MethodInfo i25 = i20.Methods[1];
            AssertEqual(i25.Name, "m25", "Method m25 is expected");
        }

        [System.Diagnostics.DebuggerStepThrough]
        static void TestManyThreadsAtOnce()
        {
            tracer = new TimeTracer();
            A a = new A(tracer);
            Task[] tasks = new Task[10];
            for (int i = 0; i < 10; ++i)
            {
                tasks[i] = new Task(a.aReallyLongMethod);
                tasks[i].Start();
            }
            Task.WaitAll(tasks);

            TraceResult tr = a.tracer.GetTraceResult();
            AssertEqual(tr.Threads.Count, 10, "10 threads are expected");
            long timeElapsed = tr.Threads[0].getLongTime();
            Assert(timeElapsed >= 3190 && timeElapsed <= 3210, "10 threads are expected to have run for 3200ms");
        }

        public static void Main(string[] args)
        {
            test_method testDelegate;
            TestRunner<test_method> r = new TestRunner<test_method>();

            testDelegate = TestMultipleThreads;
            r.RunTest(testDelegate, "MultipleThreadsTest");

            testDelegate = TestThreadTime;
            r.RunTest(testDelegate, "TestThreadTime");

            testDelegate = TestClassNames;
            r.RunTest(testDelegate, "TestClassNames");

            testDelegate = TestMethodNames;
            r.RunTest(testDelegate, "TestMethodNames");

            testDelegate = TestBinaryTreeLikeStructure;
            r.RunTest(testDelegate, "TestBinaryTreeLikeStructure");

            testDelegate = TestManyThreadsAtOnce;
            r.RunTest(testDelegate, "TestManyThreadsAtOnce");

            Console.ReadLine();
        }
    }

    class TestRunner<Z> where Z : Delegate
    {
        private long failCount = 0;

        public void RunTest(Z func, string testName)
        {
            try
            {
                func.DynamicInvoke();
                Console.Error.WriteLine(testName + " OK");
            }
            catch (SystemException e)
            {
                ++failCount;
                Console.Error.WriteLine(testName + " failed: " + e.Message);
            }
            catch { }
        }

        ~TestRunner()
        {
            Console.Error.WriteLine("{0} tests failed. Terminate", failCount);
        }
    }
}
