namespace ShakeIt.DuplicateFinder;

public class ShakeItSettings
{
    public IList<ShakeItProfile> Profiles { get; set; } = new List<ShakeItProfile>();
}

public class ShakeItProfile
{
    public Guid ProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public IList<EffectsContainerBase> EffectsContainers { get; set; } = new List<EffectsContainerBase>();
}

public class EffectsContainerBase
{
    public Guid ContainerId { get; set; }
    public string? ContainerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public IList<EffectsContainerBase> EffectsContainers { get; set; } = new List<EffectsContainerBase>();
}