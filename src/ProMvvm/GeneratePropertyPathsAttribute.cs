namespace ProMvvm;

/// <summary>
/// Generates a static <c>{TypeName}PropertyPaths</c> class containing reusable,
/// reflection-free descriptors for the accessible instance properties declared by the type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GeneratePropertyPathsAttribute : Attribute
{
}
