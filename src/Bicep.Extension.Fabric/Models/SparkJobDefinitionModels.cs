using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

[ResourceType("SparkJobDefinition")]
public class SparkJobDefinition : FabricItem
{
    [TypeProperty("OneLake path to the Spark Job Definition root directory", ObjectTypePropertyFlags.ReadOnly)]
    public string? OneLakeRootPath { get; set; }
}
