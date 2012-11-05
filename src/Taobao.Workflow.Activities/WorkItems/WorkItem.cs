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
using CodeSharp.Core.DomainBase;
using Taobao.Workflow.Activities.Statements;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 任务信息
    /// </summary>
    public class WorkItem : EntityBase<long>, IAggregateRoot
    {
        /// <summary>
        /// 获取创建时间
        /// </summary>
        public virtual DateTime CreateTime { get; private set; }
        /// <summary>
        /// 获取任务到达时间
        /// </summary>
        public virtual DateTime ArrivedTime { get; private set; }
        /// <summary>
        /// 获取任务完成时间
        /// </summary>
        public virtual DateTime? FinishTime { get; private set; }
        /// <summary>
        /// 获取任务的原始执行人
        /// </summary>
        public virtual User OriginalActioner { get; private set; }
        /// <summary>
        /// 获取任务的当前执行人
        /// </summary>
        public virtual User Actioner { get; private set; }
        /// <summary>
        /// 获取执行结果
        /// </summary>
        public virtual string Result { get; private set; }
        /// <summary>
        /// 获取任务所在的流程
        /// </summary>
        public virtual Process Process { get; private set; }
        /// <summary>
        /// 获取任务状态
        /// </summary>
        public virtual WorkItemStatus Status { get; private set; }
        /// <summary>
        /// 获取任务所在的节点实例
        /// </summary>
        public virtual HumanActivityInstance ActivityInstance { get; private set; }

        /// <summary>
        /// 获取任务所在的节点名称
        /// </summary>
        public virtual string ActivityName { get; private set; }
        //任务查询优化字段，为任务冗余对应的类型名称便于定位指定流程类型下的任务，由于流程类型版本会发生变更
        private string _processTypeName { get; set; }

        protected WorkItem()
        {
            this.CreateTime = DateTime.Now;
            this.ArrivedTime = DateTime.Now;
            this.Status = WorkItemStatus.New;
        }
        public WorkItem(User actioner, Process process, HumanActivityInstance activityInstance)
            : this()
        {
            this.Actioner = this.OriginalActioner = actioner;
            this.Process = process;
            this.ActivityInstance = activityInstance;

            this.Validate();

            //冗余
            this._processTypeName = this.Process.ProcessType.Name;
            this.ActivityName = activityInstance.ActivityName;
        }
        /// <summary>
        /// 获取管关联的Human设置
        /// </summary>
        /// <returns></returns>
        public virtual HumanSetting GetReferredSetting()
        {
            return this.Process.ProcessType.GetHumanSetting(this.ActivityName);
        }
        //在许可范围内变更状态
        public virtual void ChangeStatus(WorkItemStatus status)
        {
            if (this.Status == WorkItemStatus.Executed
                || this.Status == WorkItemStatus.Canceled)
                throw new InvalidOperationException("该任务状态不可变更");
            this.Status = status;
        }
        //修改执行人，转交场景
        protected internal virtual void ChangeActioner(User actioner)
        {
            if (this.Actioner == actioner)
                return;
            this.Actioner = actioner;
        }
        //设置任务结果并完成任务
        protected internal virtual void MarkAsExecuted(string result)
        {
            this.ChangeStatus(WorkItemStatus.Executed);
            this.Result = result;
            this.FinishTime = DateTime.Now;
        }

        private void Validate()
        {
            if (this.Actioner == null || this.OriginalActioner == null)
                throw new InvalidOperationException("执行人不能为空");
            if (this.Process == null)
                throw new InvalidOperationException("Process不能为空");
            if (this.ActivityInstance == null)
                throw new InvalidOperationException("ActivityInstance不能为空");
            if (this.Process.ID != this.ActivityInstance.ProcessId)
                throw new InvalidOperationException("ActivityInstance对应的Process与指定的不一致");
        }
    }
}