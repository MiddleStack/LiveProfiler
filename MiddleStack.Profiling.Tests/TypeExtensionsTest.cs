using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace MiddleStack.Profiling.Tests
{
    [TestFixture]
    public class TypeExtensionsTest
    {
        [Test]
        public void TypeExtensions_ToLiveProfilerName_NullType_Throws()
        {
            Action thrower = () => TypeExtensions.ToLiveProfilerName(null);

            thrower.ShouldThrow<ArgumentNullException>().Which.ParamName.Should().Be("type");
        }

        [Test]
        public void TypeExtensions_ToLiveProfilerName_NonGenericType_ReturnsFullyQualifiedName()
        {
            typeof(string).ToLiveProfilerName().Should().Be("System.String");
        }

        [Test]
        public void TypeExtensions_ToLiveProfilerName_NonGenericTypeArray_ReturnsFullyQualifiedName()
        {
            typeof(string[]).ToLiveProfilerName().Should().Be("System.String[]");
        }

        [Test]
        public void TypeExtensions_ToLiveProfilerName_GenericType_ReturnsFullyQualifiedNameForRootTypeAndShortNamesForGenericParameters()
        {
            typeof(IDictionary<string,int>).ToLiveProfilerName().Should().Be("System.Collections.Generic.IDictionary<String,Int32>");
        }

        [Test]
        public void TypeExtensions_ToLiveProfilerName_GenericTypeDefinitionWithTwoParameters_ReturnsFullyQualifiedNameForRootTypeAndShortNamesForGenericParameters()
        {
            typeof(IDictionary<,>).ToLiveProfilerName().Should().Be("System.Collections.Generic.IDictionary<,>");
        }

        [Test]
        public void TypeExtensions_ToLiveProfilerName_GenericTypeDefinitionWithOneParameter_ReturnsFullyQualifiedNameForRootTypeAndShortNamesForGenericParameters()
        {
            typeof(IList<>).ToLiveProfilerName().Should().Be("System.Collections.Generic.IList<>");
        }

        [Test]
        public void TypeExtensions_ToLiveProfilerName_NestedGenericType_ReturnsFullyQualifiedNameForRootTypeAndShortNamesForGenericParameters()
        {
            typeof(IDictionary<string,IList<int>>).ToLiveProfilerName().Should().Be("System.Collections.Generic.IDictionary<String,IList<Int32>>");
        }

        [Test]
        public void TypeExtensions_ToLiveProfilerName_GenericTypeArray_ReturnsFullyQualifiedNameForRootTypeAndShortNamesForGenericParameters()
        {
            typeof(IDictionary<string,int>[]).ToLiveProfilerName().Should().Be("System.Collections.Generic.IDictionary<String,Int32>[]");
        }
    }
}
