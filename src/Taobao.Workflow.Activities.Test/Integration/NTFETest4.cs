using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Castle.MicroKernel.Registration;
using Taobao.Workflow.Activities.Application;
using System.Threading;

namespace Taobao.Workflow.Activities.Test.Integration
{
    /// <summary>
    /// 针对新设计器解析器和BPM之间的集成测试 - 样例4
    /// </summary>
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class NTFETest4 : BaseTest
    {
        /*
         * human#节点1# 确认->human#节点2# 超时升级规则 0.05min Redirect:chong.xuc
         * human#节点2# 无
         */

        [Test(Description = "任务转交")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_human1_escalationRedirect_human2()
        {
            var process = this.CreateProcess();
            this.Evict(process);
            Guid processId = process.ID;
            process = this._processService.GetProcess(process.ID);

            WorkItem wi = null;
            var user = this._userService.GetUserWhatever("xiaoxuan.lp");
            var redirectTo = this._userService.GetUserWhatever("chong.xuc");

            //调度后进入节点1
            this.SyncScheduleUntil(process);

            Thread.Sleep(3000);//3ms后超时

            //调度超时升级任务转交给chong.xuc
            this.SyncScheduleUntil(process);

            //被跳转至指定节点
            Assert.AreEqual("节点1", TestHelper.GetCurrentActivityName(process));
            wi = this._workItemService.GetWorkItems(redirectTo, process, null).First();
            this._workItemService.Execute(wi.ID, redirectTo, "确认", new Dictionary<string, string> { });

            //调度后进入节点2
            this.SyncScheduleUntil(process);

            //被跳转至指定节点
            Assert.AreEqual("节点2", TestHelper.GetCurrentActivityName(process));
            wi = this._workItemService.GetWorkItems(user, process, null).First();
            this._workItemService.Execute(wi.ID, user, "完成", new Dictionary<string, string> { });

            //调度后流程结束
            this.SyncScheduleUntil(process);

            Assert.AreEqual(ProcessStatus.Completed, process.Status);
        }

        public new Process CreateProcess()
        {
            this.CreateProcessType();

            var user = this._userService.GetUserWhatever("xiaoxuan.lp");

            var processType = this._processTypeService.GetProcessType("NTFE_UnitTest4");
            var process = new Process("From UnitTest at " + DateTime.Now
                , processType
                , user);
            this._processService.Create(process);
            return process;
        }
        [Test(Description = "测试用例")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void CreateProcessType()
        {
            this._managementApi.CreateProcessType("NTFE_UnitTest4"
                , Resource.NTFE_UnitTest4_Workflow
                , Resource.NTFE_UnitTest4_Settings
                , "NTFE_UnitTest4"
                , "UnitTest");
        }

        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            windsor.Register(Component.For<IWorkflowParser>().ImplementedBy<WorkflowParser>());
            base.Resolve(windsor);
        }
    }
}
