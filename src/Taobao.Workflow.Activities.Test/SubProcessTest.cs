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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeSharp.Core.Services;
using Castle.MicroKernel.Registration;
using System.Threading;
using Taobao.Workflow.Activities.Statements;
using Taobao.Activities.Statements;
using Taobao.Workflow.Activities.Application;
using Taobao.Workflow.Activities.Management;
using Taobao.Workflow.Activities.Converters;

namespace Taobao.Workflow.Activities.Test
{
    [TestClass]
    public class SubProcessTest : BaseTest
    {
        [TestMethod]
        public void RunParentProcessTemplate()
        {
            var flowEngine = DependencyResolver.Resolve<ITFlowEngine>();
            flowEngine.CreateProcessType("UnitTestTypeTemplate"
                , Resource.WorkflowDefinition7
                , Resource.Settings7
                , ""
                , "");

            ProcessType parentProcessType = _processTypeService.GetProcessType("UnitTestTypeTemplate");

            User user = this._userService.GetUserWhatever("chong.xuc");
            WorkItem wi = null;

            // 创建父流程实例
            var parentProcess = new Process("From UnitTest at " + DateTime.Now,
                parentProcessType,
                user);
            this._processService.Create(parentProcess);

            // 记录父流程实例编号，以便后续使用
            Guid parentProcessId = parentProcess.ID;

            // 运行父流程实例
            this._scheduler.Run();
            Thread.Sleep(5000);

            // 流程节点1
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                parentProcess = this._processService.GetProcess(parentProcessId);
                wi = _workItemService.GetWorkItems(user, parentProcess, null).First();
                Assert.AreEqual("节点1", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "同意", null);
                r.Flush();
            }
            Thread.Sleep(5000);
            // 流程节点2
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                parentProcess = this._processService.GetProcess(parentProcessId);
                wi = _workItemService.GetWorkItems(user, parentProcess, null).First();
                Assert.AreEqual("节点2", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "同意", null);
                r.Flush();
            }
            Thread.Sleep(5000);
            // 流程节点3
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                parentProcess = this._processService.GetProcess(parentProcessId);
                wi = _workItemService.GetWorkItems(user, parentProcess, null).First();
                Assert.AreEqual("节点3", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "同意", null);
                r.Flush();
            }
            Thread.Sleep(5000);

            this._scheduler.Stop();
            Thread.Sleep(1000);
        }

        [TestMethod]
        public void RunParentProcess()
        {
            //// 创建父流程定义模板
            //var flowEngine = DependencyResolver.Resolve<ITFlowEngine>();
            //flowEngine.CreateProcessType("UnitTestTypeParentProcessTemplate"
            //    , Resource.WorkflowDefinition7
            //    , Resource.RecordsXml7
            //    , ""
            //    , "");

            //// 获取 UnitTestTypeSubProcessTemplate 流程定义中的所有设置的副本
            //ProcessType parentProcessTemplate = _processTypeService.GetProcessType("UnitTestTypeParentProcessTemplate");
            //List<ActivitySetting> modifiedActivitySettings = new List<ActivitySetting>();
            //foreach (ActivitySetting setting in parentProcessTemplate.ActivitySettings)
            //{
            //    modifiedActivitySettings.Add(setting.Clone());
            //}

            //// 把 节点2 的相关设置去掉，改为 SubProcessSetting
            //modifiedActivitySettings.RemoveAt(1);
            //modifiedActivitySettings.Insert(1, new SubProcessSetting(2, "UnitTestTypeSubProcess", "节点2", null, null, false));

            //// 创建父流程定义
            //var parentProcessType = new ProcessType("UnitTestTypeParentProcess",
            //    new ProcessType.WorkflowDefinition(Resource.WorkflowDefinition7),
            //    modifiedActivitySettings.ToArray());
            //this._processTypeService.Create(parentProcessType);

            //// 创建名为 UnitTestTypeSubProcess 的流程定义，作为子流程定义
            //ProcessType subProcessType = new ProcessType("UnitTestTypeSubProcess"
            //    , new ProcessType.WorkflowDefinition(Resource.WorkflowDefinition4)
            //    , DependencyResolver.Resolve<IWorkflowParser>().ParseActivitySettings(Resource.WorkflowDefinition4, Resource.RecordsXml4));
            //this._processTypeService.Create(subProcessType);
            
            var flowEngine = DependencyResolver.Resolve<ITFlowEngine>();
            flowEngine.CreateProcessType("UnitTestTypeParentProcess"
                , Resource.UnitTestTypeParentProcess_Workflow
                , Resource.UnitTestTypeParentProcess_Settings
                , "ParentProcess"
                , "ParentProcess");
            var parentProcessType = this._processTypeService.GetProcessType("UnitTestTypeParentProcess");

            flowEngine.CreateProcessType("ClientTest1"
                , Resource.UnitTestTypeSubProcess_Workflow
                , Resource.UnitTestTypeSubProcess_Settings
                , "SubProcess"
                , "SubProcess");

            var subProcessType = this._processTypeService.GetProcessType("ClientTest1");

            WorkItem wi = null;

            var user = this._userService.GetUserWhatever("chong.xuc");

            // 创建父流程实例
            var parentProcess = new Process("From UnitTest at " + DateTime.Now,
                parentProcessType,
                user);
            this._processService.Create(parentProcess);

            // 记录父流程实例编号，以便后续使用
            Guid parentProcessId = parentProcess.ID;

            // 运行父流程实例
            this._scheduler.Run();
            Thread.Sleep(5000);

            // 父流程节点1
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                parentProcess = this._processService.GetProcess(parentProcessId);
                wi = _workItemService.GetWorkItems(user, parentProcess, null).First();
                Assert.AreEqual("节点1", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "同意", new Dictionary<string, string> { { "ParentVariable", "1" } });
                r.Flush();
            }
            Thread.Sleep(5000);

            Process subProcess = null;
            Guid subProcessId = Guid.Empty;

            // 父流程节点2
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                long subProcessCount = 0;
                IList<Process> subProcesses = this._processService.GetProcesses(
                    NHibernate.Criterion.DetachedCriteria.For<Process>().Add(
                    NHibernate.Criterion.Expression.Eq("ProcessType",subProcessType)), 0, 10, out subProcessCount)
                    .ToList();
                if (subProcessCount > 0)
                {
                    subProcess = subProcesses[0];
                    subProcessId = subProcess.ID;
                }

                user = this._userService.GetUserWhatever("wfservice");

                // 子流程节点1
                wi = _workItemService.GetWorkItems(user, subProcess, null).First();
                Assert.AreEqual("节点1", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "同意", null);
                r.Flush();
            }
            Thread.Sleep(5000);

            // 子流程节点2
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                subProcess = this._processService.GetProcess(subProcessId);
                wi = _workItemService.GetWorkItems(user, subProcess, null).First();
                Assert.AreEqual("节点2", wi.ActivityName);
                _workItemService.Execute(wi.ID, user, "同意", new Dictionary<string, string> { { "ParentVariable", "2" } });
                r.Flush();
            }
            Thread.Sleep(5000);

            user = this._userService.GetUserWhatever("chong.xuc");

            // 父流程节点3
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                parentProcess = this._processService.GetProcess(parentProcessId);
                wi = _workItemService.GetWorkItems(user, parentProcess, null).First();
                Assert.AreEqual("节点3", wi.ActivityName);
                Assert.AreEqual("2", parentProcess.GetDataFields().Where(p => p.Key == "ParentVariable").Select(o => o.Value).First());
                _workItemService.Execute(wi.ID, user, "同意", null);
                r.Flush();
            }
            Thread.Sleep(5000);

            this._scheduler.Stop();
            Thread.Sleep(1000);
        }

        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            windsor.Register(Component.For<IWorkflowParser>().ImplementedBy<WorkflowParserSubProcess>());
            base.Resolve(windsor);
        }

        public class WorkflowParserSubProcess : WorkflowParser
        {
            public WorkflowParserSubProcess(ILoggerFactory factory, IWorkflowConverterFactory workflowConverterFactory)
                : base(factory, workflowConverterFactory)
            {
            }

            public static IEnumerable<ActivitySetting> GetSubProcessActivitySettings()
            {
                //0
                yield return new HumanSetting(0
                    , "节点1"
                    , new string[] { "同意", "否决" }
                    , -1
                    , HumanSetting.SlotDistributionMode.AllAtOnce
                    , ""
                    , new StartRule(null, null, null)
                    , new HumanActionerRule("originator")
                    , new FinishRule(new Dictionary<string, string>() { { "同意", "all('同意')" } })
                    , null
                    , false);
                //2
                yield return new HumanSetting(2
                    , "节点2"
                    , new string[] { "完成" }
                    , 0
                    , HumanSetting.SlotDistributionMode.AllAtOnce
                    , ""
                    , new StartRule(null, 0.1, null)
                    , new HumanActionerRule("originator")
                    , new FinishRule(new Dictionary<string, string>() { { "同意", "all('同意')" } })
                    , null
                    , false);
                //4
                yield return new ServerSetting(4
                    , "节点3"
                    , "'1'"
                    , null
                    , null
                    , null
                    , false);
            }

            public static IEnumerable<ActivitySetting> GetParentProcessActivitySettings()
            {
                //0
                yield return new HumanSetting(0
                    , "节点1"
                    , new string[] { "同意", "否决" }
                    , -1
                    , HumanSetting.SlotDistributionMode.AllAtOnce
                    , ""
                    , new StartRule(null, null, null)
                    , new HumanActionerRule("originator")
                    , new FinishRule(new Dictionary<string, string>() { { "同意", "all('同意')" } })
                    , null
                    , false);
                //2
                yield return new SubProcessSetting(2
                    , "UnitTestTypeSubProcess"
                    , "节点2"
                    , new StartRule(null, 0.1, null)
                    , null
                    , false);
                //4
                yield return new HumanSetting(4
                    , "节点3"
                    , new string[] { "同意" }
                    , 0
                    , HumanSetting.SlotDistributionMode.AllAtOnce
                    , ""
                    , new StartRule(null, 0.1, null)
                    , new HumanActionerRule("originator")
                    , new FinishRule(new Dictionary<string, string>() { { "同意", "all('同意')" } })
                    , null
                    , false);

                //6
                yield return new ServerSetting(6
                    , "节点4"
                    , "'1'"
                    , null
                    , null
                    , null
                    , false);
            }

            public static IEnumerable<ActivitySetting> GetTemplateProcessActivitySettings()
            {
                //0
                yield return new HumanSetting(0
                    , "节点1"
                    , new string[] { "同意", "否决" }
                    , -1
                    , HumanSetting.SlotDistributionMode.AllAtOnce
                    , ""
                    , new StartRule(null, null, null)
                    , new HumanActionerRule("originator")
                    , new FinishRule(new Dictionary<string, string>() { { "同意", "all('同意')" } })
                    , null
                    , false);
                //2
                yield return new HumanSetting(2
                    , "节点2"
                    , new string[] { "同意" }
                    , 0
                    , HumanSetting.SlotDistributionMode.AllAtOnce
                    , ""
                    , new StartRule(null, 0.1, null)
                    , new HumanActionerRule("originator")
                    , null
                    , null
                    , false);                
                //4
                yield return new HumanSetting(4
                    , "节点3"
                    , new string[] { "同意" }
                    , 0
                    , HumanSetting.SlotDistributionMode.AllAtOnce
                    , ""
                    , new StartRule(null, 0.1, null)
                    , new HumanActionerRule("originator")
                    , null
                    , null
                    , false);

                //6
                yield return new ServerSetting(6
                    , "节点4"
                    , "'1'"
                    , null
                    , null
                    , null
                    , false);
            }

            #region IWorkflowParser 成员

            public WorkflowActivity Parse(string key, string workflowDefinition, IEnumerable<ActivitySetting> activitySettings)
            {
                //本测试工程的大部分测试均基于此工作流定义，需谨慎变更 禁止修改

                var flow = new Statements.WorkflowActivity();
                IEnumerable<ActivitySetting> settings;
                if (key.IndexOf("SubProcess") >= 0)
                {
                    //0 节点1 同意|否决
                    //1 节点2 完成 
                    //2 节点3 完成 

                    settings = GetSubProcessActivitySettings();

                    var third = WorkflowBuilder.CreateServer(settings.ElementAt(2)
                        , "节点3"
                        , "'1'"
                        , null
                        , null 
                        , flow.CustomActivityResult
                        , null
                        , null);
                    var second = WorkflowBuilder.CreateHuman(settings.ElementAt(1)
                        , "节点2"
                        , new GetUsers("originator")
                        , flow.CustomActivityResult
                        , new Dictionary<string, FlowNode>() { { "同意", third } }
                        , null);
                    flow.Body.StartNode = WorkflowBuilder.CreateHuman(settings.ElementAt(0)
                        , "节点1"
                        , new GetUsers("originator")
                        , flow.CustomActivityResult
                        , new Dictionary<string, FlowNode>() { { "同意", second } }
                        , null);
                }
                else if (key.IndexOf("ParentProcess") >= 0)
                {
                    //0 节点1 同意|否决
                    //1 节点2 完成 
                    //2 节点3 完成 
                    //3 节点4 完成

                    settings = GetParentProcessActivitySettings();

                    var forth = WorkflowBuilder.CreateServer(settings.ElementAt(3)
                        , "节点4"
                        , "'1'"
                        , null
                        , null
                        , flow.CustomActivityResult
                        , null
                        , null);
                    var third = WorkflowBuilder.CreateHuman(settings.ElementAt(2)
                        , "节点3"
                        , new GetUsers("originator")
                        , flow.CustomActivityResult
                        , new Dictionary<string, FlowNode>() { { "同意", forth } }
                        , null);
                    var second = WorkflowBuilder.CreateSubProcess(settings.ElementAt(1)
                        , "节点2"
                        , null
                        , flow.CustomActivityResult
                        , null
                        , third);
                    flow.Body.StartNode = WorkflowBuilder.CreateHuman(settings.ElementAt(0)
                        , "节点1"
                        , new GetUsers("originator")
                        , flow.CustomActivityResult
                        , new Dictionary<string, FlowNode>() { { "同意", second } }
                        , null);
                }
                else
                {
                    settings = GetTemplateProcessActivitySettings();

                    var forth = WorkflowBuilder.CreateServer(settings.ElementAt(3)
                        , "节点4"
                        , "'1'"
                        , null
                        , null
                        , flow.CustomActivityResult
                        , null
                        , null);
                    var third = WorkflowBuilder.CreateHuman(settings.ElementAt(2)
                        , "节点3"
                        , new GetUsers("originator")
                        , flow.CustomActivityResult
                        , new Dictionary<string, FlowNode>() { { "同意", forth } }
                        , null);
                    var second = WorkflowBuilder.CreateHuman(settings.ElementAt(1)
                        , "节点2"
                        , new GetUsers("originator")
                        , flow.CustomActivityResult
                        , new Dictionary<string, FlowNode>() { { "同意", third } }
                        , null);
                    flow.Body.StartNode = WorkflowBuilder.CreateHuman(settings.ElementAt(0)
                        , "节点1"
                        , new GetUsers("originator")
                        , flow.CustomActivityResult
                        , new Dictionary<string, FlowNode>() { { "同意", second } }
                        , null);
                }

                return flow;
            }

            public override string Parse(WorkflowActivity workflowActivity, string originalWorkflowDefinition)
            {
                return Resource.WorkflowDefinition4;
            }

            public override string Parse(IEnumerable<ActivitySetting> activitySettings)
            {
                return Resource.Settings4;
            }

            #endregion
        }
    }
}
