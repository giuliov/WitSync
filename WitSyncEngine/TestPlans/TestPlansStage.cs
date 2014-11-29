using Microsoft.TeamFoundation.TestManagement.Client;
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

        public override int Execute(StageConfiguration configuration)
        {
            mapping = (TestPlansStageConfiguration)configuration;

            var sourceTestSvc = sourceConn.Collection.GetService<ITestManagementService>();
            var sourceProject = sourceTestSvc.GetTeamProject(sourceConn.ProjectName);
            var destTestSvc = destConn.Collection.GetService<ITestManagementService>();
            var destProject = destTestSvc.GetTeamProject(destConn.ProjectName);


            if (!configuration.TestOnly)
            {
                /*
                var newPlan = destProject.TestPlans.Create();
                var newSuite = destProject.TestSuites.CreateStatic();
                newSuite.TestCases.AddCases();
                newPlan.Save();
                 * */
                this.ChangeLog.AddEntry(new TestPlanChangeEntry("TODO"));
            }

            return saveErrors;
        }
    }
}
