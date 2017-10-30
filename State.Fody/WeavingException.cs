using System;

public enum EWeavingError
{
    InvalidPropertyType,
    InvalidPropertySetter,
    InstancePropertyWithStaticMethod,
    InstanceFieldWithStaticMethod
}

public class WeavingException : Exception
{
    public EWeavingError Error { get; }

    public WeavingException(EWeavingError error, string message)
        : base(message)
    {
        Error = error;
    }
}