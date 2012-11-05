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
    [TestFixture(Description = "对引擎核心调度器逻辑部分的测试")]
    public class SchedulerCoreTest : BaseTest
    {
        private static Process _process;
        private static int _faults, _subProcessCount, _humanCount, _serverCount;

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
            _faults = _subProcessCount = _humanCount = _serverCount = 0;
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void GetChargingBy()
        {
            var schedulers = new string[] { "1", "2", "3" };
            _log.Info(schedulers[Math.Abs(Guid.NewGuid().GetHashCode()) % schedulers.Length]);
        }

        [Test(Description = "人工活动调度测试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Human()
        {
            _process = this.CreateProcess(SequenceWorkflowParser.GetActivitySettings());
            TestHelper.SetProcessId(_process, Guid.NewGuid());
            var instance = WorkflowBuilder.CreateInstance(_process, new SequenceWorkflowParser(false));
            instance.Run();
            Thread.Sleep(1000);
            Assert.AreEqual(1, _humanCount);
            this.AssertWorkflowInstance(instance, 1, 0);
        }

        [Test(Description = "自动活动调度测试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Server()
        {
            _process = this.CreateProcess(SequenceWorkflowParser.GetActivitySettings());
            TestHelper.SetProcessId(_process, Guid.NewGuid());
            var instance = WorkflowBuilder.CreateInstance(_process, new SequenceWorkflowParser(false));
            //先跳转到自动节点
            this.UpdateCurrentNode(instance, 4);
            instance.Run();
            Thread.Sleep(1000);
            Assert.AreEqual(1, _serverCount);
        }

        [Test(Description = "子流程活动调度测试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void SubProcess()
        {
            _process = this.CreateProcess(SequenceWorkflowParser.GetActivitySettings());
            TestHelper.SetProcessId(_process, Guid.NewGuid());
            var instance = WorkflowBuilder.CreateInstance(_process, new SequenceWorkflowParser(false));
            //先跳转到子流程节点
            this.UpdateCurrentNode(instance, 2);
            instance.Run();
            Thread.Sleep(1000);
            Assert.AreEqual(1, _subProcessCount);
            this.AssertWorkflowInstance(instance, 1, 0);
        }

        [Test(Description = "并行活动调度测试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Parallel()
        {

        }

        [Test(Description = "人工活动异常处理和重试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void FaultBookmark_Human()
        {
            _process = this.CreateProcess(SequenceWorkflowParser.GetActivitySettings());
            TestHelper.SetProcessId(_process, Guid.NewGuid());
            var instance = WorkflowBuilder.CreateInstance(_process, new SequenceWorkflowParser());
            instance.Run();
            Thread.Sleep(1000);
            //节点1执行人规则解析错误
            this.AssertWorkflowInstance(instance, 1, 1);

            var bookmark = instance.GetBookmarks()[0].Name;
            //错误书签
            Assert.IsTrue(bookmark.Contains("Error#"));

            //直接重试后仍错误
            instance.ResumeBookmark(bookmark, null);
            Thread.Sleep(1000);
            this.AssertWorkflowInstance(instance, 1, 2);
            //同一个位置再次错误书签相同
            Assert.AreEqual(bookmark, instance.GetBookmarks()[0].Name);

            //修复后重试
            (SequenceWorkflowParser.First.Helper as GetUsers).Scripts[0] = "originator";
            instance.ResumeBookmark(bookmark, null);
            Thread.Sleep(1000);
            //重试成功，生成人工任务和正常书签
            this.AssertWorkflowInstance(instance, 1, 2);
            Assert.AreNotEqual(bookmark, instance.GetBookmarks()[0].Name);
            Assert.IsFalse(instance.GetBookmarks()[0].Name.Contains("Error#"));
        }

        [Test(Description = "子流程活动异常处理和重试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void FaultBookmark_SubProcess()
        {
            _process = this.CreateProcess(SequenceWorkflowParser.GetActivitySettings());
            TestHelper.SetProcessId(_process, Guid.NewGuid());
            var instance = WorkflowBuilder.CreateInstance(_process, new SequenceWorkflowParser());
            //跳转到子流程-节点2
            this.UpdateCurrentNode(instance, 2);
            instance.Run();
            Thread.Sleep(1000);
            this.AssertWorkflowInstance(instance, 1, 0);
            //此时有一个子流程节点实例
            Assert.AreEqual(1, _subProcessCount);

            var bookmark = instance.GetBookmarks()[0].Name;

            //子流程结束后恢复子流程活动
            instance.ResumeBookmark(bookmark, null);
            Thread.Sleep(1000);
            //发生了错误
            this.AssertWorkflowInstance(instance, 1, 1);
            //书签复用
            Assert.AreEqual(bookmark, instance.GetBookmarks()[0].Name);
            //错误重试，应从callback处恢复
            instance.ResumeBookmark(bookmark, null);
            Thread.Sleep(1000);
            //始终只能有一个书签
            this.AssertWorkflowInstance(instance, 1, 2);
            //重试不能重新创建子流程节点实例
            Assert.AreEqual(1, _subProcessCount);

            //修复后重试
            SequenceWorkflowParser.Second.FinishRule["error"] = "true";
            instance.ResumeBookmark(bookmark, null);
            Thread.Sleep(1000);
            this.AssertWorkflowInstance(instance, 0, 2);
        }

        [Test(Description = "并行活动本身异常处理")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void FaultParallel()
        {
            _process = this.CreateProcess(ParallelWorkflowParser.GetActivitySettings());
            TestHelper.SetProcessId(_process, Guid.NewGuid());
            var instance = WorkflowBuilder.CreateInstance(_process, new ParallelWorkflowParser(true));
            instance.Run();
            Thread.Sleep(1000);
            //完成子流程触发并行子活动完成回调
            instance.ResumeBookmark(instance.GetBookmarks()[1].Name, null);
            Thread.Sleep(1000);
            //工作流中止异常
            Assert.IsNotNull(instance.AbortedException);
            Assert.AreEqual(ProcessStatus.Error, _process.Status);
        }

        [Test(Description = "并行活动的子活动异常处理和重试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void FaultBookmark_Parallel_Children()
        {
            _process = this.CreateProcess(ParallelWorkflowParser.GetActivitySettings());
            TestHelper.SetProcessId(_process, Guid.NewGuid());
            var instance = WorkflowBuilder.CreateInstance(_process, new ParallelWorkflowParser());
            instance.Run();
            Thread.Sleep(1000);

            //人工节点出错，子流程节点正常
            this.AssertWorkflowInstance(instance, 2, 1);

            //子流程结束后完成子流程节点
            instance.ResumeBookmark(instance.GetBookmarks()[1].Name, null);
            Thread.Sleep(1000);
            //子流程也发生了错误
            this.AssertWorkflowInstance(instance, 2, 2);

            //修复子流程
            ParallelWorkflowParser.SubProcess.FinishRule["error"] = "true";
            instance.ResumeBookmark(instance.GetBookmarks()[1].Name, null);
            Thread.Sleep(1000);
            this.AssertWorkflowInstance(instance, 1, 2);

            //修复人工节点
            (ParallelWorkflowParser.Human.Helper as GetUsers).Scripts[0] = "originator";
            instance.ResumeBookmark(instance.GetBookmarks()[0].Name, null);
            Thread.Sleep(1000);
            //仅存在一个人工活动书签
            this.AssertWorkflowInstance(instance, 1, 2);
        }

        private void UpdateCurrentNode(WorkflowInstance instance, int index)
        {
            instance.Update(new Dictionary<string, object>() { { WorkflowBuilder.Variable_CurrentNode, index } });
        }
        private void AssertWorkflowInstance(WorkflowInstance instance, int bookmark, int faults)
        {
            if (bookmark > 0)
                Assert.IsFalse(instance.IsComplete);
            else if (instance.AbortedException == null)
                Assert.IsTrue(instance.IsComplete);
            Assert.AreEqual(bookmark, instance.GetBookmarks().Count);
            Assert.AreEqual(faults, _faults);
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

            Process IProcessService.GetProcess(Guid id) { return _process; }
            void IProcessService.Update(Process process) { }
            void IProcessService.UpdateWorkflowInstanceOfProcess(Process process, Process.InternalWorkflowInstance instance) { }
            void IProcessService.CreateActivityInstance(ActivityInstanceBase instance)
            {
                if (instance is SubProcessActivityInstance)
                    _subProcessCount += 1;
                if (instance is ServerActivityInstance)
                    _serverCount += 1;
                if (instance is HumanActivityInstance)
                    _humanCount += 1;
            }
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
                yield return new SubProcessSetting(2, "subProcessTypeName", "子流程-节点2", null, null, false);
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
        //含有并行的流程core调度异常模拟
        public class ParallelWorkflowParser : IWorkflowParser
        {
            public static Parallel Parallel = null;
            public static Human Human = null;
            public static SubProcess SubProcess = null;
            public static IEnumerable<ActivitySetting> GetActivitySettings()
            {
                yield return new ParallelSetting(0, "并行节点", null, false);
                yield return new HumanSetting(0, "并行子节点1"
                    , new string[] { "完成" }, -1
                    , HumanSetting.SlotDistributionMode.AllAtOnce, "", null
                    , new HumanActionerRule("错误的执行人"), null, null, true);
                yield return new SubProcessSetting(0, "subProcessTypeName", "并行子节点2", null, null, true);
            }

            private string _completion, _finishRule, _actionerRule;
            public ParallelWorkflowParser() : this(false) { }
            public ParallelWorkflowParser(bool parallelError)
            {
                this._completion = parallelError ? "错误的并行完成规则" : "false";
                this._finishRule = parallelError ? "true" : "错误的子流程节点完成规则";
                this._actionerRule = parallelError ? "originator" : "错误的执行人";
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

                ParallelWorkflowParser.Human = WorkflowBuilder.CreateHuman(settings.ElementAt(1)
                    , "并行子节点1"
                    , new GetUsers(this._actionerRule)
                    , null);
                ParallelWorkflowParser.SubProcess = WorkflowBuilder.CreateSubProcess(settings.ElementAt(2)
                    , "并行子节点2"
                    , new Dictionary<string, string>() { { "error", this._finishRule } }
                    , null);

                flow.Body.StartNode = WorkflowBuilder.CreateParallel(settings.ElementAt(0)
                    , "并行节点"
                    , this._completion
                    , null
                    , ParallelWorkflowParser.Human
                    , ParallelWorkflowParser.SubProcess);
                Parallel = (flow.Body.StartNode as FlowStep).Action as Parallel;
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