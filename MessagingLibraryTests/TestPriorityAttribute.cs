using Xunit.Abstractions;
using Xunit.Sdk;

namespace MessagingLibraryTests;

public class TestPriorityAttribute(int priority) : Attribute
{
    // ReSharper disable once UnusedMember.Global
    public int Priority { get; } = priority;
}

// ReSharper disable once UnusedMember.Global Используется для вызова тестов в нужном порядке
public class PriorityOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        return testCases.OrderBy(GetPriority);
    }

    private static int GetPriority<TTestCase>(TTestCase testCase) where TTestCase : ITestCase
    {
        var priorityAttribute = testCase.TestMethod.Method
            .GetCustomAttributes(typeof(TestPriorityAttribute).AssemblyQualifiedName!)
            .FirstOrDefault();

        return priorityAttribute?.GetNamedArgument<int>("Priority") ?? 0;
    }
}