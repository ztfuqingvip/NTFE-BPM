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
using System.Linq;
using System.Text;
using Castle.MicroKernel.Registration;
using Taobao.Workflow.Activities.Hosting;
using CodeSharp.Core.Services;
using CodeSharp.Core.Castles;
using System.Threading;
using System.Reflection;
using CodeSharp.Core;
using NUnit.Framework;
using Taobao.Activities.Statements;
using Taobao.Workflow.Activities.Statements;
using Taobao.Activities;
using Taobao.Activities.Hosting;

namespace Taobao.Workflow.Activities.Test
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture(Description = "Hosting.Scheduler错误调度测试")]
    public class SchedulerFaultResumptionTest : BaseTest
    {
        private static Process _process;
        private static int _faults, _resumCount;

        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            //提前注册桩
            windsor.RegisterComponent(typeof(ProcessServiceMock));
            windsor.RegisterComponent(typeof(SchedulerServiceMock));
            base.Resolve(windsor);
        }

        [SetUp]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitialize]
        public void Prepare()
        {
            _process = null;
            _faults = _resumCount = 0;
        }

        [Test(Description = "调度异常测试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        [Ignore]
        public void FaultResumption()
        {
            _process = this.CreateProcess(SequenceWorkflowParser.GetActivitySettings());
            TestHelper.SetProcessId(_process, Guid.NewGuid());
            TestHelper.ChangeProcessStatus(_process, ProcessStatus.Running);
            //验证调度器串行调度逻辑
            TestHelper.Resume(DependencyResolver.Resolve<Scheduler>(), new List<long>() { 1, 2, 3 }.OrderBy(o => o));
            //并行活动内的调度项的异常不互相影响，前2个都可以被执行，在第三个调度项执行时停止
            Assert.AreEqual(2, _resumCount);
        }

        private Process CreateProcess(IEnumerable<ActivitySetting> settings)
        {
            return new Process("test"
                , new ProcessType("test", new ProcessType.WorkflowDefinition(""), settings.ToArray())
                , this.GetUser("houkun"));
        }

        [CodeSharp.Core.Component]
        public class ProcessServiceMock : ProcessService, IProcessService
        {
            public ProcessServiceMock(ILoggerFactory factory, IWorkflowParser parser, IEventBus eventBus, ISchedulerService schedulerService)
                : base(factory, parser, eventBus, schedulerService) { }

            void IProcessService.Create(Process process) { }
            void IProcessService.Create(Process process, Guid assignedId) { }
            Process IProcessService.GetProcess(Guid id) { return _process; }
            void IProcessService.Update(Process process) { }
            void IProcessService.UpdateWorkflowInstanceOfProcess(Process process, Process.InternalWorkflowInstance instance) { }
            void IProcessService.CreateActivityInstance(ActivityInstanceBase instance) { }
            ActivityInstanceBase GetActivityInstanceByWorkflowActivityInstanceId(Process process, long workflowActivityInstanceId) { return null; }
        }
        [CodeSharp.Core.Component]
        public class SchedulerServiceMock : SchedulerService, ISchedulerService
        {
            public SchedulerServiceMock() : base(string.Empty) { }

            void ISchedulerService.Add(WaitingResumption r)
            {

            }
            void ISchedulerService.AddErrorRecord(ErrorRecord record)
            {
                _faults += 1;
            }

            WaitingResumption ISchedulerService.Get(long id)
            {
                if (id == 1)
                    return new WorkItemCreateResumption(_process
                        , new HumanActivityInstance(_process.ID, 0, 1, "人工节点", "错误的书签", new string[] { "houkun" }));
                if (id == 2)
                    return new SubProcessCreateResumption(_process
                        , new SubProcessActivityInstance(_process.ID, 0, 2, "子流程-节点2", "bookmark"));
                if (id == 3)
                    return new WorkItemCreateResumption(_process
                        , new HumanActivityInstance(_process.ID, 1, 1, "人工节点", "错误的书签", new string[] { "houkun" }));
                return null;
            }
            void ISchedulerService.MarkAsError(WaitingResumption r)
            {
                _resumCount += 1;
            }
        }
        //顺序流程core调度模拟 人工->子流程->服务端/自动
        public class SequenceWorkflowParser : IWorkflowParser
        {
            public static Human First = null;
            public static SubProcess Second = null;
            public static IEnumerable<ActivitySetting> GetActivitySettings()
            {
                //此处仅模拟结构，实际规则等设置不会在调度中生效
                //0
                yield return new HumanSetting(0, "节点1"
                    , new string[] { "完成" }, -1
                    , HumanSetting.SlotDistributionMode.AllAtOnce, "", null
                    , new HumanActionerRule("错误的执行人"), null, null, false);
                //2
                yield return new SubProcessSetting(2, "SubProcess", "子流程-节点2", null, null, false);
                //4
                yield return new ServerSetting(4, "节点3", "'1'", "v1", null, null, false);
            }
            private string _completion, _finishRule, _actionerRule;
            public SequenceWorkflowParser() : this(true) { }
            public SequenceWorkflowParser(bool error)
            {
                this._finishRule = !error ? "true" : "错误的子流程节点完成规则";
                this._actionerRule = !error ? "originator" : "错误的执行人";
            }

            #region IWorkflowParser Members
            public WorkflowActivity Parse(string workflowDefinition, IEnumerable<ActivitySetting> activitySettings)
            {
                return this.Parse(null, workflowDefinition, activitySettings);
            }
            public Statements.WorkflowActivity Parse(string key, string workflowDefinition, IEnumerable<ActivitySetting> activitySettings)
            {
                var settings = GetActivitySettings();
                var flow = new Statements.WorkflowActivity();
                var v1 = new Variable<string>("v1");
                flow.Variables.Add(v1);

                var third = WorkflowBuilder.CreateServer(settings.ElementAt(2), "节点3", "'1'", null, v1, flow.CustomActivityResult, null, null);

                var second = WorkflowBuilder.CreateSubProcess(settings.ElementAt(1)
                    , "子流程-节点2"
                    , new Dictionary<string, string>() { { "error", this._finishRule } }
                    , null, null
                    , third);

                SequenceWorkflowParser.Second = second.Action as SubProcess;

                flow.Body.StartNode = WorkflowBuilder.CreateHuman(settings.ElementAt(0)
                    , "节点1"
                    , new GetUsers(this._actionerRule)
                    , flow.CustomActivityResult
                    , null
                    , second);

                SequenceWorkflowParser.First = (flow.Body.StartNode as FlowStep).Action as Human;

                return flow;
            }
            public string Parse(Statements.WorkflowActivity workflowActivity, string originalWorkflowDefinition)
            {
                throw new NotImplementedException();
            }
            public string Parse(IEnumerable<ActivitySetting> activitySettings)
            {
                throw new NotImplementedException();
            }
            public ActivitySetting[] ParseActivitySettings(string workflowDefinition, string activitySettingsDefinition)
            {
                throw new NotImplementedException();
            }
            public string ParseWorkflowDefinition(string workflowDefinition, string activitySettingsDefinition)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
    }
}