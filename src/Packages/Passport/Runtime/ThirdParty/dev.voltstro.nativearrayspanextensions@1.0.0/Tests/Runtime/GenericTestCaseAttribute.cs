using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

//FROM: https://stackoverflow.com/a/40619376
namespace VoltstroStudios.NativeArraySpanExtensions.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class GenericTestCaseAttribute : TestCaseAttribute, ITestBuilder
    {
        private readonly Type type;

        public GenericTestCaseAttribute(Type type, params object[] arguments) : base(arguments)
        {
            this.type = type;
        }

        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
        {
            if (method.IsGenericMethodDefinition && type != null)
            {
                IMethodInfo gm = method.MakeGenericMethod(type);
                return BuildFrom(gm, suite);
            }

            return BuildFrom(method, suite);
        }
    }
}