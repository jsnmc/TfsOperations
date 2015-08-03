

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using TfsOperations.Interfaces;

namespace TfsOps
{
    public class BuildCommunicator : IBuildCommunicator
    {
        private readonly string tfsServerAddress;
        internal IBuildServer buildServer;

        private IBuildServer BuildServer
        {
            get
            {
                if (buildServer == null)
                {
                    TfsTeamProjectCollection tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(tfsServerAddress));
                    this.buildServer = tfs.GetService<IBuildServer>();
                }
                return this.buildServer;
            }
        }

        public BuildCommunicator(string tfsServerAddress)
        {
            this.tfsServerAddress = tfsServerAddress;
        }

        public BuildStatus GetBuildInformation(int maxDays, int maxRuns, string teamProject, IList<string> buildDefinitions)
        {
            var buildStatus = new BuildStatus();

            foreach (var bd in buildDefinitions)
            {
                var builds = GetBuildsFromTfs(maxDays, teamProject, bd);
                if(builds.Any())
                {
                    var project = MapBuildToProject(builds.FirstOrDefault(), builds.FirstOrDefault().BuildDefinition.Name);
                    buildStatus.Projects.Add(project);
                }
            }

            return buildStatus;
        }

        public BuildStatus GetBuildInformation(int maxDays = 5, int maxRuns = 10, string teamProject = "*", string buildDefinition = "")
        {
            var buildStatus = new BuildStatus();

            var builds = GetBuildsFromTfs(maxDays, teamProject, buildDefinition);

            var currentDefinition = string.Empty;

            foreach (var build in builds)
            {
                string definitionName = build.BuildDefinition.Name;
                var project = MapBuildToProject(build, definitionName);

                if (definitionName == currentDefinition)
                {
                    AddBuildToParentProject(buildStatus, definitionName, project, maxRuns);
                }
                else
                {
                    currentDefinition = definitionName;
                    buildStatus.Projects.Add(project);
                }
            }
            return buildStatus;
        }

        private IEnumerable<IBuildDetail> GetBuildsFromTfs(int maxDays, string teamProject, string buildDefinition)
        {
            IBuildDetailSpec spec = string.IsNullOrEmpty(buildDefinition)
                                        ? BuildServer.CreateBuildDetailSpec(teamProject)
                                        : BuildServer.CreateBuildDetailSpec(teamProject, buildDefinition);

            spec.InformationTypes = null;
            spec.MinFinishTime = DateTime.Now.Subtract(TimeSpan.FromDays(maxDays));
            spec.MaxFinishTime = DateTime.Now;
            spec.QueryDeletedOption = QueryDeletedOption.IncludeDeleted;

            var builds = BuildServer.QueryBuilds(spec).Builds.OrderBy(b => b.BuildDefinition.Name).ThenByDescending(b => b.FinishTime);
            return builds;
        }

        /// <summary>
        /// This function is responsible for running a quick query to get broken build stats.
        /// It counts each and every failure int a 24hour period.  It doesn't single out a chain, but counts individual failures
        /// </summary>
        /// <param name="tfsProject"></param>
        /// <param name="tfsConfigurations"></param>
        /// <returns></returns>
        public void GetRawBuildStats(string tfsProject, List<string> tfsConfigurations, ref IBuildStats buildStats)
        {
            IEnumerable<IBuildDetail> info = GetBuildsFromTfs(1, tfsProject, "");

            var currentDefinition = string.Empty;
            if (tfsConfigurations.Count > 0)
            {
                buildStats.TotalBuildFailures = info.Count() / tfsConfigurations.Count;
            }
            else
            {
                buildStats.TotalBuildFailures = 0;
            }

            foreach (var project in info)
            {
                if (tfsConfigurations.Contains(project.BuildDefinition.Name))
                {
                    if (project.Status == Microsoft.TeamFoundation.Build.Client.BuildStatus.Failed)
                    {
                        // Log failure                    
                        buildStats.TotalProjectFailures++;
                    }

                    buildStats.TotalBuilds++;
                }
            }
        }

        private Project MapBuildToProject(IBuildDetail build, string definitionName)
        {
            var project = new Project
            {
                DefinitionName = definitionName,
                Name = build.TeamProject,
                Status = build.Status,
                StartTime = build.StartTime,
                FinishTime = build.FinishTime
            };
            return project;
        }

        private void AddBuildToParentProject(BuildStatus buildStatus, string definitionName, Project project, int maxRuns)
        {
            var parent = buildStatus.Projects.First(p => p.DefinitionName == definitionName);
            if (parent.Runs.Count < maxRuns)
            {
                parent.Runs.Add(project);
            }
        }
    }
}