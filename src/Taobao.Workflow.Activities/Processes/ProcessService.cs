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
using CodeSharp.Core.RepositoryFramework;
using Castle.Services.Transaction;
using Taobao.Activities.Hosting;
using Taobao.Workflow.Activities.Hosting;
using CodeSharp.Core;
using CodeSharp.Core.Services;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 流程服务对外接口定义
    /// </summary>
    public interface IProcessService
    {
        /// <summary>
        /// 创建流程
        /// </summary>
        /// <param name="process"></param>
        void Create(Process process);
        /// <summary>
        /// 创建流程
        /// </summary>
        /// <param name="process"></param>
        /// <param name="assignedId">允许外部指定标识</param>
        void Create(Process process, Guid assignedId);
        /// <summary>
        /// 更新流程
        /// </summary>
        /// <param name="process"></param>
        void Update(Process process);
        /// <summary>
        /// 更新流程的工作流实例数据
        /// </summary>
        /// <param name="process"></param>
        /// <param name="instance"></param>
        void UpdateWorkflowInstanceOfProcess(Process process, Process.InternalWorkflowInstance instance);
        /// <summary>
        /// 获取流程
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Process GetProcess(Guid id);
        /// <summary>
        /// 获取流程对应的工作流实例数据
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        Process.InternalWorkflowInstance GetWorkflowInstance(Guid processId);
        /// <summary>
        /// 获取流程对应的工作流实例数据
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        Process.InternalWorkflowInstance GetWorkflowInstance(Process process);

        /// <summary>
        /// 变更流程版本
        /// </summary>
        /// <param name="process"></param>
        /// <param name="targetVersion"></param>
        void ChangeProcessType(Process process, ProcessType targetVersion);
        /// <summary>
        /// 动态变更流程类型而不改变流程实例当前运行状况
        /// </summary>
        /// <param name="process"></param>
        /// <param name="targetVersion"></param>
        void DynamicChangeProcessType(Process process, ProcessType targetVersion);
        /// <summary>
        /// 跳转到指定流程节点
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="activityName">目前节点名称</param>
        void Goto(Process process, string activityName);
        /// <summary>
        /// 对指定流程产生的所有错误进行重试
        /// </summary>
        /// <param name="faultProcess"></param>
        void Retry(Process faultProcess);
        /// <summary>
        /// 停止流程
        /// </summary>
        /// <param name="processId"></param>
        void Stop(Process process);
        /// <summary>
        /// 重新启动流程
        /// <remarks>Restart|Stop是一对操作</remarks>
        /// </summary>
        /// <param name="processId"></param>
        void Restart(Process process);
        /// <summary>
        /// 删除流程
        /// </summary>
        /// <param name="processId"></param>
        void Delete(Process process);
        /// <summary>
        /// 为上一个节点的操作人检查流程是否允许回滚到上一节点
        /// </summary>
        /// <param name="process"></param>
        /// <param name="previousActioner"></param>
        /// <param name="previous">返回上一个节点实例信息</param>
        /// <param name="reason">若不允许回滚则会返回原因</param>
        /// <returns></returns>
        bool CanRollback(Process process, User previousActioner, out ActivityInstanceBase previous, out string reason);
        /// <summary>
        /// 为上一个节点的操作人尝试回滚流程到上一节点
        /// </summary>
        /// <param name="process"></param>
        /// <param name="previousActioner"></param>
        void Rollback(Process process, User previousActioner);

        /// <summary>
        /// 强制取消/撤销与指定节点实例相关的运行时信息
        /// <remarks>
        /// 若是子流程节点则同时撤销对应的子流程实例CancelAllAbout(Process process)
        /// 若子流程仍处于Running状态则抛出异常
        /// </remarks>
        /// </summary>
        /// <param name="process"></param>
        /// <param name="activityInstance"></param>
        void CancelAllAbout(Process process, ActivityInstanceBase activityInstance);

        #region ActivityInstance 由于节点实例的时效性和对象可导性设计暂时未重新梳理，对于节点实例的获取一概使用以下专用api
        /// <summary>
        /// 获取指定节点实例
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ActivityInstanceBase GetActivityInstance(long id);
        /// <summary>
        /// 从父流程实例中获取启动指定子流程实例的节点实例
        /// </summary>
        /// <param name="process">父流程</param>
        /// <param name="subProcess">子流程</param>
        /// <returns></returns>
        SubProcessActivityInstance GetSubProcessActivityInstances(Process process, Process subProcess);
        /// <summary>
        /// 根据工作流节点实例标识获取实例
        /// <remarks>
        /// 由于工作流节点实例标识是根据工作流实例树产生，
        /// 若工作流发生重置会导致生成与旧实例相同的标识
        /// 因此此方法仅根据当前工作流实例树查找
        /// </remarks>
        /// </summary>
        /// <param name="process"></param>
        /// <param name="workflowActivityInstanceId">工作流节点实例标识</param>
        /// <returns></returns>
        ActivityInstanceBase GetActivityInstanceByWorkflowActivityInstanceId(Process process, long workflowActivityInstanceId);
        /// <summary>
        /// 获取流程的上一个节点实例
        /// <remarks>
        /// 由于并行节点的存在，若上一个节点是并行节点，则按时间倒序返回第一个并行子节点实例
        /// </remarks>
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        ActivityInstanceBase GetPreviousActivityInstance(Process process);
        /// <summary>
        /// 创建流程节点实例信息
        /// </summary>
        /// <param name="instance"></param>
        void CreateActivityInstance(ActivityInstanceBase instance);
        /// <summary>
        /// 更新流程节点实例信息
        /// </summary>
        /// <param name="instance"></param>
        void UpdateActivityInstance(ActivityInstanceBase instance);
        /// <summary>
        /// 强制唤醒处于延迟状态的节点实例
        /// </summary>
        /// <param name="process">流程</param>
        /// <param name="activityName">流程内的节点名</param>
        void WakeUpDelayedActivityInstance(Process process, string activityName);
        #endregion

        #region 查询
        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="criteria">查询对象</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        IEnumerable<Process> GetProcesses(object criteria, int? pageIndex, int? pageSize, out long total);
        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="key"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="status"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        IEnumerable<Process> GetProcesses(string key, int? pageIndex, int? pageSize, ProcessStatus[] status, out long total);
        #endregion
    }
    /// <summary>
    /// 流程服务
    /// </summary>
    [Transactional]
    public class ProcessService : IProcessService
    {
        private static IProcessRepository _repository;
        //避免循环依赖而使用此仓储
        private static IWorkItemRepository _workItemRepository;

        private ILog _log;
        private IWorkflowParser _parser;
        private IEventBus _eventBus;
        private ISchedulerService _schedulerService;

        static ProcessService()
        {
            _repository = RepositoryFactory.GetRepository<IProcessRepository, Guid, Process>();
            _workItemRepository = RepositoryFactory.GetRepository<IWorkItemRepository, long, WorkItem>();
        }
        public ProcessService(ILoggerFactory factory, IWorkflowParser parser, IEventBus eventBus, ISchedulerService schedulerService)
        {
            this._log = factory.Create(typeof(ProcessService));
            this._parser = parser;
            this._eventBus = eventBus;
            this._schedulerService = schedulerService;
        }

        #region IProcessService Members

        [Transaction(TransactionMode.Requires)]
        void IProcessService.Create(Process process)
        {
            (this as IProcessService).Create(process, Guid.NewGuid());
        }

        [Transaction(TransactionMode.Requires)]
        void IProcessService.Create(Process process, Guid assignedId)
        {
            process.SetId(assignedId);
            process.ChangeStatus(ProcessStatus.Running);
            //HACK:【重要】创建流程时预先设置调度标识以此作为按流程串行调度依据
            process.SetChargingBy(this._schedulerService.GetChargingBy(assignedId));
            _repository.Add(process);
            //创建流程启动请求
            this._schedulerService.Add(new ProcessStartResumption(process));

            this._log.InfoFormat("用户{0}发起类型为“{1}”的流程“{2}”#{3}，参数：{4}"
                , process.Originator.UserName
                , process.ProcessType.Name
                , process.Title
                , process.ID
                , string.Join("$", process.GetDataFields().Select(o => o.Key + "=" + o.Value)));
        }

        [Transaction(TransactionMode.Requires)]
        void IProcessService.Update(Process process)
        {
            _repository.Update(process);

            this._log.InfoFormat("更新流程实例“{0}”#{1}，状态：{2}，当前节点索引：{3}|参数：{4}"
               , process.Title
               , process.ID
               , process.Status
               , process.GetCurrentNode()
               , string.Join("$", process.GetDataFields().Select(o => o.Key + "=" + o.Value)));
        }

        [Transaction(TransactionMode.Requires)]
        void IProcessService.UpdateWorkflowInstanceOfProcess(Process process, Process.InternalWorkflowInstance instance)
        {
            _repository.UpdateWorkflowInstanceData(process.ID, instance.Serialized);
        }

        //只支持处于【错误&调度安全】流程用于修复时使用，正常流程无需切换版本
        [Transaction(TransactionMode.Requires)]
        void IProcessService.ChangeProcessType(Process process, ProcessType targetVersion)
        {
            if (!this.IsSchedulingSafeCauseByFault(process))
                throw new InvalidOperationException("只有处于Error状态的流程才允许变更流程类型");

            //当前
            var currentVersion = process.ProcessType;
            var current = process.GetCurrentNode();
            //切换流程类型
            process.ChangeProcessType(targetVersion);
            //切换后
            var target = process.GetCurrentNode();

            process.ChangeStatus(ProcessStatus.Running);
            this.CancelAllAbout(process);
            _repository.Update(process);
            //创建流程运行请求重新启动流程
            this._schedulerService.Add(new ProcessStartResumption(process));

            this._log.InfoFormat("将流程实例“{0}”#{1}的流程版本从{2}切换为{3}，当前节点从{4}指向{5}"
                , process.Title
                , process.ID
                , currentVersion.Version
                , targetVersion.Version
                , current
                , target);
        }

        //需要处于【安全状态或调度安全】状态
        [Transaction(TransactionMode.Requires)]
        void IProcessService.DynamicChangeProcessType(Process process, ProcessType targetVersion)
        {
            if (!this.IsStatusOrSchedulingSafe(process))
                throw new InvalidOperationException(string.Format("该流程处于{0}状态，不能进行版本变更", process.Status));
            //当前流程定义
            var currentVersion = process.ProcessType;
            //切换流程定义
            process.ChangeProcessType(targetVersion);
            // 保存修改
            _repository.Update(process);

            this._log.InfoFormat("将流程实例“{0}”#{1}的流程版本从{2}动态切换为{3}"
                , process.Title
                , process.ID
                , currentVersion.Version
                , targetVersion.Version);
        }

        //需要处于【安全状态或调度安全】状态
        [Transaction(TransactionMode.Requires)]
        void IProcessService.Goto(Process process, string activityName)
        {
            if (!this.IsStatusOrSchedulingSafe(process))
                throw new InvalidOperationException(string.Format("该流程处于{0}状态，不能进行跳转", process.Status));

            string current;
            int currentNode, targetNode;
            ProcessService.PerformGoto(this
                , this._schedulerService
                , this._log
                , process
                , activityName
                , out current
                , out currentNode
                , out targetNode);

            this._log.InfoFormat("将流程实例“{0}”#{1}跳转从节点“{2}”#{3}到节点“{4}”#{5}"
                , process.Title
                , process.ID
                , current
                , currentNode
                , activityName
                , targetNode);
        }

        //对处于【错误&调度安全】流程进行重试
        [Transaction(TransactionMode.Requires)]
        void IProcessService.Retry(Process faultProcess)
        {
            if (!this.IsSchedulingSafeCauseByFault(faultProcess))
                throw new InvalidOperationException("指定的流程没有错误无需重试");

            if (faultProcess.Status == ProcessStatus.Error)
            {
                faultProcess.ChangeStatus(ProcessStatus.Running);
                this.CancelAllAbout(faultProcess);
                _repository.Update(faultProcess);
                //创建错误恢复请求
                this._schedulerService.Add(new ErrorResumption(faultProcess, faultProcess.GetCurrentNode()));

                this._log.InfoFormat("将发生错误的流程实例“{0}”#{1}在节点#{2}进行重试"
                    , faultProcess.Title
                    , faultProcess.ID
                    , faultProcess.GetCurrentNode());
            }
            else
                //将调度错误逐个重试
                this._schedulerService.GetErrorRecords(faultProcess).ToList().ForEach(o => this._schedulerService.Retry(o));
        }

        //需要处于【安全状态或调度安全】状态
        [Transaction(TransactionMode.Requires)]
        void IProcessService.Stop(Process process)
        {
            if (!this.IsStatusOrSchedulingSafe(process))
                throw new InvalidOperationException(string.Format("该流程处于{0}状态，不能停止", process.Status));

            //设置停止
            process.ChangeStatus(ProcessStatus.Stopped);
            //取消所有调度请求 
            //此时并没有调度请求，只为防止意外情况
            this.CancelAllAbout(process);

            _repository.Update(process);

            this._log.InfoFormat("流程实例“{0}”#{1}在节点索引#{2}处被停止"
                , process.Title
                , process.ID
                , process.GetCurrentNode());
        }
        //针对Stop的重启
        [Transaction(TransactionMode.Requires)]
        void IProcessService.Restart(Process process)
        {
            if (process.Status != ProcessStatus.Stopped)
                throw new InvalidOperationException("指定的流程没有处于Stopped状态");

            process.ChangeStatus(ProcessStatus.Running);
            _repository.Update(process);
            //创建恢复请求
            this._schedulerService.Add(new ProcessStartResumption(process));

            this._log.InfoFormat("流程实例“{0}”#{1}在节点索引#{2}处被重新启动"
                , process.Title
                , process.ID
                , process.GetCurrentNode());
        }
        //需要处于【安全状态或调度安全】状态，同时删除所有子流程
        [Transaction(TransactionMode.Requires)]
        void IProcessService.Delete(Process process)
        {
            if (!this.IsStatusOrSchedulingSafe(process))
                throw new InvalidOperationException(string.Format("该流程处于{0}状态，不能删除", process.Status));

            //HACK:删除子流程会导致父流程无法完成，目前不支持该功能，可通过删除父流程或对父流程进行Goto操作来取消子流程的执行
            if (process.ParentProcessId.HasValue)
                throw new InvalidOperationException(string.Format("由于该流程实例是父流程实例#{0}的子流程，不允许删除", process.ParentProcessId));

            process.ChangeStatus(ProcessStatus.Deleted);
            this.CancelAllAbout(process);
            _repository.Update(process);

            this._log.InfoFormat("流程实例“{0}”#{1}被删除"
                , process.Title
                , process.ID);
        }

        //若是子流程节点则同时撤销对应的子流程实例CancelAllAbout(Process process)
        [Transaction(TransactionMode.Requires)]
        void IProcessService.CancelAllAbout(Process process, ActivityInstanceBase activityInstance)
        {
            //取消所有调度请求
            this._schedulerService.CancelAll(process, activityInstance.ID, activityInstance.ActivityName);
            //若是人工节点则取消所有人工任务
            if (activityInstance is HumanActivityInstance)
                _workItemRepository.CancelAll(process, activityInstance.ID);
            //若是子流程节点则撤销对应的子流程实例
            Process subProcess;
            var sub = activityInstance as SubProcessActivityInstance;
            if (sub != null
                && sub.SubProcessId.HasValue
                && (subProcess = _repository.FindBy(sub.SubProcessId.Value)) != null)
                this.CancelAllAbout(process, subProcess);

            this._log.InfoFormat("取消流程实例“{0}”#{1}的节点“{2}”#{3}相关的运行时信息"
                , process.Title
                , process.ID
                , activityInstance.ActivityName
                , activityInstance.ID);
        }

        [Transaction(TransactionMode.Requires)]
        void IProcessService.Rollback(Process process, User previousActioner)
        {
            ActivityInstanceBase previous;
            string reason;
            if (!(this as IProcessService).CanRollback(process, previousActioner, out previous, out reason))
                throw new InvalidOperationException(reason);
            //将流程goto到上一个节点作为回滚实现
            (this as IProcessService).Goto(process, previous.ActivityName);
        }
        bool IProcessService.CanRollback(Process process, User previousActioner, out ActivityInstanceBase previous, out string reason)
        {
            //TODO:是否要为rollback引入代理逻辑?
            reason = string.Empty;
            previous = null;
            if (!this.IsStatusOrSchedulingSafe(process, ProcessStatus.Active))
            {
                if (this._log.IsDebugEnabled)
                    this._log.DebugFormat("不允许回滚：{0}", reason = "只有处于活动状态的流程才允许回滚");
                return false;
            }

            previous = (this as IProcessService).GetPreviousActivityInstance(process);

            #region 常规判断
            if (previous == null)
            {
                if (this._log.IsDebugEnabled)
                    this._log.DebugFormat("不允许回滚：{0}", reason = "没有找到上一个节点信息");
                return false;
            }
            if (!(previous is HumanActivityInstance))
            {
                if (this._log.IsDebugEnabled)
                    this._log.DebugFormat("不允许回滚：{0}", reason = "上一个节点不是人工节点");
                return false;
            }
            if (process.ProcessType.GetActivitySetting(previous.ActivityName).IsChildOfActivity)
            {
                if (this._log.IsDebugEnabled)
                    this._log.DebugFormat("不允许回滚：{0}", reason = string.Format("上一节点“{0}”是其他节点的子节点，不支持回滚", previous.ActivityName));
                return false;
            }
            #endregion

            #region 上一个节点是人工节点才允许回滚
            var human = previous as HumanActivityInstance;
            if (!human.IsReady(previousActioner))
            {
                if (this._log.IsDebugEnabled)
                    this._log.DebugFormat("不允许回滚：{0}", reason = "传入的上一节点执行人并非实际执行人");
                return false;
            }
            if (human.Actioners.Length > 1)
            {
                if (this._log.IsDebugEnabled)
                    this._log.DebugFormat("不允许回滚：{0}", reason = string.Format("上一节点“{0}”执行人大于一人，不允许回滚", previous.ActivityName));
                return false;
            }
            if (_workItemRepository.FindAllByProcess(process).FirstOrDefault(o => o.Status != WorkItemStatus.New) != null)
            {
                if (this._log.IsDebugEnabled)
                    this._log.DebugFormat("不允许回滚：{0}", reason = string.Format("由于流程当前任务已开始被处理，无法回滚"));
                return false;
            }
            #endregion

            //TODO:目前未考虑子流程节点的情况

            return true;
        }

        #endregion

        #region Process查询
        Process IProcessService.GetProcess(Guid id)
        {
            var p = _repository.FindBy(id);
            return p == null || p.Status == ProcessStatus.Deleted ? null : p;
        }
        Process.InternalWorkflowInstance IProcessService.GetWorkflowInstance(Guid processId)
        {
            return new Process.InternalWorkflowInstance(_repository.FindWorkflowInstanceData(processId));
        }
        Process.InternalWorkflowInstance IProcessService.GetWorkflowInstance(Process process)
        {
            return (this as IProcessService).GetWorkflowInstance(process.ID);
        }
        IEnumerable<Process> IProcessService.GetProcesses(object criteria, int? pageIndex, int? pageSize, out long total)
        {
            return _repository.FindProcesses(criteria, pageIndex, pageSize, out total);
        }
        IEnumerable<Process> IProcessService.GetProcesses(string key, int? pageIndex, int? pageSize, ProcessStatus[] status, out long total)
        {
            return _repository.FindProcesses(key, pageIndex, pageSize, status, out total);
        }
        #endregion

        #region ActivityInstance
        ActivityInstanceBase IProcessService.GetActivityInstance(long id)
        {
            return _repository.FindActivityInstance(id);
        }
        SubProcessActivityInstance IProcessService.GetSubProcessActivityInstances(Process process, Process subProcess)
        {
            return _repository.FindSubProcessActivityInstance(process, subProcess);
        }
        ActivityInstanceBase IProcessService.GetActivityInstanceByWorkflowActivityInstanceId(Process process, long workflowActivityInstanceId)
        {
            return _repository.FindActivityInstance(process, workflowActivityInstanceId);
        }
        //获取当前节点的上一个节点实例
        ActivityInstanceBase IProcessService.GetPreviousActivityInstance(Process process)
        {
            var current = process.GetCurrentNode();
            //将会获取到所有节点实例，应按倒序获取当前运行中的
            var all = _repository.FindAllActivityInstances(process).OrderByDescending(o => o.CreateTime);
            //时间倒序查找第一个不等于当前节点的节点实例
            foreach (var i in all)
                if (i.FlowNodeIndex != current)
                    return i;
            return null;
        }
        [Transaction(TransactionMode.Requires)]
        void IProcessService.CreateActivityInstance(ActivityInstanceBase instance)
        {
            _repository.AddActivityInstance(instance);

            this._log.InfoFormat(
                "创建节点实例信息：{0}|ActivityInstanceId={1}|ActivityName={2}|FlowNodeIndex={3}|ProcessId={4}"
                , instance
                , instance.ID
                , instance.ActivityName
                , instance.FlowNodeIndex
                , instance.ProcessId);
        }
        [Transaction(TransactionMode.Requires)]
        void IProcessService.UpdateActivityInstance(ActivityInstanceBase instance)
        {
            _repository.UpdateActivityInstance(instance);

            this._log.InfoFormat("更新流程实例#{0}的节点实例“{1}”#{2}"
                , instance.ProcessId
                , instance.ActivityName
                , instance.ID);
        }
        [Transaction(TransactionMode.Requires)]
        void IProcessService.WakeUpDelayedActivityInstance(Process process, string activityName)
        {
            if (string.IsNullOrWhiteSpace(activityName))
                throw new InvalidOperationException("activityName不能为空");

            var r = this._schedulerService.GetValidWaitingResumptions(process)
                .FirstOrDefault(o =>
                    o is BookmarkResumption
                    && activityName.Equals((o as BookmarkResumption).ActivityName)
                    && (o as BookmarkResumption).At.HasValue);

            if (r == null)
                throw new InvalidOperationException(string.Format("没有找到指定流程在指定节点“{0}”的延迟信息", activityName));
            //已处于唤醒状态无需继续
            if (r.CanResumeAtNow)
                return;

            //立即唤醒
            r.WakeUpAt(DateTime.Now);
            this._schedulerService.Update(r);

            this._log.InfoFormat("强制唤醒流程实例#{0}的节点实例“{1}”，BookmarkResumption#{2}"
                , r.Process.ID
                , (r as BookmarkResumption).ActivityName
                , r.ID);
        }
        #endregion

        //强制取消与指定流程相关的运行时信息，若存在子流程则逐级取消,并且将子流程置为删除状态
        /* 关于CancelAllAbout的时机可能导致的调度问题：
         * 主要是子流程的情况，对父流程的Goto等操作会促使子流程的cancel并被置为deleted
         * 此时若子流程处于running状态会导致调度项被执行而流程已经删除，
         * 目前为避开这类并发问题，采用异常方式阻止cancel逻辑完成，保证所有涉及调度的行为都在安全状态下完成
         */
        private void CancelAllAbout(Process process)
        {
            this.CancelAllAbout(process, 0);
        }
        private void CancelAllAbout(Process process, int depth)
        {
            if (depth++ >= 100)
                throw new InvalidOperationException("递归执行CancelAllAbout时超出了允许的最大深度100");

            //1.取消所有调度运行时数据
            this._schedulerService.CancelAll(process);
            //2.取消所有任务
            _workItemRepository.CancelAll(process);

            this._log.InfoFormat("取消流程实例“{0}”#{1}相关的运行时信息"
                , process.Title
                , process.ID);

            //撤销所有子流程
            _repository.FindSubProcesses(process).ToList().ForEach(o =>
                this.CancelAllAbout(process, o, depth));
        }
        private void CancelAllAbout(Process parent, Process sub)
        {
            this.CancelAllAbout(parent, sub, 0);
        }
        private void CancelAllAbout(Process parent, Process sub, int depth)
        {
            //子流程删除只需要处于调度项安全即可
            if (!this.IsSchedulingSafe(sub))
                throw new InvalidOperationException(string.Format(
                    "流程“{0}”#{1}的子流程“{2}”#{3}处于{4}状态，无法将其撤销", parent.Title, parent.ID, sub.Title, sub.ID, sub.Status));
            //删除子流程
            sub.ChangeStatus(ProcessStatus.Deleted);
            _repository.Update(sub);
            this._log.InfoFormat("流程“{0}”#{1}的子流程“{2}”#{3}被置为删除状态", parent.Title, parent.ID, sub.Title, sub.ID);
            this.CancelAllAbout(sub, depth);
        }
        //HACK:判断流程是否处于调度安全状态或指定的流程状态
        private bool IsStatusOrSchedulingSafe(Process process)
        {
            return process.Status == ProcessStatus.Active
                || process.Status == ProcessStatus.Error
                || process.Status == ProcessStatus.Stopped
                || this.IsSchedulingSafe(process);
        }
        private bool IsStatusOrSchedulingSafe(Process process, params ProcessStatus[] status)
        {
            return status.Contains(process.Status) || this.IsSchedulingSafe(process);
        }
        //判断流程是否由于发生错误而处于调度安全状态
        private bool IsSchedulingSafeCauseByFault(Process process)
        {
            return process.Status == ProcessStatus.Error
                || (this._schedulerService.GetErrorRecords(process).Count() > 0
                && this.IsSchedulingSafe(process));
        }
        //HACK:【重要】判断流程是否处于调度安全，由于延迟调度的存在，流程始终处于running，在合理时间间隔内可以认为是调度安全，这样的设计是为了简化流程状态和调度的关系
        private bool IsSchedulingSafe(Process process)
        {
            //延迟调度对单流程串行调度的影响点？是否需要按调度项生成顺序和at时间顺序来综合确定调度顺序？
            //延迟调度场景：升级规则，节点延迟书签，目前而言只要不包含cancel逻辑的延迟调度就不会影响
            //调度时已经忽略了仍在延迟期的调度项
            return !this._schedulerService.AnyValidAndUnExecutedAndNoErrorResumptionsExceptDelayAt(process, DateTime.Now.AddMinutes(2));
        }

        //仅抽取ProcessService实现的Goto主体逻辑
        internal static void PerformGoto(ProcessService processService
            , ISchedulerService schedulerService
            , ILog log
            , Process process
            , string activityName
            , out string current
            , out int currentNode
            , out int targetNode)
        {
            var setting = process.ProcessType.GetActivitySetting(activityName);
            if (setting == null)
                throw new InvalidOperationException("没有找到指定节点“" + activityName + "”的定义");
            if (setting.IsChildOfActivity)
                throw new InvalidOperationException("不能跳转至子节点“" + activityName + "”");

            currentNode = process.GetCurrentNode();
            try { current = process.ProcessType.GetActivitySetting(currentNode).ActivityName; }
            catch (Exception e)
            {
                current = "未找到定义";
                log.Warn("查找流程当前节点设置信息时异常", e);
            }
            //0.更新currentnode指向目标节点
            process.UpdateCurrentNode(targetNode = setting.FlowNodeIndex);
            process.ChangeStatus(ProcessStatus.Running);
            _repository.Update(process);
            //1.取消所有相关信息
            processService.CancelAllAbout(process);
            //2.重新创建恢复请求
            schedulerService.Add(new ProcessStartResumption(process));
        }

        //UNDONE:为保证“绝对”调度安全，IsSchedulingSafe增加更新锁来解决并发同步，同时确保每个流程实例在同一时刻只会被一个线程进行更新/调度等操作
    }
}