/*
    Copyright (C) 2012  Alibaba

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel.Registration;
using NUnit.Framework;
using CodeSharp.Core;
using CodeSharp.Core.Castles;
using CodeSharp.Core.Services;
using Taobao.Workflow.Activities.Application;
using Taobao.Workflow.Activities.Hosting;
using System.Threading;
using Taobao.Activities;
using Taobao.Workflow.Activities.Statements;
using Taobao.Activities.Statements;
using Castle.Services.Transaction;

namespace Taobao.Workflow.Activities.Test
{
    [TestFixture(Description = "人工节点超时升级规则测试")]
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class EscalationRuleTest : BaseTest
    {
        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            windsor.RegisterComponent(typeof(TestHumanEscalationHelper));
            base.Resolve(windsor);
        }

        [SetUp]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitialize]
        public void Init()
        {
            _count = 0;
        }

        [Test(Description = "流程跳转")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void EscalationGotoResumption()
        {
            var process = this.PrepareEscalationGotoProcess();
            this.SyncScheduleUntil(process);
            //超时升级
            Thread.Sleep(TimeSpan.FromMinutes(EscalationExpiration));
            this.SyncScheduleUntil(process);
            Assert.AreEqual(1, _count);
            //被跳转至指定节点
            Assert.AreEqual(EscalationGotoActivity, TestHelper.GetCurrentActivityName(process));
        }

        [Test(Description = "任务转交")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void EscalationRedirectResumption()
        {
            var process = this.PrepareEscalationRedirectProcess();
            this.SyncScheduleUntil(process);
            //超时升级
            Thread.Sleep(TimeSpan.FromMinutes(EscalationExpiration));
            this.SyncScheduleUntil(process);

            Assert.AreEqual(1, _count);
            //该节点所有任务被转交至指定用户
            var list = this._workItemService.GetWorkItems(process).ToList();
            Assert.Greater(list.Count, 0);
            list.ForEach(o =>
            {
                Assert.AreEqual(Node1, o.ActivityName);
                Assert.AreEqual(EscalationRedirectToUserName, o.Actioner.UserName);
            });
        }
     
        [Test(Description = "循环通知")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void EscalationNotifyResumption()
        {
            var process = this.PrepareEscalationNotifyProcess();
            this.SyncScheduleUntil(process);
            //超时升级
            Thread.Sleep(TimeSpan.FromMinutes(EscalationExpiration));
            this.SyncScheduleUntil(process);
            Thread.Sleep(TimeSpan.FromMinutes(EscalationExpiration));
            this.SyncScheduleUntil(process);
            Thread.Sleep(TimeSpan.FromMinutes(EscalationExpiration));
            this.SyncScheduleUntil(process);

            Assert.AreEqual(3, _count);
        }

        private static int _count = 0;
        private Process PrepareEscalationGotoProcess()
        {
            return this.CreateProcess(this.CreateProcessTypeForEscalation(BaseTest.EscalationGotoFlag));
        }
        private Process PrepareEscalationNotifyProcess()
        {
            return this.CreateProcess(this.CreateProcessTypeForEscalation(BaseTest.EscalationNotifyFlag));
        }
        private Process PrepareEscalationRedirectProcess()
        {
            return this.CreateProcess(this.CreateProcessTypeForEscalation(BaseTest.EscalationRedirectFlag));
        }
        private ProcessType CreateProcessTypeForEscalation(string typeName)
        {
            //创建默认的测试
            var type = new ProcessType(typeName
                , new ProcessType.WorkflowDefinition(string.Empty)
                , Stub.WorkflowParser.GetEscalationActivitySettings(typeName).ToArray()) { Group = Group };
            this._processTypeService.Create(type);
            return type;
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