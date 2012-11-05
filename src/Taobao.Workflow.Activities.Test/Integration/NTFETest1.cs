using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Registration;
using NUnit.Framework;
using Taobao.Workflow.Activities.Application;

namespace Taobao.Workflow.Activities.Test.Integration
{
    /// <summary>
    /// 针对新设计器解析器和BPM之间的集成测试 - 样例1
    /// </summary>
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class NTFETest1 : BaseTest
    {
        /*
         * human#节点1# 同意->human#节点2# | 否决->server#自动节点1#
         * human#节点2# 确认->subprocess#子流程节点1# 
         * server#自动节点1# 无
         * subprocess#子流程节点1# Default->human#节点3#
         * human#节点3# 无
         * 
         * subprocess 统一使用 ClientTest2
         * subporcess 只有一个节点 human#节点1# 无
         */

        [Test(Description = "测试用例")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_human1_human2_subprocess1_human3()
        {
            var process = this.CreateProcess();
            this.Evict(process);
            Guid processId = process.ID;
            process = this._processService.GetProcess(process.ID);

            this._managementApi.CreateProcessType("ClientTest2"
                , Resource.ClientTest2_Workflow
                , Resource.ClientTest2_Settings
                , "ClientTest2"
                , "UnitTest");
            var subProcessType = this._processTypeService.GetProcessType("ClientTest2");

            WorkItem wi = null;
            var user = this._userService.GetUserWhatever("xiaoxuan.lp");      

            //调度后进入节点1
            this.SyncScheduleUntil(process);

            Assert.AreEqual("节点1", TestHelper.GetCurrentActivityName(process));
            wi = this._workItemService.GetWorkItems(user, process, null).First();
            this._workItemService.Execute(wi.ID, wi.Actioner, "同意", new Dictionary<string, string> { { "UserName1", "xiaoxuan.lp" } });

            //调度后进入节点2
            this.SyncScheduleUntil(process);

            Assert.AreEqual("节点2", TestHelper.GetCurrentActivityName(process));
            wi = this._workItemService.GetWorkItems(user, process, null).First();
            this._workItemService.Execute(wi.ID, user, "确认", new Dictionary<string, string> { });

            //调度后进入子流程节点1
            this.SyncScheduleUntil(process);

            Assert.AreEqual("子流程节点1", TestHelper.GetCurrentActivityName(process));
            Process subProcess = null;
            Guid subProcessId = Guid.Empty;
            long subProcessCount = 0;
            IList<Process> subProcesses = this._processService.GetProcesses(
                NHibernate.Criterion.DetachedCriteria.For<Process>().Add(
                NHibernate.Criterion.Expression.Eq("ProcessType", subProcessType)), 0, 10, out subProcessCount)
                .ToList();
            if (subProcessCount > 0)
            {
                subProcess = subProcesses[0];
                subProcessId = subProcess.ID;
            }
            user = this._userService.GetUserWhatever("wfservice");

            //调度后进入ClientTest2-节点1
            this.SyncScheduleUntil(subProcess);

            //ClientTest2-节点1
            wi = this._workItemService.GetWorkItems(user, subProcess, null).First();
            Assert.AreEqual("节点1", wi.ActivityName);
            _workItemService.Execute(wi.ID, user, "同意", null);

            //调度后ClientTest2流程结束
            this.SyncScheduleUntil(subProcess);

            //调度后进入节点3
            this.SyncScheduleUntil(process);

            user = this._userService.GetUserWhatever("xiaoxuan.lp");
            Assert.AreEqual("节点3", TestHelper.GetCurrentActivityName(process));
            wi = this._workItemService.GetWorkItems(user, process, null).First();
            this._workItemService.Execute(wi.ID, user, "完成", new Dictionary<string, string> { });

            //调度后流程结束
            this.SyncScheduleUntil(process);

            Assert.AreEqual(ProcessStatus.Completed, process.Status);
        }

        [Test(Description = "测试用例")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_human1_server1()
        {
            var process = this.CreateProcess();
            this.Evict(process);
            Guid processId = process.ID;
            process = this._processService.GetProcess(process.ID);

            WorkItem wi = null;
            var user = this._userService.GetUserWhatever("xiaoxuan.lp");

            //调度后进入节点1
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

            var processType = this._processTypeService.GetProcessType("NTFE_UnitTest1");
            var process = new Process("From UnitTest at " + DateTime.Now,
                processType,
                user);
            this._processService.Create(process);
            return process;
        }
        [Test(Description = "流程类型创建")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void CreateProcessType()
        {
            this._managementApi.CreateProcessType("NTFE_UnitTest1"
                , Resource.NTFE_UnitTest1_Worflow
                , Resource.NTFE_UnitTest1_Settings
                , "NTFE_UnitTest1"
                , "UnitTest");
        }

        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            windsor.Register(Component.For<IWorkflowParser>().ImplementedBy<WorkflowParser>());
            base.Resolve(windsor);
        }
    }
}
