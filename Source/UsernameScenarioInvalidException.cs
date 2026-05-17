using System.Runtime.Serialization;

namespace Aeoquotes;

public enum NameScenario
{
    NegativeCount,
    ZeroCount,
    MultipleUsers
}

public enum NameType
{
    Nickname,
    DisplayName,
    Username
}

public class NameScenarioInvalidException : Exception, ISerializable
{



    public NameScenario Scenario { get; init; }

    public NameType Name { get; init; }

    public NameScenarioInvalidException() : base() {}

    public NameScenarioInvalidException(string? message) : base(message) {}

    public NameScenarioInvalidException(string? message, Exception? innerException) : base(message, innerException) {}

    public NameScenarioInvalidException(string? message, NameScenario scenario, NameType type) : this(message)
    {
        Scenario = scenario;
        Name = type;
    }
}