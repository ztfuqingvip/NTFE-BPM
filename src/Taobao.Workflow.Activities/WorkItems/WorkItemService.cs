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
using Taobao.Workflow.Activities.Hosting;
using CodeSharp.Core.Services;
using CodeSharp.Core;
using Taobao.Workflow.Activities.Statements;

namespace Taobao.Workflow.Activities
{
    //目前接口的上下文/权限设计不够清晰，层次不明

    /// <summary>
    /// 流程任务服务对外接口
    /// </summary>
    public interface IWorkItemService
    {
        /// <summary>
        /// 创建任务
        /// </summary>
        /// <param name="workItem"></param>
        void Create(WorkItem workItem);
        /// <summary>
        /// 释放任务
        /// </summary>
        /// <param name="workItemId"></param>
        void Release(WorkItem workItem);

        //下列接口需要设计权限控制 引入Context?

        /// <summary>
        /// 打开/占用任务
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        WorkItem Open(long workItemId, User user);
        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="workItemId"></param>
        /// <param name="user"></param>
        /// <param name="action"></param>
        /// <param name="inputs">同时更新流程数据，可以为空</param>
        void Execute(long workItemId, User user, string action, IDictionary<string, string> inputs);
        /// <summary>
        /// 转交指定编号的任务
        /// </summary>
        /// <param name="workItemId"></param>
        /// <param name="fromUser"></param>
        /// <param name="toUser"></param>
        void Redirect(long workItemId, User fromUser, User toUser);

        #region 任务查询
        /// <summary>
        /// 获取待办任务
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        WorkItem GetWorkItem(long id);
        /// <summary>
        /// 获取用户的待办任务（不占用）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        WorkItem GetWorkItem(long id, User user);
        /// <summary>
        /// 获取指定流程的待办任务列表
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> GetWorkItems(Process process);
        /// <summary>
        /// 获取指定流程和节点的待办任务列表
        /// </summary>
        /// <param name="process"></param>
        /// <param name="activityName"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> GetWorkItems(Process process, string activityName);
        /// <summary>
        /// 获取用户的待办任务列表
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> GetWorkItems(User user);
        /// <summary>
        /// 获取指定流程类型名称的所有待办任务
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> GetWorkItemsByProcessTypeName(string typeName);
        /// <summary>
        /// 获取指定流程的待办任务
        /// </summary>
        /// <param name="user"></param>
        /// <param name="process"></param>
        /// <param name="activityName">不指定则忽略</param>
        /// <returns></returns>
        IEnumerable<WorkItem> GetWorkItems(User user, Process process, string activityName);
        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="criteria">查询对象</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="total"></param>
        /// <returns></returns> 
        IEnumerable<WorkItem> GetWorkItems(object criteria, int? pageIndex, int? pageSize, out long total);
        #endregion
    }
    /// <summary>
    /// 流程任务服务
    /// </summary>
    [Transactional]
    public class WorkItemService : IWorkItemService
    {
        /// <summary>
        /// 有效任务的状态
        /// </summary>
        public static readonly WorkItemStatus[] VALID_STATUS = new WorkItemStatus[] { WorkItemStatus.Open, WorkItemStatus.New };
        private static readonly string _no_task = "任务已经由其他人处理";

        private static IWorkItemRepository _repository;

        private ILog _log;
        private IAgentService _agentService;
        private IScriptParser _scriptParser;
        private IProcessService _processService;
        private ISchedulerService _resumptionService;
        private IEventBus _bus;
        static WorkItemService()
        {
            _repository = RepositoryFactory.GetRepository<IWorkItemRepository, long, WorkItem>();
        }
        public WorkItemService(ILoggerFactory factory
            , IScriptParser scriptParser
            , IAgentService agentService
            , IProcessService processService
            , ISchedulerService resumptionService
            , IEventBus bus)
        {
            this._log = factory.Create(typeof(ProcessService));
            this._scriptParser = scriptParser;
            this._agentService = agentService;
            this._processService = processService;
            this._resumptionService = resumptionService;
            this._bus = bus;
        }

        #region IWorkItemService Members

        [Transaction(TransactionMode.Requires)]
        void IWorkItemService.Create(WorkItem workItem)
        {
            _repository.Add(workItem);

            //任务创建事件
            if (workItem.Status == WorkItemStatus.New)
                this._bus.RaiseWorkItemArrived(new WorkItemArgs(workItem));

            this._log.InfoFormat(
                "创建任务：ID={0}|Actioner={1}|Bookmark={2}|HumanActivityInstanceId={3}|ActivityName={4}|ProcessId={5}|Status={6}"
                , workItem.ID
                , workItem.Actioner.UserName
                , workItem.ActivityInstance.ReferredBookmarkName
                , workItem.ActivityInstance.ID
                , workItem.ActivityInstance.ActivityName
                , workItem.Process.ID
                , workItem.Status);
        }

        [Transaction(TransactionMode.Requires)]
        void IWorkItemService.Release(WorkItem workItem)
        {
            if (workItem.Status != WorkItemStatus.Open)
                throw new InvalidOperationException(string.Format("不能释放处于{0}状态的任务", workItem.Status));
            workItem.ChangeStatus(WorkItemStatus.New);
            _repository.Update(workItem);

            //释放任务会释放slot，需要将该节点的NoSlot的任务置为New
            if (workItem.GetReferredSetting().IsUsingSlot)
                _repository.FindWorkItemsByActivityInstance(workItem.Process, workItem.ActivityInstance)
                    .Where(o => o.Status == WorkItemStatus.NoSlot).ToList()
                    .ForEach(o =>
                    {
                        o.ChangeStatus(WorkItemStatus.New);
                        _repository.Update(workItem);
                    });
        }

        [Transaction(TransactionMode.Requires)]
        WorkItem IWorkItemService.Open(long workItemId, User user)
        {
            WorkItem workItem = this.GetWorkItem(workItemId, user);

            if (workItem.GetReferredSetting().IsUsingSlot)
            {
                //lock
                var all = _repository.FindWorkItemsByActivityInstance(workItem.Process, workItem.ActivityInstance);
                var others = all.Where(o => o.ID != workItem.ID).ToList();
                //open
                this.OpenWorkItem(others, workItem);
            }
            else
                workItem.ChangeStatus(WorkItemStatus.Open);

            _repository.Update(workItem);
            return workItem;
        }

        [Transaction(TransactionMode.Requires)]
        void IWorkItemService.Execute(long workItemId, User user, string action, IDictionary<string, string> inputs)
        {
            //HACK:open和execute放在同一事务内 避免两次updlock造成死锁

            //lock
            var all = _repository.FindWorkItemsByActivityInstance(this.GetWorkItem(workItemId, user));
            var others = all.Where(o => o.ID != workItemId).ToList();
            var current = all.FirstOrDefault(o => o.ID == workItemId);

            HumanSetting setting = current.GetReferredSetting();

            //允许执行不存在的action，用以支持一些隐含的动态规则，如：动态节点功能
            //if (!setting.Actions.Contains(action))
            //    throw new InvalidOperationException(string.Format("不存在名为“{0}”的Action定义", action));

            //open
            this.OpenWorkItem(others, current);

            //人工节点执行结果
            var result = string.Empty;
            var script = string.Empty;
            //人工节点是否完成
            var isHumanDone = false;

            #region 1.更新流程变量
            if (inputs != null && inputs.Count > 0)
            {
                current.Process.UpdateDataFields(inputs);
                this._processService.Update(current.Process);
            }
            #endregion

            #region 2.测试完成规则
            //所有其他的任务
            var allOtherExecutions = others
                .Select(o => new DefaultHumanFinishRuleAsserter.Execution(o.Result, o.Status))
                .ToList();
            //补充未创建的任务
            var nexts = current.ActivityInstance.GetNextUsers(setting.SlotCount, setting.IsUsingSlot);
            if (nexts.Count() > 0)
            {
                foreach (var u in nexts)
                    allOtherExecutions.Add(new DefaultHumanFinishRuleAsserter.Execution(null, WorkItemStatus.New));

                if (this._log.IsDebugEnabled)
                    this._log.DebugFormat("由于存在未创建的任务，为任务判断而临时生成用户{0}的任务执行信息", string.Join("|", nexts));
            }
            else if (setting.IsUsingSlot
                && setting.SlotMode == HumanSetting.SlotDistributionMode.OneAtOnce
                && allOtherExecutions.Count < current.ActivityInstance.Actioners.Length
                && this._log.IsDebugEnabled)
            {
                this._log.DebugFormat("由于Slot={0}均已被占用，剩余未被创建的用户{1}的任务将被忽略"
                    , setting.SlotCount
                    , string.Join("|", current.ActivityInstance.GetUsersWhoNotReady()));
            }
            //下一个未激活的任务
            var next = current.ActivityInstance.GetNextUser(setting.SlotCount, setting.IsUsingSlot);
            var hasNext = !string.IsNullOrWhiteSpace(next);
            //所有其他任务是否已执行
            var allOtherExecuted = !hasNext
                && allOtherExecutions.FirstOrDefault(o =>
                    o.Status == WorkItemStatus.New || o.Status == WorkItemStatus.Open) == null;
            //没有完成规则的情况或其他任务都完成则完成
            //即使没有完成规则成立，只要其他任务都完成则节点完成
            if (allOtherExecuted)
                isHumanDone = true;
            //有规则的情况
            if (setting.FinishRule != null)
            {
                foreach (var s in setting.FinishRule.Scripts)
                {
                    //HACK:测试完成规则是否满足，遇到第一个满足的条件则退出
                    if (DependencyResolver.Resolve<IScriptParser>().EvaluateFinishRule(current
                        , allOtherExecutions
                        , current.Process.GetDataFields()
                        , action
                        , s.Value))
                    {
                        isHumanDone = true;
                        result = s.Key;
                        script = s.Value;
                        break;
                    }
                }
            }
            #endregion

            #region 3.更新任务状态
            current.MarkAsExecuted(action);
            _repository.Update(current);
            //若节点完成，撤销其他未执行的任务
            if (isHumanDone)
                others.ForEach(o =>
                {
                    if (o.Status == WorkItemStatus.New || o.Status == WorkItemStatus.Open)
                    {
                        o.ChangeStatus(WorkItemStatus.Canceled);
                        _repository.Update(o);
                    }
                });
            #endregion

            #region 4.若任务所在的人工环节完成
            if (isHumanDone)
            {
                //取消所有的未执行的 EscalationJobResumption
                this._resumptionService.CancelAllEscalationJob(current.Process, current.ActivityInstance.ID, current.ActivityName);
                //将人工节点实例置为完成
                current.ActivityInstance.SetAsComplete();
                this._processService.UpdateActivityInstance(current.ActivityInstance);
                //将流程置为运行状态
                current.Process.ChangeStatus(ProcessStatus.Running);
                this._processService.Update(current.Process);
                //并创建恢复请求
                this._resumptionService.Add(new BookmarkResumption(current.Process
                    , current.ActivityInstance.ActivityName
                    , current.ActivityInstance.ReferredBookmarkName
                    , result));

                //HACK:节点完成事件
                this._bus.RaiseHumanActivityInstanceCompleted(new ActivityInstanceArgs(current.ActivityInstance, current.Process));
            }
            else if (hasNext)
            {
                //将流程置为运行状态
                current.Process.ChangeStatus(ProcessStatus.Running);
                this._processService.Update(current.Process);
                //HACK:创建下一个任务
                this._resumptionService.Add(new WorkItemCreateResumption(current.Process, current.ActivityInstance));
            }
            #endregion

            this._log.InfoFormat(
                "用户{0}执行节点“{1}”的任务#{2}，Action={3}{4}{5}"
                , user.UserName
                , current.ActivityName
                , current.ID
                , action
                , isHumanDone ? "，满足人工节点完成条件“" + script + "”，将进入“" + result + "”" : ""
                , !isHumanDone && hasNext ? "，根据Slot分发规则OneAtOnce将为下一个用户" + next + "创建任务" : "");
        }

        [Transaction(TransactionMode.Requires)]
        void IWorkItemService.Redirect(long workItemId, User fromUser, User toUser)
        {
            WorkItemService.ThrowIfNull(fromUser, "fromUser");
            WorkItemService.ThrowIfNull(toUser, "toUser");

            WorkItem w = this.GetWorkItem(workItemId, fromUser);

            w.ChangeActioner(toUser);
            _repository.Update(w);

            this._log.InfoFormat("用户{0}将任务#{1}转交给用户{2}"
                , fromUser.UserName
                , workItemId
                , toUser.UserName);
        }

        WorkItem IWorkItemService.GetWorkItem(long id)
        {
            WorkItem w = _repository.FindBy(id);
            WorkItemService.ThrowIfInValid(w);
            return w;
        }
        //只获取有效状态的任务
        WorkItem IWorkItemService.GetWorkItem(long id, User user)
        {
            WorkItemService.ThrowIfNull(user, "user");

            if (id <= 0)
                throw new InvalidOperationException("id不合法");

            WorkItem w = _repository.FindBy(id);
            WorkItemService.ThrowIfInValid(w);

            if (w == null
                || w.Actioner.Equals(user)
                || this._agentService.GetActings(user).FirstOrDefault(o =>
                    o.CheckValid(w.Process.ProcessType)) != null)
                return w;
            else
                return null;
        }
        //只获取有效状态的任务，包含代理的
        IEnumerable<WorkItem> IWorkItemService.GetWorkItems(User user)
        {
            WorkItemService.ThrowIfNull(user, "user");

            var list = new List<WorkItem>();
            list.AddRange(_repository.FindAllBy(user, VALID_STATUS));
            //获取代理的任务
            this._agentService.GetActings(user).ToList().ForEach(o =>
            {
                if (!o.IsValid) return;
                list.AddRange(o.Range == ActingRange.All
                    ? _repository.FindAllBy(o.ActAs, VALID_STATUS)
                    : _repository.FindAllBy(o.ActAs, o.ActingItems.Select(p => p.ProcessTypeName).ToArray(), VALID_STATUS));
            });

            return list.Distinct().OrderByDescending(o => o.ArrivedTime);
        }
        IEnumerable<WorkItem> IWorkItemService.GetWorkItems(User user, Process process, string activityName)
        {
            WorkItemService.ThrowIfNull(user, "user");

            var list = new List<WorkItem>();
            list.AddRange(_repository.FindAllBy(user, process, activityName));
            //获取代理的任务
            this._agentService.GetActings(user).ToList().ForEach(o =>
            {
                if (o.IsValid && o.CheckValid(process.ProcessType))
                    list.AddRange(_repository.FindAllBy(o.ActAs, process, activityName));
            });

            return list.Distinct().OrderByDescending(o => o.ArrivedTime);
        }
        //只获取有效状态的任务
        IEnumerable<WorkItem> IWorkItemService.GetWorkItems(Process process)
        {
            return _repository.FindAllByProcess(process);
        }
        IEnumerable<WorkItem> IWorkItemService.GetWorkItems(Process process, string activityName)
        {
            return _repository.FindAllByProcessAndActivity(process, activityName);
        }
        IEnumerable<WorkItem> IWorkItemService.GetWorkItemsByProcessTypeName(string typeName)
        {
            return _repository.FindAllByProcessType(typeName);
        }
        IEnumerable<WorkItem> IWorkItemService.GetWorkItems(object criteria, int? pageIndex, int? pageSize, out long total)
        {
            return _repository.FindAllBy(criteria, pageIndex, pageSize, out total);
        }
        #endregion

        private WorkItem GetWorkItem(long id, User user)
        {
            WorkItem w = (this as IWorkItemService).GetWorkItem(id, user);

            if (w == null)
                throw new InvalidOperationException(string.Format("没有为用户{0}找到任务{1}", user.UserName, id));

            return w;
        }
        //仅处理open计算逻辑
        private void OpenWorkItem(List<WorkItem> others, WorkItem current)
        {
            HumanSetting setting = current.GetReferredSetting();

            if (!setting.IsUsingSlot)
            {
                current.ChangeStatus(WorkItemStatus.Open);
                return;
            }

            //已占用的slot数量
            var opened = others.Count(o =>
                o.Status == WorkItemStatus.Open || o.Status == WorkItemStatus.Executed);
            //slot占用完后将其他状态为New任务置为NoSlot
            if (opened + 1 >= setting.SlotCount)
            {
                others.ForEach(o =>
                {
                    if (o.Status != WorkItemStatus.New) return;
                    o.ChangeStatus(WorkItemStatus.NoSlot);
                    _repository.Update(o);
                });
            }
            //没有slot可占用
            if (opened >= setting.SlotCount)
                throw new InvalidOperationException(_no_task);

            current.ChangeStatus(WorkItemStatus.Open);
        }

        private static void ThrowIfNull(object obj, string name)
        {
            if (obj == null)
                throw new InvalidOperationException(name + "不能为空");
        }
        private static void ThrowIfInValid(WorkItem w)
        {
            if (w == null) return;
            if (!VALID_STATUS.Contains(w.Status))
                throw new InvalidOperationException(string.Format("任务处于无效状态{0}，不可操作", w.Status));
        }
    }
}