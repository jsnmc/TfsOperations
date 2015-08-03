using System;

namespace TfsOps
{
    public interface IBuildCommunicator
    {
        BuildStatus GetBuildInformation(int maxDays = 5, int maxRuns = 10, string teamProject = "*", string buildDefinition = "");
    }
}