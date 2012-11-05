using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Castle.MicroKernel.Registration;
using Taobao.Workflow.Activities.Application;

namespace Taobao.Workflow.Activities.Test.Integration
{
    /// <summary>
    /// 针对新设计器解析器和BPM之间的集成测试 - 样例5
    /// </summary>
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class NTFETest6 : BaseTest
    {
        /*
         * human#节点1# 完成->human#节点2# 执行人规则 让其异常
         * human#节点2# 无
         */

        [Test(Description = "异常流测试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void TestMetWorkItem_human1_Wrong()
        {
            var process = this.CreateProcess();
            this.Evict(process);
            Guid processId = process.ID;
            process = this._processService.GetProcess(process.ID);

            //调度后进入节点1
            this.SyncScheduleUntil(process);
        }

        public new Process CreateProcess()
        {
            this.CreateProcessType();

            var user = this._userService.GetUserWhatever("xiaoxuan.lp");

            var processType = this._processTypeService.GetProcessType("NTFE_UnitTest6");
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
            this._managementApi.CreateProcessType("NTFE_UnitTest6"
                , Resource.NTFE_UnitTest6_Workflow
                , Resource.NTFE_UnitTest6_Settings
                , "NTFE_UnitTest6"
                , "UnitTest");
        }

        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            windsor.Register(Component.For<IWorkflowParser>().ImplementedBy<WorkflowParser>());
            base.Resolve(windsor);
        }
    }
}
