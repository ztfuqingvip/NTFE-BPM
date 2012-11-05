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
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Registration;
using CodeSharp.Core;
using CodeSharp.Core.Castles;
using CodeSharp.Core.Services;
using Taobao.Workflow.Activities.Application;
using Taobao.Workflow.Activities.Hosting;
using NUnit.Framework;

namespace Taobao.Workflow.Activities.Test
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class BaseTest
    {
        protected IUserService _userService;
        protected IProcessService _processService;
        protected IProcessTypeService _processTypeService;
        protected IWorkItemService _workItemService;
        protected ITimeZoneService _timeZoneService;
        protected ISchedulerService _resumptionService;
        protected IScheduler _scheduler;
        protected Castle.Facilities.NHibernateIntegration.ISessionManager _sessionManager;
        protected ILog _log;
        protected Taobao.Workflow.Activities.Management.ITFlowEngine _managementApi;
        protected Taobao.Workflow.Activities.Client.ITFlowEngine _clientApi;

        [TestFixtureSetUp]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitialize]
        public void Config()
        {
            try
            {
                CodeSharp.Core.Configuration.ConfigWithEmbeddedXml(null
                    , "application_config"
                    , Assembly.GetExecutingAssembly()
                    , "Taobao.Workflow.Activities.Test.ConfigFiles")
                    .RenderProperties()
                    .Castle(o => this.Resolve(o.Container));
                //设置容器
                Taobao.Activities.ActivityUtilities.Container(new Taobao.Workflow.Activities.Application.Container());
                Taobao.Activities.Hosting.WorkflowInstance.IsEnableDebug = false;
            }
            catch (InvalidOperationException e)
            {
                if (!e.Message.Contains("不可重复初始化配置"))
                    Console.WriteLine(e.Message);
            }

            this._log = DependencyResolver.Resolve<ILoggerFactory>().Create(this.GetType());
            this._userService = DependencyResolver.Resolve<IUserService>();
            this._processService = DependencyResolver.Resolve<IProcessService>();
            this._processTypeService = DependencyResolver.Resolve<IProcessTypeService>();
            this._workItemService = DependencyResolver.Resolve<IWorkItemService>();
            this._timeZoneService = DependencyResolver.Resolve<ITimeZoneService>();
            this._resumptionService = DependencyResolver.Resolve<ISchedulerService>();
            this._scheduler = DependencyResolver.Resolve<IScheduler>();
            this._sessionManager = DependencyResolver.Resolve<Castle.Facilities.NHibernateIntegration.ISessionManager>();
            this._managementApi = DependencyResolver.Resolve<Taobao.Workflow.Activities.Management.ITFlowEngine>();
            this._clientApi = DependencyResolver.Resolve<Taobao.Workflow.Activities.Client.ITFlowEngine>();
        }
        [TestFixtureTearDown]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanup]
        public void Cleanup()
        {
            CodeSharp.Core.Configuration.Cleanup();
            this.EvictAll();
        }

        protected virtual void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            //优先注册测试桩
            windsor.Register(Component.For<IWorkflowParser>().ImplementedBy<Stub.WorkflowParser>());
            //人员库桩
            windsor.Register(Component.For<IUserHelper>().ImplementedBy<Stub.UserHelper>());
            //事件桩
            windsor.RegisterComponent(typeof(Stub.EventBus));

            //常规注册
            windsor.RegisterRepositories(Assembly.Load("Taobao.Workflow.Activities.Repositories"));
            windsor.RegisterServices(Assembly.Load("Taobao.Workflow.Activities"));
            windsor.RegisterComponent(Assembly.Load("Taobao.Workflow.Activities.Application"));
            windsor.RegisterComponent(Assembly.Load("Taobao.Workflow.Activities"));
            //应用调度器
            windsor.RegisterFromInterface(typeof(Taobao.Workflow.Activities.Hosting.Scheduler));
            //子流程创建调度辅助默认实现
            windsor.RegisterFromInterface(typeof(Taobao.Workflow.Activities.Hosting.SubProcessCreateWaitingResumption.DefaultSubProcessHelper));
            //升级规则调度辅助默认实现
            windsor.RegisterFromInterface(typeof(Taobao.Workflow.Activities.Hosting.HumanEscalationWaitingResumption.DefaultHumanEscalationHelper));
            //DLM zookeeper
            //windsor.ZookeeperDLM();
            //DLM mutex
            //windsor.RegisterComponent(typeof(CodeSharp.Core.DLM.MutexManager));
            //设计器转换器注册
            windsor.RegisterComponent(Assembly.Load("Taobao.Workflow.Activities.Converters"));
        }

        protected void EvictAll()
        {
            this._sessionManager.OpenSession().Clear();
            this._sessionManager.OpenSession().Close();
        }
        protected void Evict(object entity)
        {
            this._sessionManager.OpenSession().Evict(entity);
        }
        //protected void ClearRuntimeData()
        //{
        //    this._sessionManager.OpenStatelessSession().CreateSQLQuery("delete from NTFE_WaitingResumption").ExecuteUpdate();
        //    this._sessionManager.OpenStatelessSession().CreateSQLQuery("delete from NTFE_ActivityInstance").ExecuteUpdate();
        //    this._sessionManager.OpenStatelessSession().CreateSQLQuery("delete from NTFE_ProcessType").ExecuteUpdate();
        //    this._sessionManager.OpenStatelessSession().CreateSQLQuery("delete from NTFE_Activity").ExecuteUpdate();
        //    this._sessionManager.OpenStatelessSession().CreateSQLQuery("delete from NTFE_Process").ExecuteUpdate();
        //    this._sessionManager.OpenStatelessSession().CreateSQLQuery("delete from NTFE_ProcessDataField").ExecuteUpdate();
        //    this._sessionManager.OpenStatelessSession().CreateSQLQuery("delete from NTFE_WorkItem").ExecuteUpdate();
        //    this._sessionManager.OpenStatelessSession().CreateSQLQuery("delete from NTFE_ErrorRecord").ExecuteUpdate();
        //}
        protected void ClearRuntimeData(Process p)
        {
            this.ClearResumption(p);
            this._sessionManager.OpenStatelessSession()
                .CreateSQLQuery("delete from NTFE_WorkItem where ProcessId=:id")
                .SetGuid("id", p.ID)
                .ExecuteUpdate();
            this._sessionManager.OpenStatelessSession()
                .CreateSQLQuery("delete from NTFE_ErrorRecord where ProcessId=:id")
                .SetGuid("id", p.ID)
                .ExecuteUpdate();
        }
        protected void ClearResumption(Process p)
        {
            this._sessionManager.OpenStatelessSession()
                .CreateSQLQuery("delete from NTFE_WaitingResumption where ProcessId=:id")
                .SetGuid("id", p.ID)
                .ExecuteUpdate();
        }
        //同步执行调度器，可用于同步调度测试以及效率、性能验证
        protected void SyncScheduleOnce(Process p)
        {
            TestHelper.Resume(DependencyResolver.Resolve<Scheduler>()
                , this._resumptionService.GetValidWaitingResumptions(p)
                .Select(o => o.ID)
                .OrderBy(o => o));
        }
        //同步循环执行调度器 直到没有调度项或出现延迟调度
        protected void SyncScheduleUntil(Process p)
        {
            do
            {
                var list = this._resumptionService.GetValidWaitingResumptions(p).OrderBy(o => o.ID);
                if (list.Count() == 0
                    || !list.First().CanResumeAtNow)
                    break;
                //为防范测试环境下的无限制重试错误
                if (list.Any(o => o.IsError))
                    throw new InvalidOperationException("不应在调度中出现已经发生错误的调度项");

                TestHelper.Resume(DependencyResolver.Resolve<Scheduler>()
                    , list.Select(o => o.ID).OrderBy(o => o));
            } while (true);
        }

        //本测试项目使用的默认流程定义，大多数测试基于此，需要定制请单独编写
        public static readonly string SubFlag = "Sub";
        public static readonly string SubProcessTypeName = "UnitTest-" + SubFlag;
        //子流程节点在流程图中的索引
        public static readonly int SubProcessNodeIndex = 13;
        public static readonly string SubProcessNode = "节点8";
        //变量
        public static readonly string VariableUser = "username1";
        //EscalationRule测试使用
        public static readonly double EscalationExpiration = 0.05;
        public static readonly string EscalationNotifyFlag = "Notify";
        public static readonly string EscalationRedirectFlag = "Redirect";
        public static readonly string EscalationGotoFlag = "Goto";
        public static readonly string EscalationRedirectToUserName = "chong.xuc";
        public static readonly string EscalationGotoActivity = "节点2";

        protected static readonly string Originator = "houkun";
        protected static readonly string UnitTestType = "UnitTest";
        protected static readonly string Group = "UnitTest";
        protected static readonly string Node1 = "节点1";
        protected static readonly int Node1Index = 0;
        protected static readonly string Node2 = "节点2";
        protected static readonly int Node2Index = 2;
        protected Process CreateProcess()
        {
            return this.CreateProcess(Originator);
        }
        protected Process CreateProcess(ProcessType type)
        {
            return this.CreateProcess(type, null);
        }
        protected Process CreateProcess(ProcessType type, IDictionary<string, string> dict)
        {
            return this.CreateProcess(Originator, type, dict);
        }
        protected Process CreateProcess(string originator)
        {
            return this.CreateProcess(originator, UnitTestType);
        }
        protected Process CreateProcess(string originator, string typeName)
        {
            return this.CreateProcess(originator, typeName, null);
        }
        protected Process CreateProcess(string originator, string typeName, IDictionary<string, string> dict)
        {
            return this.CreateProcess(originator, this.CreateProcessType(typeName),dict);
        }
        protected Process CreateProcess(string originator, ProcessType type, IDictionary<string, string> dict)
        {
            var process = new Process("From UnitTest at " + DateTime.Now
                  , type
                  , this._userService.GetUserWhatever(originator)
                  , 0
                  , dict);
            this._processService.Create(process);
            //总是先evict避免之后的session问题
            this.Evict(process);
            process = this._processService.GetProcess(process.ID);
            return process;
        }
        protected ProcessType CreateProcessType(string typeName)
        {
            //创建默认的测试
            var type = new ProcessType(typeName
                , new ProcessType.WorkflowDefinition(string.Empty)
                , Stub.WorkflowParser.GetActivitySettings(typeName).ToArray()) { Group = Group };
            this._processTypeService.Create(type);
            return type;
        }
        protected User GetUser(string user)
        {
            return this._userService.GetUserWhatever(user);
        }
    }
}