using PathForge.Domain.Enums;

namespace PathForge.Domain.Rules;

public static class ProblemStatusRules
{
    public static bool CanTransition(ProblemStatus current, ProblemStatus next)
    {
        return (current, next) switch
        {
            (ProblemStatus.NotStarted, ProblemStatus.InProgress) => true,
            (ProblemStatus.NotStarted, ProblemStatus.Blocked) => true,
            (ProblemStatus.InProgress, ProblemStatus.Blocked) => true,
            (ProblemStatus.Blocked, ProblemStatus.InProgress) => true,
            (ProblemStatus.InProgress, ProblemStatus.Resolved) => true,
            (ProblemStatus.Blocked, ProblemStatus.Resolved) => true,
            (ProblemStatus.Resolved, ProblemStatus.InProgress) => true,
            _ => false
        };
    }
}
