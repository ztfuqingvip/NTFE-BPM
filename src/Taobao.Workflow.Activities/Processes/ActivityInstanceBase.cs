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

namespace Taobao.Workflow.Activities
{
    //注意：由于ActivityInstance对象同时作为对应Activity中的暂存对象，因此从Process不可导

    /// <summary>
    /// 用于记录流程运行中产生的节点实例信息
    /// </summary>
    public abstract class ActivityInstanceBase : CodeSharp.Core.DomainBase.EntityBase<long>
    { 
        //工作流节点实例标识 仅作记录和用于ntfe-core中的workflowActivityInstanceId关联，在一个Process内可能会重复
        private long _workflowActivityInstanceId { get; set; }
        /// <summary>
        /// 获取流程图索引
        /// </summary>
        public virtual int FlowNodeIndex { get; private set; }
        /// <summary>
        /// 获取节点名
        /// </summary>
        public virtual string ActivityName { get; private set; }

        /// <summary>
        /// 获取流程实例标识
        /// </summary>
        public virtual Guid ProcessId { get; private set; }
        /// <summary>
        /// 获取创建时间
        /// </summary>
        public virtual DateTime CreateTime { get; private set; }
        /// <summary>
        /// 获取完成时间
        /// </summary>
        public virtual DateTime? FinishTime { get; private set; }

        protected ActivityInstanceBase()
        {
            this.CreateTime = DateTime.Now;
        }
        protected ActivityInstanceBase(Guid processId
            , int flowNodeIndex
            , long workflowActivityInstanceId
            , string activityName)
            : this()
        {
            this.ProcessId = processId;
            this.FlowNodeIndex = flowNodeIndex;
            this._workflowActivityInstanceId = workflowActivityInstanceId;
            this.ActivityName = activityName;

            this.Validate();
        }

        /// <summary>
        /// 将节点实例设置为已完成
        /// </summary>
        public virtual void SetAsComplete()
        {
            this.FinishTime = DateTime.Now;
        }

        private void Validate()
        {
            if (this.ProcessId == Guid.Empty)
                throw new InvalidOperationException("ProcessId不合法");
            if (string.IsNullOrEmpty(this.ActivityName))
                throw new InvalidOperationException("ActivityName不能为空");
            AssertHelper.ThrowIfInvalidFlowNodeIndex(this.FlowNodeIndex);
            AssertHelper.ThrowIfInvalidActivityInstanceId(this._workflowActivityInstanceId);
        }
    }
}