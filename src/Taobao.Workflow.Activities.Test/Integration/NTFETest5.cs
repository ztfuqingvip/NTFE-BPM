using System;
using System.Collections.Generic;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Services.Transaction;
using NUnit.Framework;
using CodeSharp.Core.Castles;
using CodeSharp.Core.Services;
using Taobao.Workflow.Activities.Application;
using Taobao.Workflow.Activities.Hosting;

namespace Taobao.Workflow.Activities.Test.Integration
{
    /// <summary>
    /// 针对新设计器解析器和BPM之间的集成测试 - 样例5
    /// </summary>
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class NTFETest5 : BaseTest
    {
        /*
         * human#节点1# 确认->human#节点2# 超时升级规则 0.05min Notify
         * human#节点2# 无
         */

        [SetUp]
        public void Init()
        {
            //每次调用清零
            _count = 0;
        }

        private static int _count = 0;

        [Test(Description = "循环通知")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_human1_escalationNotify()
        {
            var process = this.CreateProcess();
            this.Evict(process);
            Guid processId = process.ID;
            process = this._processService.GetProcess(process.ID);

            //调度后进入节点1
            this.SyncScheduleUntil(process);
            Thread.Sleep(3000);//3ms后超时
            //调度超时升级第一次消息通知
            this.SyncScheduleUntil(process);
            Thread.Sleep(3000);//3ms后超时
            //调度超时升级第二次消息通知
            this.SyncScheduleUntil(process);

            Assert.AreEqual(2, _count);
        }

        public new Process CreateProcess()
        {
            this.CreateProcessType();

            var user = this._userService.GetUserWhatever("xiaoxuan.lp");

            var processType = this._processTypeService.GetProcessType("NTFE_UnitTest5");
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
            this._managementApi.CreateProcessType("NTFE_UnitTest5"
                , Resource.NTFE_UnitTest5_Workflow
                , Resource.NTFE_UnitTest5_Settings
                , "NTFE_UnitTest5"
                , "UnitTest");
        }

        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            windsor.RegisterComponent(typeof(TestHumanEscalationHelper));
            windsor.Register(Component.For<IWorkflowParser>().ImplementedBy<WorkflowParser>());
            base.Resolve(windsor);
        }

        [CodeSharp.Core.Component]
        [Transactional]
        public class TestHumanEscalationHelper : HumanEscalationWaitingResumption.DefaultHumanEscalationHelper, HumanEscalationWaitingResumption.IHumanEscalationHelper
        {
            public TestHumanEscalationHelper(ILoggerFactory factory
                 , IWorkItemService workItemService
                 , IUserService userService
                 , ISchedulerService schedulerService
                 , ProcessService processService)
                : base(factory, workItemService, userService, schedulerService, processService) { }

            [Transaction(TransactionMode.Requires)]
            public override void Goto(Process process, string from, string to)
            {
                _count += 1;
                base.Goto(process, from, to);
            }
            [Transaction(TransactionMode.Requires)]
            public override void Redirect(Process process, string activityName, IEnumerable<WorkItem> workItems, string toUserName)
            {
                _count += 1;
                base.Redirect(process, activityName, workItems, toUserName);
            }
            [Transaction(TransactionMode.Requires)]
            public override void Notify(Process process, IEnumerable<WorkItem> workItems, string templateName)
            {
                _count += 1;
                base.Notify(process, workItems, templateName);
            }
        }
    }
}
