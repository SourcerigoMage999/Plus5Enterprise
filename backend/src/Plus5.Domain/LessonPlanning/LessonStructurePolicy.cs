namespace Plus5.Domain.LessonPlanning;

public static class LessonStructurePolicy
{
    public static readonly int[] BaselineWeights = [1, 2, 3, 4, 2];

    public static IReadOnlyList<int> AllocateDurations(int targetDurationMinutes)
    {
        if (targetDurationMinutes is < LessonPlanVersion.MinimumTargetDurationMinutes or > LessonPlanVersion.MaximumTargetDurationMinutes)
            throw new ArgumentOutOfRangeException(nameof(targetDurationMinutes));
        if (targetDurationMinutes == LessonPlanVersion.DefaultTargetDurationMinutes)
            return [5, 10, 15, 20, 10];

        var remaining = targetDurationMinutes - BaselineWeights.Length;
        var totalWeight = BaselineWeights.Sum();
        var allocations = BaselineWeights.Select(weight => 1 + (remaining * weight / totalWeight)).ToArray();
        var remainders = BaselineWeights.Select((weight, index) => new
        {
            Index = index,
            Remainder = remaining * weight % totalWeight,
        }).OrderByDescending(item => item.Remainder).ThenBy(item => item.Index).ToArray();
        var missing = targetDurationMinutes - allocations.Sum();
        for (var index = 0; index < missing; index++) allocations[remainders[index].Index]++;
        return allocations;
    }
}
