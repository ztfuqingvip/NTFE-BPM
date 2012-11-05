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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Castle.Services.Transaction;
using NHibernate.Criterion;
using Taobao.Activities.Statements;
using CodeSharp.Core;
using CodeSharp.Core.Services;
using Taobao.Workflow.Activities.Statements;
using Taobao.Workflow.Activities.Hosting;

namespace Taobao.Workflow.Activities.Application
{
    /// <summary>
    /// 用于提供NTFE-BPM客户端服务接口实现
    /// <remarks>将作为远程服务提供</remarks>
    /// </summary>
    [CodeSharp.Core.Component]
    [Transactional]
    public class TFlowEngine : Client.ITFlowEngine, Management.ITFlowEngine
    {
        private ILog _log;
        private IUserService _userService;
        private IAgentService _agentService;
        private IWorkItemService _workItemService;
        private IProcessService _processService;
        private IProcessTypeService _processTypeService;
        private ITimeZoneService _timeZoneService;
        private IWorkflowParser _workflowParser;
        private IMethodInvoker _invoker;
        private ISchedulerService _schedulerService;
        public TFlowEngine(ILoggerFactory factory
            , IUserService userService
            , IAgentService agentService
            , IWorkItemService workItemService
            , IProcessService processService
            , IProcessTypeService processTypeService
            , ITimeZoneService timeZoneService
            , IWorkflowParser workflowParser
            , IMethodInvoker invoker
            ,ISchedulerService schedulerService)
        {
            this._log = factory.Create(typeof(TFlowEngine));
            this._userService = userService;
            this._agentService = agentService;
            this._workItemService = workItemService;
            this._processService = processService;
            this._processTypeService = processTypeService;
            this._timeZoneService = timeZoneService;
            this._workflowParser = workflowParser;
            this._invoker = invoker;
            this._schedulerService = schedulerService;
        }

        #region ITFlowEngine Members

        public Client.WorkItem GetWorkItem(long id, string user)
        {
            var w = this._workItemService.GetWorkItem(id, this.GetUser(user));
            return w == null ? null : this.Parse(w);
        }
        [Transaction(TransactionMode.Requires)]
        public Client.WorkItem OpenWorkItem(long id, string user)
        {
            return this.Parse(this._workItemService.Open(id, this.GetUser(user)));
        }
        public Client.WorkItem[] GetWorkItems(string user)
        {
            return this._workItemService.GetWorkItems(this.GetUser(user))
                .Select(o => this.Parse(o))
                .ToArray();//do not lazy
        }
        public Client.WorkItem[] GetWorkItemsByProcess(string user, Guid processId, string activityName)
        {
            return this._workItemService.GetWorkItems(this.GetUser(user), this.GetProcessById(processId), activityName)
                  .Select(o => this.Parse(o))
                  .ToArray();
        }
        [Transaction(TransactionMode.Requires)]
        public void ExecuteWorkItem(long workItemId, string user, string action, IDictionary<string, string> inputs)
        {
            this._workItemService.Execute(workItemId, this.GetUser(user), action, inputs);
        }
        [Transaction(TransactionMode.Requires)]
        public void RedirectWorkItem(long workItemId, string fromUser, string toUser)
        {
            this._workItemService.Redirect(workItemId, this.GetUser(fromUser), this.GetUser(toUser));
        }

        [Transaction(TransactionMode.Requires)]
        public Client.Process NewProcess(string processTypeName, string title, string originator, int priority, IDictionary<string, string> dataFields)
        {
            var p = new Process(title
                , this._processTypeService.GetProcessType(processTypeName)
                , this._userService.GetUserWhatever(originator)//HACK:发起流程时，用户不存在则创建用户
                , priority
                , dataFields);
            this._processService.Create(p);
            return this.Parse(p);
        }
        [Transaction(TransactionMode.Requires)]
        public Client.Process NewProcessWithAssignedId(Guid assignedProcessId, string processTypeName, string title, string originator, int priority, IDictionary<string, string> dataFields)
        {
            var p = new Process(title
                 , this._processTypeService.GetProcessType(processTypeName)
                 , this._userService.GetUserWhatever(originator)
                 , priority
                 , dataFields);
            this._processService.Create(p, assignedProcessId);
            return this.Parse(p);
        }
        public Client.Process GetProcess(Guid id)
        {
            var p = this._processService.GetProcess(id);
            return p == null ? null : this.Parse(p);
        }
        [Transaction(TransactionMode.Requires)]
        public void UpdateDataFields(Guid processId, IDictionary<string, string> overrides)
        {
            var process = this.GetProcessById(processId);
            if (overrides != null)
                overrides.ToList().ForEach(o => process.UpdateDataField(o.Key, o.Value));
            this._processService.Update(process);
        }
        public bool CanRevokeProcess(Guid processId, string originator)
        {
            var p = this.GetProcessById(processId);
            if (!p.Originator.UserName.Equals(originator, StringComparison.InvariantCultureIgnoreCase))
            {
                this._log.Debug("不允许撤销：不是发起人");
                return false;
            }
            if (this._processService.GetPreviousActivityInstance(p) != null)
            {
                this._log.Debug("不允许撤销：流程已经运行到其他节点");
                return false;
            }
            if (this._workItemService.GetWorkItems(p).FirstOrDefault(o => o.Status != WorkItemStatus.New) != null)
            {
                this._log.Debug("不允许撤销：当前节点已经开始被处理");
                return false;
            }
            return true;
            //是发起人且没有上一节点且任务都处于New
            //return p.Originator.UserName.Equals(originator, StringComparison.InvariantCultureIgnoreCase)
            //    && this._processService.GetPreviousActivityInstance(p) == null
            //    && this._workItemService.GetWorkItems(p).FirstOrDefault(o => o.Status == WorkItemStatus.New) == null;
        }
        [Transaction(TransactionMode.Requires)]
        public void RevokeProcess(Guid processId, string originator)
        {
            if (!this.CanRevokeProcess(processId, originator))
                throw new Exception("流程无法撤销");
            //撤销即删除流程
            this._processService.Delete(this.GetProcessById(processId));
        }
        public bool CanRollbackProcess(Guid processId, string previousActioner)
        {
            ActivityInstanceBase i;
            string reason;
            return this._processService.CanRollback(this.GetProcessById(processId)
                , this.GetUser(previousActioner)
                , out i
                , out reason);
        }
        [Transaction(TransactionMode.Requires)]
        public void RollbackProcess(Guid processId, string previousActioner)
        {
            this._processService.Rollback(this.GetProcessById(processId), this.GetUser(previousActioner));
        }

        [Transaction(TransactionMode.Requires)]
        public void WakeUpDelayedActivity(Guid processId, string activityName)
        {
            this._processService.WakeUpDelayedActivityInstance(this.GetProcessById(processId), activityName);
        }
        public Client.BooleanResult CanAppendHumanActivity(Guid processId, Client.AppendHumanMode mode, string appendActivityName)
        {
            string reason; HumanSetting setting;
            var result = this.CanAppendHumanActivity(processId, mode, appendActivityName, out setting, out reason);
            if (!result && this._log.IsDebugEnabled)
                this._log.Debug(reason);
            return new Client.BooleanResult() { Result = result, Reason = reason };
        }
        [Transaction(TransactionMode.Requires)]
        public void AppendHumanActivity(Guid processId, Client.AppendHumanMode mode, Client.AppendHumanSetting appendHumanSetting)
        {
            //HACK:动态节点出于重复考虑，总是会先移除重名的节点，此设计会存在意外的情况

            string reason;
            HumanSetting currentHumanSetting;
            if (!this.CanAppendHumanActivity(processId, mode, appendHumanSetting.ActivityName, out currentHumanSetting, out reason))
                throw new InvalidOperationException(reason);
            if (appendHumanSetting == null)
                throw new ArgumentNullException("humanSetting");

            var process = this.GetProcessById(processId);
            var currentProcessType = process.ProcessType;
            //获取所有节点设置的副本
            var clonedSettings = new List<ActivitySetting>(currentProcessType.ActivitySettings.Select(o => o.Clone()));
            var workflow = this._workflowParser.Parse(currentProcessType.Workflow.Serialized, clonedSettings);
            var originalWorkflowDefinition = currentProcessType.Workflow.Serialized;

            #region 查找节点信息
            var currentNode = this.GetFlowStep(workflow, currentHumanSetting.ActivityName);
            var currentSetting = clonedSettings.FirstOrDefault(o => o.ActivityName == currentNode.Action.DisplayName) as HumanSetting;
            var nextNode = this.GetNextFlowStep(workflow, currentNode);
            if (currentNode == null || currentSetting == null)
                throw new InvalidOperationException(string.Format(
                    "没有在流程中找到节点“{0}”的定义", currentHumanSetting.ActivityName));
            #endregion

            #region 修改当前节点
            clonedSettings.Remove(currentSetting);
            var finishRule = currentSetting.FinishRule.Scripts;
            //总是先移除避免多次重复追加
            finishRule.Remove(appendHumanSetting.EnterFinishRuleName);
            //新增完成规则用于满足后进入新节点
            //必须将新规则插入在第一个规则位置，避免原有规则的影响 用atMostOf?该细节不能出现在Model层
            finishRule = new Dictionary<string, string>() { 
            { appendHumanSetting.EnterFinishRuleName, string.Format("all('{0}')", appendHumanSetting.EnterAction) } }
                .Concat(finishRule)
                .ToDictionary(o => o.Key, o => o.Value);
            currentSetting = new HumanSetting(currentSetting.FlowNodeIndex
                , currentSetting.ActivityName
                , currentSetting.Actions
                , currentSetting.SlotCount
                , currentSetting.SlotMode
                , currentSetting.Url
                , currentSetting.StartRule
                , currentSetting.ActionerRule
                , new FinishRule(finishRule)
                , currentSetting.EscalationRule
                , currentSetting.IsChildOfActivity);
            clonedSettings.Add(currentSetting);
            #endregion

            #region 新增节点设置
            var newHumanSetting = new HumanSetting(0//此时无需设置实际值，最终创建流程类型时会由转换器填充实际值
                , appendHumanSetting.ActivityName
                , appendHumanSetting.Actions
                , appendHumanSetting.SlotCount
                , (HumanSetting.SlotDistributionMode)(int)appendHumanSetting.SlotMode
                , appendHumanSetting.Url
                , null
                , new HumanActionerRule(appendHumanSetting.ActionerRule)
                , this.GetFinishRule(appendHumanSetting)
                , null
                , false);
            clonedSettings.Add(newHumanSetting);
            #endregion

            #region 将新节点与左右节点连接
            FlowStep newNode = null;
            if (mode == Client.AppendHumanMode.Wait)
                newNode = WorkflowBuilder.CreateHuman(newHumanSetting
                    , newHumanSetting.ActivityName
                    , new GetUsers(appendHumanSetting.ActionerRule)
                    , null
                    , null
                    , currentNode);//总是返回当前节点
            else if (mode == Client.AppendHumanMode.Continues)
                newNode = WorkflowBuilder.CreateHuman(newHumanSetting
                    , newHumanSetting.ActivityName
                    , new GetUsers(appendHumanSetting.ActionerRule)
                    , workflow.CustomActivityResult
                    , nextNode == null
                    ? null
                    //HACK:下一节点的进入与否将根据新节点的完成规则而定，若满足则继续，若不满足则流程结束，常见于加签的情况
                    : new Dictionary<string, FlowNode>() { { appendHumanSetting.FinishRuleName, nextNode } }
                    , null);
            //总是先移除避免多次重复追加
            (currentNode.Next as FlowSwitch<string>).Cases.Remove(appendHumanSetting.EnterFinishRuleName);
            (currentNode.Next as FlowSwitch<string>).Cases.Add(appendHumanSetting.EnterFinishRuleName, newNode);
            //总是先移除避免多次重复追加
            workflow.Body.Nodes.Remove(this.GetFlowStep(workflow, newHumanSetting.ActivityName));
            //由于解析需要应同时补充此元数据
            workflow.Body.Nodes.Add(newNode);
            #endregion

            //创建新流程定义，动态变更
            this._processService.DynamicChangeProcessType(process
                , this.CreateProcessType(currentProcessType.Name
                , this._workflowParser.Parse(workflow, originalWorkflowDefinition)
                , this._workflowParser.Parse(clonedSettings)
                , currentProcessType.Description
                , currentProcessType.Group
                , false));
        }
        
        public Client.Agent[] GetAgents(string user)
        {
            return this._agentService.GetAgents(this.GetUser(user))
                .Select(o => this.Parse(o))
                .ToArray();
        }
        [Transaction(TransactionMode.Requires)]
        public void RevokeAllAgents(string user)
        {
            this._agentService.RevokeAll(this.GetUser(user));
        }
        [Transaction(TransactionMode.Requires)]
        public void RevokeAgent(string actAsUser, string user)
        {
            this._agentService.GetAgents(this.GetUser(actAsUser))
                .Where(o => o.User.UserName.Equals(user, StringComparison.InvariantCultureIgnoreCase))
                .ToList()
                .ForEach(o => this._agentService.Revoke(o));
        }
        [Transaction(TransactionMode.Requires)]
        public void CreateAgent(string user, string actAsUser, DateTime begin, DateTime end, string[] processTypeNames)
        {
            ProcessType[] types = null;
            ActingRange range = ActingRange.All;
            if (processTypeNames != null && processTypeNames.Length > 0)
            {
                types = processTypeNames.Select(o =>
                    this._processTypeService.GetProcessType(o)).Where(o => o != null).ToArray();
                //声明的流程类型都不存在则不进行创建
                if (types.Length == 0) return;
            }
            else
                range = ActingRange.All;

            this._agentService.Create(new Agent(this.GetUser(user)
                , this.GetUser(actAsUser)
                , begin
                , end
                , range
                , range == ActingRange.All ? null : types));
        }

        #endregion

        #region ITFlowEngine Management Members

        public Client.WorkItem[] GetAllWorkItemsByType(string processTypeName)
        {
            return this._workItemService.GetWorkItemsByProcessTypeName(processTypeName).Select(o => this.Parse(o)).ToArray();
        }
        public Client.WorkItemsInfo SearchWorkItems(Management.WorkItemQuery query, int pageIndex, int pageSize)
        {
            var criteria = DetachedCriteria.For<WorkItem>();

            var flag = false;
            //执行人 TODO:是否需要包含代理人逻辑？
            if (!string.IsNullOrWhiteSpace(query.Actioner) && (flag = true))
                criteria.Add(Expression.Eq("Actioner", this.GetUser(query.Actioner)));
            //流程类型
            if (!string.IsNullOrWhiteSpace(query.ProcessTypeName) && (flag = true))
                criteria.Add(Expression.Eq("_processTypeName", query.ProcessTypeName));
            //标题
            if (!string.IsNullOrWhiteSpace(query.ProcessTitle) && (flag = true))
                criteria.CreateCriteria("Process").Add(Expression.Like("Title", "%" + query.ProcessTitle + "%"));
            //状态
            if (query.Status != null && query.Status.Length > 0 && (flag = true))
                criteria.Add(Expression.In("Status", query.Status.Select(o => this.Parse(o)).ToArray()));
            //发起时间
            if (query.CreateFrom.HasValue && query.CreateTo.HasValue && (flag = true))
            {
                criteria.Add(Expression.Ge("CreateTime", query.CreateFrom.Value));
                criteria.Add(Expression.Le("CreateTime", query.CreateTo.Value));
            }

            long total;
            return flag
                ? new Client.WorkItemsInfo()
                {
                    WorkItems = this._workItemService
                        .GetWorkItems(criteria, pageIndex, pageSize, out total)
                        .Select(o => this.Parse(o))
                        .ToArray(),
                    Total = total
                }
                : new Client.WorkItemsInfo();
        }
        [Transaction(TransactionMode.Requires)]
        public void ReleaseWorkItem(long id)
        {
            this._workItemService.Release(this.GetWorkItemById(id));
        }

        [Transaction(TransactionMode.Requires)]
        public void CreateProcessType(string name, string workflowDefinition, string customSettingsDefinition, string description, string group)
        {
            this.CreateProcessType(name, workflowDefinition, customSettingsDefinition, description, group, true);
        }
        public string[] GetWorkflowDefinition(string name)
        {
            var type = this.GetProcessType(name);
            return new string[] { type.Workflow.Serialized, this._workflowParser.Parse(type.ActivitySettings) };
        }
        public Client.ProcessType[] GetProcessTypes()
        {
            return this._processTypeService.GetProcessTypes().Select(o => this.Parse(o)).ToArray();
        }
        public Client.ProcessType[] GetHistoriesOfProcessType(string name)
        {
            return this._processTypeService.GetHistories(name).Select(o => this.Parse(o)).ToArray();
        }
        public Client.ProcessType[] GetAllVersionsOfProcessType(string name)
        {
            var list = new List<ProcessType>();
            list.Add(this.GetProcessType(name));
            list.AddRange(this._processTypeService.GetHistories(name));
            return list.Select(o => this.Parse(o)).ToArray();
        }

        public Client.ProcessesInfo GetProcessesByKeyword(string key, int pageIndex, int pageSize)
        {
            long total;
            return new Client.ProcessesInfo()
            {
                Processes = this._processService.GetProcesses(key, pageIndex, pageSize
                    , new ProcessStatus[]{ ProcessStatus.Active
                    , ProcessStatus.Error
                    , ProcessStatus.New
                    , ProcessStatus.Running
                    , ProcessStatus.Stopped
                    , ProcessStatus.Completed}, out total)
                    .Select(o => this.Parse(o))
                    .ToArray(),
                Total = total
            };
        }
        public Client.ProcessesInfo SearchProcesses(Management.ProcessQuery query, int pageIndex, int pageSize)
        {
            var criteria = DetachedCriteria.For<Process>();
            var flag = false;
            //发起人
            if (!string.IsNullOrWhiteSpace(query.Originator) && (flag = true))
                criteria.Add(Expression.Eq("Originator", this.GetUser(query.Originator)));
            //流程类型 存在多版本问题应用ProcessTypeName对应
            if (!string.IsNullOrWhiteSpace(query.ProcessTypeName) && (flag = true))
                criteria.CreateCriteria("ProcessType").Add(Expression.Eq("Name", query.ProcessTypeName));
            //criteria.Add(Expression.Eq("ProcessType", this.GetProcessType(query.ProcessTypeName)));
            //标题
            if (!string.IsNullOrWhiteSpace(query.Title) && (flag = true))
                criteria.Add(Expression.Like("Title", "%" + query.Title + "%"));
            //状态
            if (query.Status != null && query.Status.Length > 0 && (flag = true))
                criteria.Add(Expression.In("Status", query.Status.Select(o => this.Parse(o)).ToArray()));
            //发起时间 起始
            if (query.CreateFrom.HasValue && (flag = true))
                criteria.Add(Expression.Ge("CreateTime", query.CreateFrom.Value));
            //发起时间 截止
            if (query.CreateTo.HasValue && (flag = true))
                criteria.Add(Expression.Le("CreateTime", query.CreateTo.Value));

            long total;
            return flag
                ? new Client.ProcessesInfo()
                {
                    Processes = this._processService
                        .GetProcesses(criteria, pageIndex, pageSize, out total)
                        .Select(o => this.Parse(o))
                        .ToArray(),
                    Total = total
                }
                : new Client.ProcessesInfo();
        }

        [Transaction(TransactionMode.Requires)]
        public void RedirectProcess(Guid processId, string activityName)
        {
            this._processService.Goto(this.GetProcessById(processId), activityName);
        }
        [Transaction(TransactionMode.Requires)]
        public void ChangeProcessType(Guid processId, string version)
        {
            var p = this.GetProcessById(processId);
            this._processService.ChangeProcessType(p, this.GetProcessTypeByVersion(p.ProcessType.Name, version));
        }
        [Transaction(TransactionMode.Requires)]
        public void SetCurrentProcessType(string name, string version)
        {
            this._processTypeService.SetAsCurrent(name, version);
        }
        [Transaction(TransactionMode.Requires)]
        public void StopProcess(Guid processId)
        {
            this._processService.Stop(this.GetProcessById(processId));
        }
        [Transaction(TransactionMode.Requires)]
        public void RestartProcess(Guid processId)
        {
            this._processService.Restart(this.GetProcessById(processId));
        }
        [Transaction(TransactionMode.Requires)]
        public void DeleteProcess(Guid processId)
        {
            this._processService.Delete(this.GetProcessById(processId));
        }
        [Transaction(TransactionMode.Requires)]
        public void RetryFaultProcess(Guid faultProcessId)
        {
            this._processService.Retry(this.GetProcessById(faultProcessId));
        }
        public Management.ScriptFunction[] GetScriptFunctions()
        {
            var list = this._invoker.GetMethods()
                .Where(o => o.Key != "getSuperior")
                .Select(o => new Management.ScriptFunction()
                {
                    Name = o.Key,
                    Description = o.Value.Description,
                    HasReutrnValue = !o.Value.Void,
                    Parameters = o.Value.Parameters.Select(p =>
                        new Management.ScriptFunctionParameter()
                        {
                            Name = p.Item1,
                            Type = p.Item2.FullName,
                            Description = p.Item3
                        }).ToArray()
                }).ToList();
            //HACK:由于本期没有实现良好的函数插件化机制，对内置方法特殊处理
            list.Add(new Management.ScriptFunction()
            {
                Name = "getSuperior",
                Description = "获取流程发起人的主管",
                HasReutrnValue = true,
                Parameters = new Management.ScriptFunctionParameter[] { }
            });
            return list.ToArray();
        }

        public Management.ErrorRecord[] GetErrors()
        {
            return this._schedulerService.GetErrorRecords().Select(o => this.Parse(o)).ToArray();
        }
        #endregion

        private User GetUser(string user)
        {
            return this._userService.GetUserWhatever(user);

            //if (u == null)
            //    throw new Exception("用户“" + user + "”不存在");
            //return u;
        }
        private ProcessType GetProcessType(string name)
        {
            var type = this._processTypeService.GetProcessType(name);

            if (type == null)
                throw new Exception("不存在名为“" + name + "”的流程定义");

            return type;
        }
        private ProcessType GetProcessTypeByVersion(string name, string version)
        {
            var type = this._processTypeService.GetProcessType(name, version);

            if (type == null)
                throw new Exception("没有找到版本为" + version + "的流程" + name);
            return type;
        }
        private Process GetProcessById(Guid id)
        {
            var p = this._processService.GetProcess(id);

            if (p == null)
                throw new Exception("不存在标识为“" + id + "”的流程实例");

            return p;
        }
        private WorkItem GetWorkItemById(long id)
        {
            var w = this._workItemService.GetWorkItem(id);

            if (w == null)
                throw new Exception("不存在标识为“" + id + "”的任务");

            return w;
        }

        private Client.WorkItem Parse(WorkItem workItem)
        {
            var setting = workItem.GetReferredSetting();
            return new Client.WorkItem()
            {
                ID = workItem.ID,
                Actioner = workItem.Actioner.UserName,
                ArrivedTime = workItem.ArrivedTime,
                CreateTime = workItem.CreateTime,
                OriginalActioner = workItem.OriginalActioner.UserName,
                Status = (Client.WorkItemStatus)(int)workItem.Status,
                ActivityName = workItem.ActivityName,

                Actions = setting != null ? setting.Actions : new string[] { },
                //HACK:任务链接的填充目前放在外围调用系统完成，无需在此直接定死
                Url = setting != null ? setting.Url : string.Empty,

                ProcessId = workItem.Process.ID,
                ProcessTitle = workItem.Process.Title,
                ProcessCreateTime = workItem.Process.CreateTime,
                ProcessTypeName = workItem.Process.ProcessType.Name,
            };
        }
        private Client.ProcessType Parse(ProcessType processType)
        {
            var activity = this._workflowParser.Parse(WorkflowBuilder.GetCacheKey(processType)
                , processType.Workflow.Serialized
                , processType.ActivitySettings);
            return new Client.ProcessType()
            {
                CreateTime = processType.CreateTime,
                Description = processType.Description,
                Name = processType.Name,
                Version = processType.Version,
                IsCurrent = processType.IsCurrent,
                Group = processType.Group,
                ActivityNames = processType.ActivitySettings.Select(o => o.ActivityName).ToArray(),
                DataFields = activity.Variables.Select(o => o.Name).ToArray()
            };
        }
        private Client.Process Parse(Process process)
        {
            return new Client.Process()
            {
                CreateTime = process.CreateTime,
                DataFields = process.GetDataFields(),
                ID = process.ID,
                Originator = process.Originator.UserName,
                Priority = process.Priority,
                ProcessType = this.Parse(process.ProcessType),
                Status = (Client.ProcessStatus)(int)process.Status,
                Title = process.Title
            };
        }
        private Client.Agent Parse(Agent agent)
        {
            return new Client.Agent()
            {
                ActAsUserName = agent.ActAs.UserName,
                BeginTime = agent.BeginTime,
                CreateTime = agent.CreateTime,
                EndTime = agent.EndTime,
                ProcessTypeNames = agent.ActingItems.Select(o => o.ProcessTypeName).ToArray(),
                Range = (Client.ActingRange)(int)agent.Range,
                UserName = agent.User.UserName
            };
        }
        private Client.ActivityInstance Parse(HumanActivityInstance instance)
        {
            return new Client.ActivityInstance()
            {
                ActivityName = instance.ActivityName,
                CreateTime = instance.CreateTime
            };
        }
        private Management.ErrorRecord Parse(ErrorRecord record)
        {
            return new Management.ErrorRecord()
            {
                ID = record.ID,
                CreateTime = record.CreateTime,
                Reason = record.Reason,
                Process = this.Parse(record.Process)
            };
        }

        private WorkItemStatus Parse(Client.WorkItemStatus status)
        {
            return (WorkItemStatus)(int)status;
        }
        private ProcessStatus Parse(Client.ProcessStatus status)
        {
            return (ProcessStatus)(int)status;
        }

        private FlowStep GetFlowStep(WorkflowActivity workflow, string activityName)
        {
            return workflow.Body.Nodes.FirstOrDefault(o =>
                o is FlowStep && (o as FlowStep).Action.DisplayName == activityName) as FlowStep;
        }
        private FlowStep GetNextFlowStep(WorkflowActivity workflow, FlowStep step)
        {
            var flowSwitch = step.Next as FlowSwitch<string>;
            return flowSwitch.Cases.Count > 0
                ? flowSwitch.Cases.First().Value as FlowStep
                : flowSwitch.Default as FlowStep;
        }
        private FinishRule GetFinishRule(Client.AppendHumanSetting setting)
        {
            return !string.IsNullOrWhiteSpace(setting.FinishRuleName)
                && !string.IsNullOrWhiteSpace(setting.FinishRuleBody)
                ? new FinishRule(new Dictionary<string, string>() { { setting.FinishRuleName, setting.FinishRuleBody } })
                : null;
        }
        private bool CanAppendHumanActivity(Guid processId
            , Client.AppendHumanMode mode
            , string appendActivityName
            , out HumanSetting currentHumanSetting
            , out string reason)
        {
            if (string.IsNullOrWhiteSpace(appendActivityName))
                throw new InvalidOperationException("appendActivityName不能为空");

            reason = string.Empty;
            var process = this.GetProcessById(processId);
            var processType = process.ProcessType;
            var workItems = this._workItemService.GetWorkItems(process);

            if (workItems.Count() != 1)
                reason = "当前节点可能不是人工节点";

            var human = workItems.First().ActivityInstance;
            currentHumanSetting = processType.GetHumanSetting(human.ActivityName);

            if (currentHumanSetting.ActivityName == appendActivityName)
                reason = "当前节点与期望新增的节点同名";
            else if (currentHumanSetting.IsChildOfActivity)
                reason = "当前节点是子节点不支持";
            else if (human.Actioners.Length > 1)
                reason = "当前节点是多人任务节点";
            else if (mode == Client.AppendHumanMode.Continues//HACK:AppendHumanMode.Continues模式下不支持多分支的节点，无法选取
                && currentHumanSetting.FinishRule != null
                && currentHumanSetting.FinishRule.Scripts != null
                && currentHumanSetting.FinishRule.Scripts.Count > 1)
                reason = "当前节点存在多分支";

            return string.IsNullOrWhiteSpace(reason);
        }
        private ProcessType CreateProcessType(string name
            , string workflowDefinition
            , string activitySettingsDefinition
            , string description
            , string group
            , bool current)
        {
            var type = new ProcessType(name
                , new ProcessType.WorkflowDefinition(workflowDefinition)
                , this._workflowParser.ParseActivitySettings(workflowDefinition, activitySettingsDefinition))
            {
                Description = description,
                Group = group
            };
            this._processTypeService.Create(type, current);
            return type;
        }
    }
}