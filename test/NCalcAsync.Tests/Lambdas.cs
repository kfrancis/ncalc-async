using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NCalcAsync.Tests
{
    [TestClass]
    public class Lambdas
    {
        private class Context
        {
            public int FieldA { get; set; }
            public string FieldB { get; set; }
            public decimal FieldC { get; set; }
            public decimal? FieldD { get; set; }
            public int? FieldE { get; set; }

            public int Test(int a, int b)
            {
                return a + b;
            }

            public string Test(string a, string b)
            {
                return a + b;
            }

            public int Test(int a, int b, int c)
            {
                return a + b + c;
            }

            public double Test(double a, double b, double c)
            {
                return a + b + c;
            }

            public string Sum(string msg, params int[] numbers)
            {
                int total = 0;
                foreach (var num in numbers)
                {
                    total += num;
                }
                return msg + total;
            }

            public int Sum(params int[] numbers)
            {
                int total = 0;
                foreach (var num in numbers)
                {
                    total += num;
                }
                return total;
            }

            public int Sum(TestObject1 obj1, TestObject2 obj2)
            {
                return obj1.Count1 + obj2.Count2;
            }

            public int Sum(TestObject2 obj1, TestObject1 obj2)
            {
                return obj1.Count2 + obj2.Count1;
            }

            public int Sum(TestObject1 obj1, TestObject1 obj2)
            {
                return obj1.Count1 + obj2.Count1;
            }

            public int Sum(TestObject2 obj1, TestObject2 obj2)
            {
                return obj1.Count2 + obj2.Count2;
            }

            public class TestObject1
            {
                public int Count1 { get; set; }
            }

            public class TestObject2
            {
                public int Count2 { get; set; }
            }


            public TestObject1 CreateTestObject1(int count)
            {
                return new TestObject1() { Count1 = count };
            }

            public TestObject2 CreateTestObject2(int count)
            {
                return new TestObject2() { Count2 = count };
            }
        }

        private class SubContext : Context
        {
            public int Multiply(int a, int b)
            {
                return a * b;
            }

            public new int Test(int a, int b)
            {
                return base.Test(a, b) / 2;
            }

            public int Test(int a, int b, int c, int d)
            {
                return a + b + c + d;
            }

            public int Sum(TestObject1 obj1, TestObject2 obj2, TestObject2 obj3)
            {
                return obj1.Count1 + obj2.Count2 + obj3.Count2 + 100;
            }
        }

        [DataTestMethod]
        [DataRow("1+2", 3)]
        [DataRow("1-2", -1)]
        [DataRow("2*2", 4)]
        [DataRow("10/2", 5)]
        [DataRow("7%2", 1)]
        public async Task ShouldHandleIntegers(string input, int expected)
        {
            var expression = new Expression(input);
            var sut = await expression.ToLambdaAsync<int>();

            Assert.AreEqual(sut(), expected);
        }

        [TestMethod]
        public async Task ShouldHandleParameters()
        {
            var expression = new Expression("[FieldA] > 5 && [FieldB] = 'test'");
            var sut = await expression.ToLambdaAsync<Context, bool>();
            var context = new Context { FieldA = 7, FieldB = "test" };

            Assert.IsTrue(sut(context));
        }

        [TestMethod]
        public async Task ShouldHandleOverloadingSameParamCount()
        {
            var expression = new Expression("Test('Hello', ' world!')");
            var sut = await expression .ToLambdaAsync<Context, string>();
            var context = new Context();

            Assert.AreEqual("Hello world!", sut(context));
        }

        [TestMethod]
        public async Task ShouldHandleOverloadingDifferentParamCount()
        {
            var expression = new Expression("Test(Test(1, 2), 3, 4)");
            var sut = await expression.ToLambdaAsync<Context, int>();
            var context = new Context();

            Assert.AreEqual(10, sut(context));
        }

        [TestMethod]
        public async Task ShouldHandleOverloadingObjectParameters()
        {
            var expression = new Expression("Sum(CreateTestObject1(2), CreateTestObject2(2)) + Sum(CreateTestObject2(1), CreateTestObject1(5))");
            var sut = await expression.ToLambdaAsync<Context, int>();
            var context = new Context();

            Assert.AreEqual(10, sut(context));
        }


        [TestMethod]
        public async Task ShouldHandleParamsKeyword()
        {
            var expression = new Expression("Sum(Test(1,1),2)");
            var sut = await expression.ToLambdaAsync<Context, int>();
            var context = new Context();

            Assert.AreEqual(4, sut(context));
        }

        [TestMethod]
        public async Task ShouldHandleMixedParamsKeyword()
        {
            var expression = new Expression("Sum('Your total is: ', Test(1,1), 2, 3)");
            var sut = await expression.ToLambdaAsync<Context, string>();
            var context = new Context();

            Assert.AreEqual("Your total is: 7", sut(context));
        }

        [TestMethod]
        public async Task ShouldHandleCustomFunctions()
        {
            var expression = new Expression("Test(Test(1, 2), 3)");
            var sut = await expression.ToLambdaAsync<Context, int>();
            var context = new Context();

            Assert.AreEqual(sut(context), 6);
        }

        [TestMethod]
        public async Task ShouldHandleContextInheritance()
        {
            var lambda1 = await new Expression("Multiply(5, 2)").ToLambdaAsync<SubContext, int>();
            var lambda2 = await new Expression("Test(5, 5)").ToLambdaAsync<SubContext, int>();
            var lambda3 = await new Expression("Test(1,2,3,4)").ToLambdaAsync<SubContext, int>();
            var lambda4 = await new Expression("Sum(CreateTestObject1(100), CreateTestObject2(100), CreateTestObject2(100))").ToLambdaAsync<SubContext, int>();

            var context = new SubContext();
            Assert.AreEqual(10, lambda1(context));
            Assert.AreEqual(5, lambda2(context));
            Assert.AreEqual(10, lambda3(context));
            Assert.AreEqual(400, lambda4(context));
        }

        [DataTestMethod]
        [DataRow("Test(1, 1, 1)")]
        [DataRow("Test(1.0, 1.0, 1.0)")]
        [DataRow("Test(1.0, 1, 1.0)")]
        public async Task ShouldHandleImplicitConversion(string input)
        {
            var lambda = await new Expression(input).ToLambdaAsync<Context, int>();

            var context = new Context();
            Assert.AreEqual(3, lambda(context));
        }

        [TestMethod]
        public async Task MissingMethod()
        {
            var expression = new Expression("MissingMethod(1)");
            try
            {
                var sut = await expression.ToLambdaAsync<Context, int>();
            }
            catch (System.MissingMethodException ex)
            {

                System.Diagnostics.Debug.Write(ex);
                Assert.IsTrue(true);
                return;
            }
            Assert.IsTrue(false);

        }

        [TestMethod]
        public async Task ShouldHandleTernaryOperator()
        {
            var expression = new Expression("Test(1, 2) = 3 ? 1 : 2");
            var sut = await expression.ToLambdaAsync<Context, int>();
            var context = new Context();

            Assert.AreEqual(sut(context), 1);
        }

        [TestMethod]
        public async Task Issue1()
        {
            var expr = new Expression("2 + 2 - a - b - x");

            decimal x = 5m;
            decimal a = 6m;
            decimal b = 7m;

            expr.Parameters["x"] = x;
            expr.Parameters["a"] = a;
            expr.Parameters["b"] = b;

            var f = await expr.ToLambdaAsync<float>(); // Here it throws System.ArgumentNullException. Parameter name: expression
            Assert.AreEqual(f(), -14);
        }

        [DataTestMethod]
        [DataRow("if(true, true, false)")]
        [DataRow("in(3, 1, 2, 3, 4)")]
        public async Task ShouldHandleBuiltInFunctions(string input)
        {
            var expression = new Expression(input);
            var sut = await expression.ToLambdaAsync<bool>();
            Assert.IsTrue(sut());
        }

        [DataTestMethod]
        [DataRow("[FieldA] > [FieldC]", true)]
        [DataRow("[FieldC] > 1.34", true)]
        [DataRow("[FieldC] > (1.34 * 2) % 3", false)]
        [DataRow("[FieldE] = 2", true)]
        [DataRow("[FieldD] > 0", false)]
        public async Task ShouldHandleDataConversions(string input, bool expected)
        {
            var expression = new Expression(input);
            var sut = await expression.ToLambdaAsync<Context, bool>();
            var context = new Context { FieldA = 7, FieldB = "test", FieldC = 2.4m, FieldE = 2 };

            Assert.AreEqual(expected, sut(context));
        }

        [DataTestMethod]
        [DataRow("Min(3,2)", 2)]
        [DataRow("Min(3.2,6.3)", 3.2)]
        [DataRow("Max(2.6,9.6)", 9.6)]
        [DataRow("Max(9,6)", 9.0)]
        [DataRow("Pow(5,2)", 25)]
        public async Task ShouldHandleNumericBuiltInFunctions(string input, double expected)
        {
            var expression = new Expression(input);
            var sut = await expression.ToLambdaAsync<object>();
            Assert.AreEqual(expected, sut());
        }

        [DataTestMethod]
        [DataRow("if(true, 1, 0.0)", 1.0)]
        [DataRow("if(true, 1.0, 0)", 1.0)]
        [DataRow("if(true, 1.0, 0.0)", 1.0)]
        public async Task ShouldHandleFloatIfFunction(string input, double expected)
        {
            var expression = new Expression(input);
            var sut = await expression.ToLambdaAsync<object>();
            Assert.AreEqual(expected, sut());
        }

        [DataTestMethod]
        [DataRow("if(true, 1, 0)", 1)]
        public async Task ShouldHandleIntIfFunction(string input, int expected)
        {
            var expression = new Expression(input);
            var sut = await expression.ToLambdaAsync<object>();
            Assert.AreEqual(expected, sut());
        }

        [DataTestMethod]
        [DataRow("if(true, 'a', 'b')", "a")]
        public async Task ShouldHandleStringIfFunction(string input, string expected)
        {
            var expression = new Expression(input);
            var sut = await expression .ToLambdaAsync<object>();
            Assert.AreEqual(expected, sut());
        }

        [TestMethod]
        public async Task ShouldAllowValueTypeContexts()
        {
            // Arrange
            const decimal expected = 6.908m;
            var expression = new Expression("Foo * 3.14");
            var sut = await expression.ToLambdaAsync<FooStruct, decimal>();
            var context = new FooStruct();

            // Act
            var actual = sut(context);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        // https://github.com/sklose/NCalc2/issues/54
        [TestMethod]
        public async Task Issue54()
        {
            // Arrange
            const long expected = 9999999999L;
            var expression = $"if(true, {expected}, 0)";
            var e = new Expression(expression);
            var context = new object();

            var lambda = await e.ToLambdaAsync<object, long>();

            // Act
            var actual = lambda(context);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        internal struct FooStruct
        {
            public double Foo => 2.2;
        }
    }
}
