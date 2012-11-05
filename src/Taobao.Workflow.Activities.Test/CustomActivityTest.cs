using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

using Taobao.Activities;
using Taobao.Activities.Statements;
using Taobao.Workflow.Activities.Statements;
using Taobao.Activities.Hosting;
using System.Diagnostics;

namespace Taobao.Workflow.Activities.Test
{
    /// <summary>
    /// 人工活动节点测试
    /// </summary>
    [TestClass]
    public class CustomActivityTest 
    {
        [TestMethod]
        public void Human()
        {
            Taobao.Activities.Hosting.WorkflowInstance.IsEnableDebug = true;

            var app = new WorkflowInstance(new HumanWorkflow(), null);
            app.Extensions.Add<HumanExtension>(new HumanExtension());
            app.Extensions.Add<CustomExtension>(new CustomExtension(new List<CustomSetting>() { new ServerSetting(0, "节点1", "", "", null, null, false) }));
            app.Extensions.Add<DataFieldExtension>(new DataFieldExtension("", 0, null));
            app.Run();

            Thread.Sleep(1000);
        }

        [TestMethod]
        public void Server()
        {
            Taobao.Activities.Hosting.WorkflowInstance.IsEnableDebug = true;

            var app = new WorkflowInstance(new ServerWorkflow(), null);
            app.Extensions.Add<HumanExtension>(new HumanExtension());
            app.Extensions.Add<CustomExtension>(new CustomExtension(new List<CustomSetting>() { new ServerSetting(0, "节点1", "", "", null, null, false) }));
            app.Extensions.Add<DataFieldExtension>(new DataFieldExtension("",0, null));
            app.Run();

            Thread.Sleep(1000);

            Assert.AreEqual("test", app.Extensions.Find<DataFieldExtension>().DataFields["Variable1"]);
        }

        class HumanWorkflow : NativeActivity
        {
            public Flowchart Body { get; set; }

            public Variable<string> CustomResult { get; set; }

            public HumanWorkflow()
            {
                this.DisplayName = "NTFE Workflow";
                this.Body = new Flowchart();
                this.CustomResult = new Variable<string>("CustomResult");

                this.Body.StartNode = WorkflowBuilder.CreateHuman(new ServerSetting(0, "节点1", "", "", null, null, false)
                    , "节点1"
                    , o => new string[] { } //{ "user1", "user2", "user3" }
                    , this.CustomResult
                    , null
                    , null);
            }

            protected override void Execute(NativeActivityContext context)
            {
                context.ScheduleActivity(this.Body);
            }
        }
        class ServerWorkflow : NativeActivity
        {
            public Variable<string> CustomResult { get; set; }
            public Variable<string> Variable1 { get; set; }

            public Flowchart Body { get; set; }

            public ServerWorkflow()
            {
                this.DisplayName = "NTFE Workflow";
                this.Body = new Flowchart();
                this.CustomResult = new Variable<string>("CustomResult");
                this.Variable1 = new Variable<string>("Variable1");

                this.Body.StartNode = WorkflowBuilder.CreateServer(new ServerSetting(0, "节点1", "", "", null, null, false)
                    , "节点1"
                    , "test"
                    , null
                    , this.Variable1
                    , this.CustomResult
                    , null
                    , null);
            }

            protected override void Execute(NativeActivityContext context)
            {
                context.ScheduleActivity(this.Body);
            }
        }
    }
}
