using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Castle.MicroKernel.Registration;
using Taobao.Workflow.Activities.Application;
using NUnit.Framework;

namespace Taobao.Workflow.Activities.Test.Integration
{
    /// <summary>
    /// 针对新设计器解析器和BPM之间的集成测试 - 样例2
    /// </summary>
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class NTFETest2 : BaseTest
    {
        /*
         * human#节点1# 同意->human#节点2# | 否决->server#节点3#
         * human#节点2# 否决->human#节点1# | 同意->parallelContainer#并行节点4#
         * server#节点3# 无
         * parallelContainer#并行节点4# Default->human#节点5#
         * humanPar#并行子节点1#, huamanPar#并行子节点2#
         * human#节点5# 无
         */

        [Test(Description = "测试用例")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_human1_human2_parallelContainer4_human5()
        {
            var process = this.CreateProcess();
            this.Evict(process);
            Guid processId = process.ID;
            process = this._processService.GetProcess(process.ID);

            WorkItem wi = null;
            var user = this._userService.GetUserWhatever("xiaoxuan.lp");
            var superior = this._userService.GetUserWhatever("xiexun");

            //调度后进入节点1
            this.SyncScheduleUntil(process);

            Assert.AreEqual("节点1", TestHelper.GetCurrentActivityName(process));
            wi = this._workItemService.GetWorkItems(user, process, null).First();
            this._workItemService.Execute(wi.ID, user, "同意", new Dictionary<string, string> { });

            //调度后进入节点2
            this.SyncScheduleUntil(process);

            Assert.AreEqual("节点2", TestHelper.GetCurrentActivityName(process));
            wi = this._workItemService.GetWorkItems(superior, process, null).First();
            this._workItemService.Execute(wi.ID, superior, "同意", new Dictionary<string, string> { });

            //调度后进入并行节点4
            this.SyncScheduleUntil(process);

            var wi1 = this._workItemService.GetWorkItems(superior, process, null).First();
            var wi2 = this._workItemService.GetWorkItems(user, process, null).First();
            Assert.AreEqual("并行子节点1", wi1.ActivityName);
            Assert.AreEqual("并行子节点2", wi2.ActivityName);
            this._workItemService.Execute(wi1.ID, superior, "完成", new Dictionary<string, string> { });
            this._workItemService.Execute(wi2.ID, user, "完成", new Dictionary<string, string> { });

            //调度后流程结束
            this.SyncScheduleUntil(process);

            Assert.AreEqual(ProcessStatus.Completed, process.Status);
        }

        [Test(Description = "测试用例")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_human1_human2_human1_server3()
        {
            var process = this.CreateProcess();
            this.Evict(process);
            Guid processId = process.ID;
            process = this._processService.GetProcess(process.ID);

            WorkItem wi = null;
            var user = this._userService.GetUserWhatever("xiaoxuan.lp");
            var superior = this._userService.GetUserWhatever("xiexun");

            //调度后进入节点1
            this.SyncScheduleUntil(process);

            Assert.AreEqual("节点1", TestHelper.GetCurrentActivityName(process));
            wi = this._workItemService.GetWorkItems(user, process, null).First();
            this._workItemService.Execute(wi.ID, user, "同意", new Dictionary<string, string> { });

            //调度后进入节点2
            this.SyncScheduleUntil(process);

            Assert.AreEqual("节点2", TestHelper.GetCurrentActivityName(process));
            wi = this._workItemService.GetWorkItems(superior, process, null).First();
            this._workItemService.Execute(wi.ID, superior, "否决", new Dictionary<string, string> { });

            //调度后重回到节点1
            this.SyncScheduleUntil(process);

            Assert.AreEqual("节点1", TestHelper.GetCurrentActivityName(process));
            wi = this._workItemService.GetWorkItems(user, process, null).First();
            this._workItemService.Execute(wi.ID, user, "否决", new Dictionary<string, string> { });

            //调度后流程结束
            this.SyncScheduleUntil(process);

            Assert.AreEqual(ProcessStatus.Completed, process.Status);
        }

        public new Process CreateProcess()
        {
            this.CreateProcessType();

            var user = this._userService.GetUserWhatever("xiaoxuan.lp");

            var processType = this._processTypeService.GetProcessType("NTFE_UnitTest2");
            var process = new Process("From UnitTest at " + DateTime.Now
                , processType
                , user
                , 0
                , new Dictionary<string, string> { { "username1", "" } });
            this._processService.Create(process);
            return process;
        }
        [Test(Description = "流程类型创建")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void CreateProcessType()
        {
            this._managementApi.CreateProcessType("NTFE_UnitTest2"
                , Resource.NTFE_UnitTest2_Workflow
                , Resource.NTFE_UnitTest2_Settings
                , "NTFE_UnitTest2"
                , "UnitTest");
        }

        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            windsor.Register(Component.For<IWorkflowParser>().ImplementedBy<WorkflowParser>());
            base.Resolve(windsor);
        }
    }
}
