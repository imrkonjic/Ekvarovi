using EKvarovi.Shared.Enums;

namespace EKvarovi.Api.Infrastructure;

public static class FaultReportStatusTransitions
{
    private static readonly Dictionary<int, HashSet<int>> Allowed = new()
    {
        [FaultStatusIds.Zaprimljeno] = [FaultStatusIds.Pregledano, FaultStatusIds.Dodijeljeno],
        [FaultStatusIds.Pregledano] = [FaultStatusIds.Dodijeljeno],
        [FaultStatusIds.Dodijeljeno] = [FaultStatusIds.URadu],
        [FaultStatusIds.URadu] = [FaultStatusIds.Rijeseno],
        [FaultStatusIds.Rijeseno] = [FaultStatusIds.Zatvoreno, FaultStatusIds.URadu],
        [FaultStatusIds.Zatvoreno] = []
    };

    public static void EnsureAllowed(int fromStatusId, int toStatusId)
    {
        if (fromStatusId == toStatusId)
            return;

        if (!Allowed.TryGetValue(fromStatusId, out var targets) || !targets.Contains(toStatusId))
            throw AppException.Validation("Prijelaz statusa prijave nije dopušten.");
    }
}
