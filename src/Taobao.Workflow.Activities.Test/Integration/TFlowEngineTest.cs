using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Castle.MicroKernel.Registration;
using CodeSharp.Core;
using CodeSharp.Core.Castles;
using CodeSharp.Core.Services;
using Taobao.Workflow.Activities.Application;
using Taobao.Workflow.Activities.Hosting;
using Taobao.Workflow.Activities.Management;
using Taobao.Workflow.Activities.Converters;
using NUnit.Framework;

namespace Taobao.Workflow.Activities.Test
{
    /// <summary>
    /// 针对TFlowEngine实现的半集成测试
    /// </summary>
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class TFlowEngineTest : BaseTest
    {
        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            //使用实际解析器
            windsor.RegisterComponent(typeof(WorkflowParser));
            base.Resolve(windsor);
        }
        [Test(Description = "流程类型创建")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void CreateProcessType()
        {
            var flowEngine = DependencyResolver.Resolve<ITFlowEngine>();
            flowEngine.CreateProcessType("Test"
                , Resource.WorkflowDefinition1
                , Resource.ActivitySettingsDefinitionNew1
                , "第一版设计器转换器测试"
                , Group);
            flowEngine.CreateProcessType("Test_New1"
                , Resource.WorkflowDefinitionNew1
                , Resource.ActivitySettingsDefinitionNew1
                , "第二版设计器转换器测试"
                , Group);
            flowEngine.CreateProcessType("Test_New2"
                , Resource.WorkflowDefinitionNew4
                , Resource.ActivitySettingsDefinitionNew4
                , "第二版设计器转换器测试"
                , Group);
        }

        [Test(Description = "是否允许动态追加人工节点")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void CanAppendHumanActivity()
        {
            //流程：节点1（多人任务节点 originator+user）->节点2（单人任务多分支节点）->节点3（单人任务单分支节点）->节点4
            var process = this.CreateProcess(this.CreateProcessType(UnitTestType + "DynamicWrong"
                , Resource.AppendHumanActivity_Workflow_Wrong
                , Resource.AppendHumanActivity_Settings_Wrong)
                , new Dictionary<string, string>() { { "user", "xuanfang" } });
            this.Evict(process);
            process = this._processService.GetProcess(process.ID);

            //调度后进入多人任务节点1
            this.SyncScheduleUntil(process);
            //多人任务节点不允许
            Assert.IsFalse(this._clientApi.CanAppendHumanActivity(process.ID, Client.AppendHumanMode.Wait, _dynamicActivityName).Result);
            Assert.IsFalse(this._clientApi.CanAppendHumanActivity(process.ID, Client.AppendHumanMode.Continues, _dynamicActivityName).Result);

            //跳转到多分支节点2
            this.GoAndScheduleTo(process, 2);
            //多分支节点 Wait模式允许
            Assert.IsTrue(this._clientApi.CanAppendHumanActivity(process.ID, Client.AppendHumanMode.Wait, _dynamicActivityName).Result);
            //Continues不允许
            Assert.IsFalse(this._clientApi.CanAppendHumanActivity(process.ID, Client.AppendHumanMode.Continues, _dynamicActivityName).Result);

            //跳转到单人任务单分支节点3
            this.GoAndScheduleTo(process, 4);
            Assert.IsTrue(this._clientApi.CanAppendHumanActivity(process.ID, Client.AppendHumanMode.Wait, _dynamicActivityName).Result);
            Assert.IsTrue(this._clientApi.CanAppendHumanActivity(process.ID, Client.AppendHumanMode.Continues, _dynamicActivityName).Result);
        }

        [Test(Description = "动态追加人工节点-Wait")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void AppendHumanActivity_Wait()
        {
            this.AppendHumanActivity(Client.AppendHumanMode.Wait, true);
        }

        [Test(Description = "动态追加人工节点-Continues 执行后继续")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void AppendHumanActivity_Continues_True()
        {
            this.AppendHumanActivity(Client.AppendHumanMode.Continues, true);
        }

        [Test(Description = "动态追加人工节点-Continues 执行后结束流程")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void AppendHumanActivity_Continues_False()
        {
            this.AppendHumanActivity(Client.AppendHumanMode.Continues, false);
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        [Ignore]
        public void GetErrors()
        {
            this._managementApi.GetErrors();
        }

        private static readonly string _activityName1 = "节点1";
        private static readonly string _activityName2 = "节点2";
        private string _enterAction = "同意并加签/要求回答";
        private string _action1 = "同意";
        private string _action2 = "否决";
        private string _dynamicActivityName = "动态节点";
        private void AppendHumanActivity(Client.AppendHumanMode mode, bool flag)
        {
            //流程：节点1（单人任务节点）->节点2
            var process = this.CreateProcess(this.CreateProcessType(UnitTestType + "Dynamic"
                , Resource.AppendHumanActivity_Workflow
                , Resource.AppendHumanActivity_Settings));

            //调度后进入节点1
            this.SyncScheduleUntil(process);
            Assert.AreEqual(_activityName1, TestHelper.GetCurrentActivityName(process));

            //人工单人任务节点允许
            Assert.IsTrue(this._clientApi.CanAppendHumanActivity(process.ID, mode, _dynamicActivityName).Result);

            //避免出现相同版本的流程类型
            Thread.Sleep(1000);

            //追加
            this._clientApi.AppendHumanActivity(process.ID, mode, this.CreateAppendHumanSetting(mode));
            //执行任务
            var w = this._workItemService.GetWorkItems(process).First();
            this._workItemService.Execute(w.ID, w.Actioner, this._enterAction, null);

            //调度后进入动态节点
            this.SyncScheduleUntil(process);
            Assert.AreEqual(this._dynamicActivityName, TestHelper.GetCurrentActivityName(process));
            //将动态节点任务全部执行完
            this._workItemService.GetWorkItems(process).ToList().ForEach(o =>
            {
                Assert.AreEqual(this._dynamicActivityName, o.ActivityName);
                this._workItemService.Execute(o.ID
                    , o.Actioner
                    //根据flag决定是否激活完成规则
                    , flag ? this._action1 : this._action2
                    , null);
            });

            this.SyncScheduleUntil(process);

            //流程结束
            if (!flag)
            {
                Assert.AreEqual(ProcessStatus.Completed, process.Status);
                return;
            }
            //调度后返回节点1
            if (mode == Client.AppendHumanMode.Wait)
                Assert.AreEqual(_activityName1, TestHelper.GetCurrentActivityName(process));
            //调度后进入节点2
            if (mode == Client.AppendHumanMode.Continues)
                Assert.AreEqual(_activityName2, TestHelper.GetCurrentActivityName(process));
        }
        private Client.AppendHumanSetting CreateAppendHumanSetting(Client.AppendHumanMode mode)
        {
            var setting = new Client.AppendHumanSetting();
            setting.ActionerRule = new string[] { "'houkun'", "'xuanfang'" };
            setting.Actions = new string[] { this._action1, this._action2 };
            setting.ActivityName = this._dynamicActivityName;
            setting.EnterAction = this._enterAction;
            setting.EnterFinishRuleName = this._enterAction;
            if (mode == Client.AppendHumanMode.Continues)
            {
                setting.FinishRuleName = "动态节点执行完成";
                setting.FinishRuleBody = string.Format("all('{0}')", _action1);
            }
            setting.Url = "aspx";
            return setting;
        }
        private ProcessType CreateProcessType(string typeName, string workflowDefinition, string activitySettingsDefinition)
        {
            var type = new ProcessType(typeName
                , new ProcessType.WorkflowDefinition(workflowDefinition)
                , DependencyResolver.Resolve<IWorkflowParser>().ParseActivitySettings(workflowDefinition, activitySettingsDefinition));
            this._processTypeService.Create(type);
            return type;
        }
        private void GoAndScheduleTo(Process process, int index)
        {
            this.ClearRuntimeData(process);
            TestHelper.ChangeProcessStatus(process, ProcessStatus.Running);
            TestHelper.UpdateCurrentNode(process, index);
            var start = new ProcessStartResumption(process);
            this._resumptionService.Add(start);
            this.SyncScheduleUntil(process);
        }

        /*
         *     public enum FlowEngineTestType
    {
        /// <summary>
        /// 迁移到新节点，然后迁移到当前节点的下一个节点
        /// </summary>
        AppendAuditor,

        /// <summary>
        /// 迁移到新节点，然后回迁到当前节点
        /// </summary>
        RequestMore,
    }

    public static class FlowEngineTestHelper
    {
        /// <summary>
        /// 测试类型
        /// </summary>
        public static FlowEngineTestType Type { get; set; }
    }
        [Test(Description = "动态追加人工节点-Wait")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void RequestMore()
        {
            FlowEngineTestHelper.Type = FlowEngineTestType.RequestMore;

            Taobao.Workflow.Activities.Client.ITFlowEngine flowEngine;
            WorkItem wi = null;

            var user = this._userService.GetUserWhatever("chong.xuc");

            Process process = CreateProcess();
            Guid processId = process.ID;

            this._scheduler.Run();
            Thread.Sleep(5000);

            // 原始流程定义中包括3个节点：节点1 -〉节点2 -〉结束

            // 在节点1 和 节点2 之间插入 节点4
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                process = this._processService.GetProcess(processId);

                Assert.AreEqual(ProcessStatus.Active, process.Status);
                Dictionary<string, string> finishRuleScripts = new Dictionary<string, string>();
                finishRuleScripts.Add("同意", "true");
                flowEngine = DependencyResolver.Resolve<Taobao.Workflow.Activities.Client.ITFlowEngine>();
                flowEngine.AppendHumanActivity(process.ID,
                    "节点1",
                    "节点1",
                    new Client.AppendHumanSetting() { ActionerRule = new Client.HumanActionerRule() { Scripts = new string[] { "'chong.xuc'" } }, FinishRule = new Client.FinishRule() { Scripts = finishRuleScripts }, ActivityName = "节点4", Actions = new string[] { "同意" } });
                r.Flush();
            }

            Thread.Sleep(5000);

            // 执行 节点1 人工任务
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                process = this._processService.GetProcess(processId);
                wi = _workItemService.GetWorkItems(user, process, null).First();
                Assert.AreEqual("节点1", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "RuntimeAppendedAction", null);
                r.Flush();
            }
            Thread.Sleep(5000);

            // 此时应该已经为 节点4 创建人工任务
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                process = this._processService.GetProcess(processId);
                wi = _workItemService.GetWorkItems(user, process, null).First();
                Assert.AreEqual("节点4", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "同意", null);
                r.Flush();
            }
            Thread.Sleep(5000);

            // 返回 节点1 继续执行
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                process = this._processService.GetProcess(processId);
                wi = _workItemService.GetWorkItems(user, process, null).First();
                Assert.AreEqual("节点1", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "同意", null);
                r.Flush();
            }
            Thread.Sleep(5000);

            // 执行 节点2 的人工任务，流程结束
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                process = this._processService.GetProcess(processId);
                wi = _workItemService.GetWorkItems(user, process, null).First();
                Assert.AreEqual("节点2", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "同意", null);
                r.Flush();
            }

            this._scheduler.Stop();
            Thread.Sleep(1000);
        }

        [Test(Description = "动态追加人工节点-Continues")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void AppendAuditor()
        {
            FlowEngineTestHelper.Type = FlowEngineTestType.AppendAuditor;

            Taobao.Workflow.Activities.Client.ITFlowEngine flowEngine;
            WorkItem wi = null;

            var user = this._userService.GetUserWhatever("chong.xuc");

            Process process = CreateProcess();
            Guid processId = process.ID;

            this._scheduler.Run();
            Thread.Sleep(5000);

            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                process = this._processService.GetProcess(processId);

                Assert.AreEqual(ProcessStatus.Active, process.Status);
                Dictionary<string, string> finishRuleScripts = new Dictionary<string, string>();
                finishRuleScripts.Add("同意", "true");
                flowEngine = DependencyResolver.Resolve<Taobao.Workflow.Activities.Client.ITFlowEngine>();
                flowEngine.AppendHumanActivity(process.ID,
                    "节点1",
                    "节点2",
                    new Client.AppendHumanSetting() { ActionerRule = new Client.HumanActionerRule() { Scripts = new string[] { "'chong.xuc'" } }, FinishRule = new Client.FinishRule() { Scripts = finishRuleScripts }, ActivityName = "节点4", Actions = new string[] { "同意" } });
                r.Flush();
            }

            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                process = this._processService.GetProcess(processId);
                wi = _workItemService.GetWorkItems(user, process, null).First();
                Assert.AreEqual("节点1", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "RuntimeAppendedAction", null);
                r.Flush();
            }
            Thread.Sleep(5000);

            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                process = this._processService.GetProcess(processId);
                wi = _workItemService.GetWorkItems(user, process, null).First();
                Assert.AreEqual("节点4", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "同意", null);
                r.Flush();
            }
            Thread.Sleep(5000);

            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                process = this._processService.GetProcess(processId);
                wi = _workItemService.GetWorkItems(user, process, null).First();
                Assert.AreEqual("节点2", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "同意", null);
                r.Flush();
            }

            this._scheduler.Stop();
            Thread.Sleep(1000);
        }
        
        protected Process CreateProcess()
        {
            return this.CreateProcess("chong.xuc");
        }
        protected Process CreateProcess(string originator)
        {
            return this.CreateProcess(originator, UnitTestType);
        }
        protected Process CreateProcess(string originator, string typeName)
        {
            var type = this.CreateProcessType(typeName);

            var process = new Process("From UnitTest at " + DateTime.Now
              , type
              , this._userService.GetUserWhatever(originator));
            this._processService.Create(process);
            return process;
        }
        protected ProcessType CreateProcessType(string typeName)
        {
            var type = new ProcessType(typeName
                , new ProcessType.WorkflowDefinition(Resource.WorkflowDefinition4)
                , DependencyResolver.Resolve<IWorkflowParser>().ParseActivitySettings(Resource.WorkflowDefinition4, Resource.RecordsXml4));
            this._processTypeService.Create(type);
            return type;
        }

        public class WorkflowParserAppendActivity : WorkflowParser
        {
            public WorkflowParserAppendActivity(ILoggerFactory factory, ITimeZoneService timeZoneService, IWorkflowConverterFactory _workflowConverterFactory)
                : base(factory, timeZoneService, _workflowConverterFactory)
            {
            }

            public override string Parse(Statements.WorkflowActivity activity)
            {
                if (FlowEngineTestHelper.Type == FlowEngineTestType.RequestMore)
                {
                    return Resource.WorkflowDefinition5;
                }
                else
                {
                    return Resource.WorkflowDefinition6;
                }
            }
        }
         */
    }
}