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
using CodeSharp.Core.Services;
using CodeSharp.Core;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 人工节点任务超时事件升级调度记录
    /// </summary>
    public class HumanEscalationResumption : WaitingResumption
    {
        /// <summary>
        /// 获取人工任务创建信息
        /// </summary>
        public virtual HumanActivityInstance HumanActivityInstance { get; private set; }

        protected HumanEscalationResumption() : base() { }
        public HumanEscalationResumption(Process process, DateTime? at, HumanActivityInstance humanActivityInstance)
            : base(process, WaitingResumption.MaxPriority, at)
        {
            this.HumanActivityInstance = humanActivityInstance;
            if (this.HumanActivityInstance == null)
                throw new InvalidOperationException("HumanActivityInstance不能为空");
        }

        public override Type Handle
        {
            get { return typeof(HumanEscalationWaitingResumption); }
        }
    }
    [CodeSharp.Core.Component]
    [Transactional]
    public class HumanEscalationWaitingResumption : IWaitingResumptionHandle
    {
        private ILog _log;
        private ISchedulerService _schedulerService;
        private IWorkItemService _workItemService;
        private IHumanEscalationHelper _helper;
        private IScriptParser _parser;
        public HumanEscalationWaitingResumption(ILoggerFactory factory
            , ISchedulerService schedulerService
            , IWorkItemService workItemService
            , IScriptParser parser
            , IHumanEscalationHelper helper)
        {
            this._log = factory.Create(typeof(HumanEscalationWaitingResumption));
            this._schedulerService = schedulerService;
            this._workItemService = workItemService;
            this._parser = parser;
            this._helper = helper;
        }

        #region IWaitingResumptionHandle 成员
        [Transaction(TransactionMode.Requires)]
        void IWaitingResumptionHandle.Resume(WaitingResumption waitingResumption)
        {
            var r = waitingResumption as HumanEscalationResumption;
            var process = r.Process;
            var activityName = r.HumanActivityInstance.ActivityName;
            var setting = process.ProcessType.GetHumanSetting(activityName);
            var workItems = this._workItemService.GetWorkItems(process, activityName);
            HumanEscalationRule rule = setting.EscalationRule;
            IDictionary<string, string> dataFields = process.GetDataFields();

            if (rule == null
                || !rule.IsValid
                || workItems.Count() == 0)
            {
                this._log.WarnFormat("流程“{0}”#{1}的节点“{2}”的超时事件升级规则无效或已经失效"
                    , process.Title
                    , process.ID
                    , r.HumanActivityInstance.ActivityName);
                return;
            }
            string to;
            if (rule.NotifyTemplateName != null)
            {
                //消息通知
                this._helper.Notify(process, workItems, rule.NotifyTemplateName);
                //重复通知
                if (rule.NotifyRepeatMinutes.HasValue)
                {
                    //从当前开始计算
                    var at = DateTime.Now.AddMinutes(rule.NotifyRepeatMinutes.Value);
                    this._schedulerService.Add(new HumanEscalationResumption(r.Process, at, r.HumanActivityInstance));
                    this._log.InfoFormat("由于设置了重复通知，再次创建超时事件升级调度项，将于{0}激活", at);
                }
            }
            else if (!string.IsNullOrEmpty(rule.RedirectTo)
                //HACK:【脚本使用】解析脚本获取实际转交目标
                && !string.IsNullOrWhiteSpace(to = this.Evaluate(rule.RedirectTo, process)))
                //将当前任务转交给其他人
                this._helper.Redirect(process, activityName, workItems, to);
            else if (!string.IsNullOrEmpty(rule.GotoActivityName))
                //强制跳转到指定节点
                this._helper.Goto(process, activityName, rule.GotoActivityName);
        }
        #endregion

        private string Evaluate(string script, Process process)
        {
            return this._parser.Evaluate(script, WorkflowBuilder.CreateDataFieldExtension(process));
        }

        /// <summary>
        /// 超时事件升级规则辅助
        /// </summary>
        public interface IHumanEscalationHelper
        {
            void Notify(Process process, IEnumerable<WorkItem> workItems, string templateName);
            void Redirect(Process process, string activityName, IEnumerable<WorkItem> workItems, string toUserName);
            void Goto(Process process, string from, string to);
        }
        /// <summary>
        /// 提供默认实现
        /// <remarks>要完善功能可派生此类</remarks>
        /// </summary>
        [Transactional]
        public class DefaultHumanEscalationHelper : IHumanEscalationHelper
        {
            private ILog _log;
            private IWorkItemService _workItemService;
            private IUserService _userService;
            private ISchedulerService _schedulerService;
            //此类是针对ProcessService的默认实现，故直接使用，不可参考
            private ProcessService _processService;
            public DefaultHumanEscalationHelper(ILoggerFactory factory
                , IWorkItemService workItemService
                , IUserService userService
                , ISchedulerService schedulerService
                , ProcessService processService)
            {
                this._log = factory.Create(this.GetType());
                this._workItemService = workItemService;
                this._userService = userService;
                this._schedulerService = schedulerService;
                this._processService = processService;
            }

            #region IHumanEscalationHelper Members
            public virtual void Notify(Process process, IEnumerable<WorkItem> workItems, string templateName)
            {
                this._log.Info("向未完成的任务执行人发送消息提醒");
                //不提供实现，由外部完成
            }
            [Transaction(TransactionMode.Requires)]
            public virtual void Redirect(Process process,string activityName, IEnumerable<WorkItem> workItems, string toUserName)
            {
                //直接转交，子类可扩展此实现，为转交增加历史记录等
                var user = this._userService.GetUserWhatever(toUserName);
                workItems.ToList().ForEach(o => this._workItemService.Redirect(o.ID, o.Actioner, user));
                this._log.InfoFormat("由于节点超时事件升级，将所有未完成任务转交给用户{0}", toUserName);
            }
            [Transaction(TransactionMode.Requires)]
            public virtual void Goto(Process process, string from, string to)
            {
                string current;
                int currentNode, targetNode;
                //直接使用ProcessService的Goto逻辑
                ProcessService.PerformGoto(this._processService
                    , this._schedulerService
                    , this._log
                    , process
                    , to
                    , out current
                    , out currentNode
                    , out targetNode);

                this._log.InfoFormat("由于节点超时事件升级，将流程实例“{0}”#{1}跳转从节点“{2}”#{3}到节点“{4}”#{5}"
                    , process.Title
                    , process.ID
                    , current
                    , currentNode
                    , to
                    , targetNode);
            }
            #endregion
        }
    }
}
