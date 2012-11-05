using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Castle.MicroKernel.Registration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Taobao.Infrastructure.Castles;
using Taobao.Infrastructure.Services;
using Taobao.Workflow.Activities.Hosting;
using Taobao.Workflow.Activities.Application;
using System.Diagnostics;
using NHibernate.Criterion;
using NHibernate.Cfg;
using System.Collections;
using Taobao.Activities;
using Taobao.Activities.Statements;
using Taobao.Workflow.Activities.Statements;
using Taobao.Workflow.Activities.Management;

namespace Taobao.Workflow.Activities.Test
{
    [TestClass]
    public class WorkflowTest : BaseTest
    {
        private string _typeName = "流程实例Test";

        #region 创建流程类型
        //[TestMethod]
        //public void Create()
        //{
        //    var workflowDefinition = Resource.WorkflowDefinition;
        //    var timeZone = this._timeZoneService.GetTimeZone("Default");

        //    //人工活动列表
        //    var humanSettings = new List<HumanSetting>()
        //    {
        //        new HumanSetting(0
        //            , "会签1"
        //            , new string[] { "同意", "否决" }
        //            , -1
        //            , "http://taobao-wf-dev01:1234"
        //            , new HumanStartRule(DateTime.Now.AddMinutes(-1), null, timeZone)
        //            , new HumanActionerRule(null)
        //            , new HumanFinishRule(new Dictionary<string, string>() { { "同意", "all('同意')" }, { "否决", "all('否决')" } })
        //            , new HumanEscalationRule(DateTime.Now.AddMinutes(0.5), null, timeZone))
        //        ,  new HumanSetting(2
        //            , "会签2"
        //            , new string[] { "同意", "否决" }
        //            , 2
        //            , "http://taobao-wf-dev01:1234"
        //            , new HumanStartRule(DateTime.Now.AddMinutes(-1), null, timeZone)
        //            , new HumanActionerRule(null)
        //            , new HumanFinishRule(new Dictionary<string, string>() { { "同意", "all('同意')" }, { "否决", "all('否决')" } })
        //            , new HumanEscalationRule(DateTime.Now.AddMinutes(0.5), null, timeZone))
        //        ,  new HumanSetting(4
        //            , "会签3"
        //            , new string[] { "同意", "否决" }
        //            , 1
        //            , "http://taobao-wf-dev01:1234"
        //            , new HumanStartRule(DateTime.Now.AddMinutes(-1), null, timeZone)
        //            , new HumanActionerRule(null)
        //            , new HumanFinishRule(new Dictionary<string, string>() { { "同意", "all('同意')" }, { "否决", "all('否决')" } })
        //            , new HumanEscalationRule(DateTime.Now.AddMinutes(0.5), null, timeZone))
        //    };
        //    var processType = new ProcessType(this._typeName
        //       , new ProcessType.WorkflowDefinition(workflowDefinition)
        //       , humanSettings.ToArray());
        //    //创建流程类型
        //    this._processTypeService.Create(processType);
        //    Assert.AreEqual("会签1", processType.GetHumanSetting(0).ActivityName);
        //    Assert.AreEqual("会签2", processType.GetHumanSetting(2).ActivityName);
        //    Assert.AreEqual("会签3", processType.GetHumanSetting(4).ActivityName);
        //    Assert.IsTrue(processType.IsCurrent); 
        //}
        #endregion

        /// <summary>
        /// 使用ITFlowEngine创建流程类型
        /// </summary>
        [TestMethod]
        public void Publish()
        {
            var flowEngine = DependencyResolver.Resolve<ITFlowEngine>();
            //创建流程类型
            flowEngine.CreateProcessType(this._typeName
                , Resource.WorkflowDefinition3
                , Resource.RecordsXml
                , "工作流程图");
            var processType = this._processTypeService.GetProcessType(this._typeName);
            Assert.AreEqual("会签1", processType.GetHumanSetting(0).ActivityName);
            Assert.AreEqual("会签2", processType.GetHumanSetting(2).ActivityName);
            Assert.AreEqual("会签3", processType.GetHumanSetting(4).ActivityName);
            Assert.IsTrue(processType.IsCurrent); 
        }
        /// <summary>
        /// 整个工作流程测试
        /// </summary>
        [TestMethod]
        public void Full()
        {
            //node1(会签1 1人 all(同意) getSuperior() 不包含StartRule)
            //->node2(会签2 1人 all(同意) getSuperior() 包含StartRule,At方式 default:node3)
            //->node3(会签3 1人 all(同意) getSuperior() 包含StartRule,At方式)

            var process = new Process("test" + DateTime.Now
              , this._processTypeService.GetProcessType(this._typeName)
              , this._userService.GetUser("houkun")
              , 0
              , new Dictionary<string, string>() { 
                    { "username1", "houkun" }
                    , { "username2", "xiaoxuan.lp" }
                    , { "username3", "houkun" }
                });
            TestHelper.ChangeProcessStatus(process, ProcessStatus.Running);
            //创建流程
            this._processService.Create(process);

            //流程ID
            Guid processId = process.ID;
            IList<WaitingResumption> resumptions = null;
            IList<WorkItem> workItems = null;

            using (var session = Taobao.Infrastructure.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                //流程应为New状态
                Assert.AreEqual(ProcessStatus.Running, process.Status);

                resumptions = this._resumptionRepository.GetWaitingResumptions(process);
                Assert.AreEqual(1, resumptions.Count);
                //当前包含ProcessStartResumption
                Assert.AreEqual(typeof(ProcessStartResumption), resumptions[0].GetType());
                this._scheduler.Resume((WaitingResumption)resumptions[0]);

                Thread.Sleep(1000);
                session.Flush();
            }     

            using (var session = Taobao.Infrastructure.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                process = this._processService.GetProcess(process.ID);
                //流程为运行状态
                Assert.AreEqual(ProcessStatus.Running, process.Status);

                //检查任务创建请求
                resumptions = this._resumptionRepository.GetWaitingResumptions(process);
                Assert.AreEqual(1, resumptions.Count);
                //当前包含WorkItemCreateResumption
                Assert.AreEqual(typeof(WorkItemCreateResumption), resumptions[0].GetType());
                this._scheduler.Resume((WaitingResumption)resumptions[0]);

                Thread.Sleep(1000);
                session.Flush();
            }
            using (var session = Taobao.Infrastructure.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                process = this._processService.GetProcess(processId);
                var resumption = this._resumptionRepository.FindBy(resumptions[0].ID);

                //流程为运行状态
                Assert.AreEqual(ProcessStatus.Active, process.Status);

                var activityInstanceId = (resumption as WorkItemCreateResumption).HumanActivityInstance.ActivityInstanceId;
                workItems = this.GetWorkItems(this._sessionManager.OpenSession(), process, activityInstanceId);
                Assert.AreEqual(1, workItems.Count);
            //    //当前工作任务状态为新建
                //Assert.AreEqual(WorkItemStatus.New, workItems[0].Status);
            //    session.Flush();
                
            }
            this._workItemService.Execute(workItems[0].ID, workItems[0].Actioner, "同意", null);
            

            //using (var session = Taobao.Infrastructure.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            //{
            //    process = this._processService.GetProcess(processId);
            //    //流程为活动状态
            //    Assert.AreEqual(ProcessStatus.Active, process.Status);

            //    resumptions = this._resumptionRepository.GetWaitingResumptions(process);
            //    Assert.AreEqual(1, resumptions.Count);
            //    Assert.AreEqual(typeof(HumanBookmakrResumption), resumptions[0].GetType());
            //    this._scheduler.Resume(resumptions[0]);

            //    Thread.Sleep(1000);
            //    session.Flush();
            //}
            //using (var session = Taobao.Infrastructure.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            //{
            //    process = this._processService.GetProcess(process.ID);
            //    Assert.AreEqual(ProcessStatus.Running, process.Status);

            //    resumptions = this._resumptionRepository.GetWaitingResumptions(process);
            //    Assert.AreEqual(1, resumptions.Count);
            //    Assert.AreEqual(typeof(WorkItemCreateResumption), resumptions[0].GetType());
            //    this._scheduler.Resume((WaitingResumption)resumptions[0]);

            //    Thread.Sleep(1000);
            //    session.Flush();
            //}
            //using (var session = Taobao.Infrastructure.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            //{
            //    process = this._processService.GetProcess(processId);
            //    var resumption = this._resumptionRepository.FindBy(resumptions[0].ID);

            //    //流程为运行状态
            //    Assert.AreEqual(ProcessStatus.Active, process.Status);

            //    TestHelper.ChangeProcessStatus(resumption.Process, ProcessStatus.Running);
            //    var activityInstanceId = (resumption as WorkItemCreateResumption).HumanTaskInfo.ActivityInstanceId;
            //    workItems = this._workItemService.GetWorkItems(process, activityInstanceId);
            //    Assert.AreEqual(1, workItems.Count);
            //    //当前工作任务状态为新建
            //    Assert.AreEqual(WorkItemStatus.New, workItems[0].Status);
            //    session.Flush();
            //}
            //this._workItemService.Execute(workItems[0].ID, workItems[0].Actioner, "同意", null);
        }
        //通过流程和活动实例编号获取工作任务
        private IList<WorkItem> GetWorkItems(NHibernate.ISession session, Process process, long activityInstanceId)
        {
            return session.CreateCriteria<WorkItem>()
                    .Add(Expression.Eq("Process", process))
                    .Add(Expression.Eq("ActivityInstanceId", activityInstanceId))
                    .List<WorkItem>();
        }

        #region BaseTest Members
        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            //优先注册测试桩
            windsor.Register(Component.For<IWorkflowParser>().ImplementedBy<Application.WorkflowParser>());
            windsor.Register(Component.For<IUserHelper>().ImplementedBy<Stub.UserHelper>());
            windsor.Register(Component.For<ITFlowEngine>().ImplementedBy<TFlowEngine>());

            windsor.RegisterRepositories(Assembly.Load("Taobao.Workflow.Activities.Repositories"));
            windsor.RegisterServices(Assembly.Load("Taobao.Workflow.Activities"));
            windsor.RegisterComponent(Assembly.Load("Taobao.Workflow.Activities.Application"));
            windsor.RegisterComponent(Assembly.Load("Taobao.Workflow.Activities"));
            windsor.RegisterComponent(Assembly.Load("Taobao.Workflow.Activities.Client"));
            //应用调度器
            windsor.RegisterFromInterface(typeof(Taobao.Workflow.Activities.Application.Scheduler));
        }
        #endregion
    }
}