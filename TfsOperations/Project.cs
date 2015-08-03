using System;
using System.Collections.Generic;

namespace TfsOps
{
    public class Project
    {
        public Project()
        {
            Runs = new List<Project>();
        }

        public string DefinitionName { get; set; }
        public string Name { get; set; }
        public Microsoft.TeamFoundation.Build.Client.BuildStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
        public List<Project> Runs { get; set; }
    }
}