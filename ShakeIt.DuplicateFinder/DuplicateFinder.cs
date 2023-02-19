namespace ShakeIt.DuplicateFinder;

public class DuplicateFinder
{
    public Dictionary<Guid, List<string>> GuidToEffect { get; } = new();

    public void Analyze(IList<ShakeItProfile> profiles)
    {
        foreach (var profile in profiles)
        {
            AnalyzeEffects(profile.EffectsContainers, profile.Name);
        }

        foreach (var (guid, list) in GuidToEffect)
        {
            if (list.Count > 1)
            {
                Console.WriteLine($"{list.Count} duplicates for guid {guid}");
                foreach (var name in list)
                {
                    Console.WriteLine($"  {name}");
                }
            }
        }
    }

    private void AnalyzeEffects(IList<EffectsContainerBase> effectsContainers, string baseName)
    {
        foreach (var effectsContainer in effectsContainers)
        {
            if (!GuidToEffect.ContainsKey(effectsContainer.ContainerId))
            {
                GuidToEffect[effectsContainer.ContainerId] = new List<string>();
            }

            GuidToEffect[effectsContainer.ContainerId].Add(baseName + " / " + effectsContainer.Description);

            AnalyzeEffects(effectsContainer.EffectsContainers, baseName + " / " + effectsContainer.Description);
        }
    }
}