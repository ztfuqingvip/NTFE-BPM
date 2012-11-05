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
using Castle.Services.Transaction;

using CodeSharp.Core.RepositoryFramework;
using Taobao.Activities;
using Taobao.Activities.Hosting;
using Taobao.Activities.Interfaces;
using Taobao.Workflow.Activities.Statements;
using Taobao.Workflow.Activities.Hosting;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 提供工作流持久化实现
    /// </summary>
    [CodeSharp.Core.Component]
    public class WorkflowInstanceStore : DefaultInstanceStore, IInstanceStore
    {
        private ILog _log;
        private IProcessService _processService;
        private WorkflowInstanceStoreHelper _helper;

        public WorkflowInstanceStore(ILoggerFactory factory, IProcessService processService, WorkflowInstanceStoreHelper helper)
        {
            this._log = factory.Create(typeof(WorkflowInstanceStore));
            this._processService = processService;
            this._helper = helper;
        }

        //HACK:若出现无法序列化的类型，可根据需要在此增加序列化KnownType
        protected override IEnumerable<Type> GetKnownTypes()
        {
            return base.GetKnownTypes();
        }
        protected override string InternalLoad(Guid id)
        {
            return this._processService.GetWorkflowInstance(id).Serialized;
        }
        protected override void InternalSave(WorkflowInstance instance, string serialized)
        {
            this._helper.Save(instance, serialized);
        }
    }

    //为WorkflowInstanceStore启用事务管理而设计的helper
    [CodeSharp.Core.Component]
    [Transactional]
    public class WorkflowInstanceStoreHelper
    {
        private ILog _log;
        private IProcessService _processService;
        private ISchedulerService _schedulerService;
        private IEventBus _bus;

        public WorkflowInstanceStoreHelper(ILoggerFactory factory
            , IProcessService processService
            , IUserService userService
            , ISchedulerService schedulerService
            , IEventBus bus)
        {
            this._log = factory.Create(typeof(WorkflowInstanceStoreHelper));
            this._processService = processService;
            this._schedulerService = schedulerService;
            this._bus = bus;
        }

        [Transaction(TransactionMode.Requires)]
        public virtual void Save(WorkflowInstance instance, string serialized)
        {
            //HACK:以下逻辑不允许出现任何未捕获异常，一旦异常均是不可恢复异常，属于引擎或流程故障，因此只能在此包含简单直接逻辑

            try
            {
                var process = this._processService.GetProcess(instance.ID);

                if (process == null)
                    throw new InvalidOperationException("严重错误，流程#" + instance.ID + "不存在");

                var customExtension = instance.Extensions.Find<CustomExtension>();
                var dataFieldExtension = instance.Extensions.Find<DataFieldExtension>();
                var huamanExtension = instance.Extensions.Find<HumanExtension>();
                var serverExtension = instance.Extensions.Find<ServerExtension>();
                var parallelExtension = instance.Extensions.Find<ParallelExtension>();
                var subProcessExtension = instance.Extensions.Find<SubProcessExtension>();

                IEnumerable<CustomExtension.FaultBookmark> faults = customExtension.GetFaultBookmarks();
                IEnumerable<CustomExtension.DelayBookmark> delays = customExtension.GetDelayBookmarks();
                IEnumerable<HumanActivityInstance> humans = huamanExtension.GetHumanActivityInstances();
                IEnumerable<ServerActivityInstance> servers = serverExtension.GetServerActivityInstances();
                IEnumerable<long> cancelled = parallelExtension.GetCancelledActivityInstances();
                IEnumerable<SubProcessActivityInstance> subProcesses = subProcessExtension.GetSubProcessActivityInstance();

                //TODO:将该异常明细记录至db便于反馈
                //HACK:【重要】工作流中止异常，Core调度过程中发生的未知异常
                //由于调整为细粒度错误重试支持，通常不会有工作流中止异常出现，可能出现该异常情况有：并行节点主体会回调执行异常或调度过程发生未知异常
                if (instance.AbortedException != null)
                {
                    process.MarkAsError(instance.AbortedException);
                    this._log.Error(string.Format("流程“{0}”#{1}发生工作流中止异常"
                        , process.Title
                        , process.ID)
                        , instance.AbortedException);
                }
                else if (instance.IsComplete)
                    process.ChangeStatus(ProcessStatus.Completed);
                else
                    //一律置为runing
                    process.ChangeStatus(ProcessStatus.Running);

                //更新工作流实例树
                this._processService.UpdateWorkflowInstanceOfProcess(process, new Process.InternalWorkflowInstance(serialized));
                //更新流程变量到Process
                process.UpdateDataFields(dataFieldExtension.DataFields);
                //HACK:【重要】Core调度完成后更新当前节点索引
                process.UpdateCurrentNode(dataFieldExtension.CurrentNode);
                this._processService.Update(process);

                #region 错误书签
                foreach (var f in faults)
                {
                    this._schedulerService.AddErrorRecord(new FaultBookmarkRecord(process, f.Reason, f.Name, f.ActivityName));
                    this._log.InfoFormat("由于“{0}”为节点“{1}”创建错误书签“{2}”记录", f.Reason.Message, f.ActivityName, f.Name);
                }
                customExtension.ClearFault();
                #endregion

                #region 延迟书签
                foreach (var d in delays)
                {
                    this._schedulerService.Add(new BookmarkResumption(process, d.ActivityName, d.Name, null, d.At));
                    this._log.InfoFormat("为节点“{0}”创建延迟书签“{1}”调度请求，将于{2}恢复", d.ActivityName, d.Name, d.At);
                }
                customExtension.ClearDelay();
                #endregion

                #region 取消节点实例
                foreach (var id in cancelled)
                {
                    var a = this._processService.GetActivityInstanceByWorkflowActivityInstanceId(process, id);
                    if (a == null)
                        this._log.WarnFormat("没有找到流程“{0}”#{1}的工作流节点实例#{2}，将造成该节点的运行时信息无法被取消"
                            , process.Title
                            , process.ID
                            , id);
                    else
                        //生成节点实例取消调度项，缩小粒度和避免失败，但可能造成节点取消的及时性
                        this._schedulerService.Add(new ActivityInstanceCancelResumption(process, a));
                    //this._processService.CancelAllAbout(process, a);
                }
                parallelExtension.Clear();
                #endregion

                #region 创建人工节点实例
                foreach (var h in humans)
                {
                    this._processService.CreateActivityInstance(h);
                    //生成任务创建请求
                    this._schedulerService.Add(new WorkItemCreateResumption(process, h));
                    this._log.InfoFormat("为人工节点“{0}”创建人工节点实例#{1}以及人工任务调度项，书签名为“{2}”", h.ActivityName, h.ID, h.ReferredBookmarkName);

                    //根据超时升级规则创建调度
                    var rule = process.ProcessType.GetHumanSetting(h.ActivityName).EscalationRule;
                    if (rule != null && rule.IsValid)
                    {
                        var at = DateTime.Now.AddMinutes(rule.ExpirationMinutes.Value);
                        this._schedulerService.Add(new HumanEscalationResumption(process, at, h));
                        this._log.InfoFormat("创建超时事件升级调度项，将于{0}激活", at);
                    }
                }
                //从扩展中清空
                huamanExtension.Clear();
                #endregion

                #region 创建自动节点实例
                foreach (var s in servers)
                {
                    this._processService.CreateActivityInstance(s);
                    this._log.InfoFormat("创建Server节点“{0}”#{1}", s.ActivityName, s.ID);
                }
                //从扩展中清空
                serverExtension.Clear();
                #endregion

                #region 创建子流程节点实例
                foreach (var s in subProcesses)
                {
                    //保存节点实例信息
                    this._processService.CreateActivityInstance(s);
                    //生成子流程创建请求
                    this._schedulerService.Add(new SubProcessCreateResumption(process, s));
                    this._log.InfoFormat("为子流程节点“{0}”创建子流程节点实例#{1}以及子流程调度项，书签名为“{2}”", s.ActivityName, s.ID, s.ReferredBookmarkName);
                }
                //从扩展中清空
                subProcessExtension.Clear();
                #endregion

                #region 子流程完成
                Process parent;
                if (process.Status == ProcessStatus.Completed
                    && process.ParentProcessId.HasValue
                    && (parent = this._processService.GetProcess(process.ParentProcessId.Value)) != null)
                {
                    //将父流程置为running
                    //UNDONE:跨调度边界将父流程置为running是否会造成意外的调度？主要是并行节点情况，update可能导致意外的覆盖，应该调整为直接更新status字段？
                    parent.ChangeStatus(ProcessStatus.Running);
                    this._processService.Update(parent);
                    this._schedulerService.Add(new SubProcessCompleteResumption(parent, process));
                }
                #endregion

                //HACK:流程完成事件
                if (instance.IsComplete)
                    this._bus.RaiseProcessCompleted(new ProcessArgs(process));
                //HACK：节点开始事件
                if (humans.Count() > 0)
                    foreach (var h in humans)
                        this._bus.RaiseHumanActivityInstanceStarted(new ActivityInstanceArgs(h, process));
                //TODO:其他类型节点的开始事件
            }
            catch (Exception e)
            {
                this._log.Fatal("发生严重错误", e);
            }
        }
    }
}