namespace EKvarovi.Shared.Enums;

public enum HistoryChangeType : short
{
    Created = 1,
    StatusChanged = 2,
    PriorityChanged = 3,
    TypeChanged = 4,
    DueDateChanged = 5,
    Assigned = 6,
    Reassigned = 7,
    InterventionStarted = 8,
    InterventionFinished = 9,
    Reopened = 10,
    Closed = 11
}
