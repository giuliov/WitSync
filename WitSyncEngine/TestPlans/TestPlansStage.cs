using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    internal class TestPlanChangeEntry : SuccessEntry
    {
        internal TestPlanChangeEntry(string name)
            : base("TestPlan", name, name, "AddOrUpdate")
        { }
    }

    public class TestPlansStage : PipelineStage
    {
        public TestPlansStage(TfsConnection source, TfsConnection dest, IEngineEvents eventHandler)
            : base(source, dest, eventHandler)
        {
            //no-op
        }

        protected TestPlansStageConfiguration mapping;

        public override int Prepare(StageConfiguration configuration)
        {
            mapping = (TestPlansStageConfiguration)configuration;
            return 0;
        }

        /*
         * Some pointers http://blogs.msdn.com/b/densto/archive/2010/03/04/the-test-management-api-part-2-creating-modifying-test-plans.aspx
         * http://blogs.msdn.com/b/duat_le/archive/2010/02/25/wiql-for-test.aspx
         */
        public override int Execute(StageConfiguration configuration)
        {
            mapping = (TestPlansStageConfiguration)configuration;

            var sourceTestSvc = sourceConn.Collection.GetService<ITestManagementService>();
            var sourceProject = sourceTestSvc.GetTeamProject(sourceConn.ProjectName);
            var destTestSvc = destConn.Collection.GetService<ITestManagementService>();
            var destProject = destTestSvc.GetTeamProject(destConn.ProjectName);

            // sync contextual data
            sourceProject.

            var srcPlans = sourceProject.TestPlans.Query(mapping.SourceQuery);
            foreach (ITestPlan2 srcPlan in srcPlans)
            {
                ITestPlan2 newPlan = (ITestPlan2)destProject.TestPlans.Create();
                newPlan.AreaPath = mapAreaPath(srcPlan.AreaPath);
                newPlan.AutomatedTestEnvironmentId = mapAutomatedTestEnvironment(srcPlan.AutomatedTestEnvironmentId);
                newPlan.AutomatedTestSettingsId = mapAutomatedTestSettings(srcPlan.AutomatedTestSettingsId);
                newPlan.CopyPropertiesFrom(srcPlan);
                newPlan.Description = srcPlan.Description;
                newPlan.EndDate = srcPlan.EndDate;
                newPlan.Iteration = mapIteration(srcPlan.Iteration);
                //newPlan.Links
                newPlan.ManualTestEnvironmentId = mapManualTestEnvironment(srcPlan.ManualTestEnvironmentId);
                newPlan.ManualTestSettingsId = mapManualTestSettings(srcPlan.ManualTestSettingsId);
                newPlan.Name = srcPlan.Name;
                newPlan.StartDate = srcPlan.StartDate;
                newPlan.Status = srcPlan.Status;

                var root = sourceProject.TestSuites.FetchTestSuitesForPlan(srcPlan, srcPlan.RootSuite.Id);
                foreach (var srcSuite in root.SubSuites)
                {
                    ITestSuiteBase newSuite = null;
                    switch (srcSuite.TestSuiteType)
                    {
                        case TestSuiteType.DynamicTestSuite:
                            newSuite = destProject.TestSuites.CreateDynamic();
                            break;
                        case TestSuiteType.None:
                            break;
                        case TestSuiteType.RequirementTestSuite:
                            WorkItem wi = null; //TODO lookup index
                            newSuite = destProject.TestSuites.CreateRequirement(wi);
                            break;
                        case TestSuiteType.StaticTestSuite:
                            newSuite = destProject.TestSuites.CreateStatic();
                            //srcSuite.TestSuiteEntry
                            break;
                        default:
                            break;
                    }//switch
                    newSuite.Description = srcSuite.Description;
                    newSuite.Title = srcSuite.Title;

                    var destCases = new List<ITestCase>();
                    foreach (var srcCase in srcSuite.AllTestCases)
                    {
                        // Assume it was cloned in Work Item stage
                        int it = mapTestCase(srcCase.Id);
                        ITestCase destCase = destProject.TestCases.Find(it);

                        // destCase.TestSuiteEntry ???

                        destCases.Add(destCase);
                    }
                    newSuite.TestCases.AddCases(destCases, true);
                }
            }

            if (!configuration.TestOnly)
            {
                /*
                newPlan.Save();
                this.ChangeLog.AddEntry(new TestPlanChangeEntry("TODO"));
                 * */
            }

            return saveErrors;
        }

        private int mapTestCase(int id)
        {
            throw new NotImplementedException();
        }

        private string mapIteration(string iterationPath)
        {
            throw new NotImplementedException();
        }

        private Guid mapManualTestEnvironment(Guid guid)
        {
            throw new NotImplementedException();
        }

        private int mapManualTestSettings(int settings)
        {
            throw new NotImplementedException();
        }

        private int mapAutomatedTestSettings(int settings)
        {
            throw new NotImplementedException();
        }

        private Guid mapAutomatedTestEnvironment(Guid guid)
        {
            throw new NotImplementedException();
        }

        private string mapAreaPath(string areaPath)
        {
            throw new NotImplementedException();
        }
    }
}
