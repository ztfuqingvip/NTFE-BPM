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

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 流程节点设置信息
    /// <remarks>
    /// 相当于activity基本定义
    /// 包含基本的active信息
    /// </remarks>
    /// </summary>
    public abstract class ActivitySetting : EntityBase<Guid>
    {
        /// <summary>
        /// 获取所在流程图节点的索引
        /// FlowNodeIndex
        /// </summary>
        public virtual int FlowNodeIndex { get; private set; }
        /// <summary>
        /// 获取节点名称
        /// </summary>
        public virtual string ActivityName { get; private set; }
        /// <summary>
        /// 获取节点是否是其他节点的子节点
        /// <remarks>处于容器节点中</remarks>
        /// </summary>
        public virtual bool IsChildOfActivity { get; private set; }

        /// <summary>
        /// 获取节点是否允许有子节点
        /// </summary>
        public virtual bool CanHaveChildren { get { return false; } }

        protected ActivitySetting() { }
        public ActivitySetting(int flowNodeIndex
            , string activityName
            , bool isChildOfActivity)
        {
            this.FlowNodeIndex = flowNodeIndex;
            this.ActivityName = activityName;
            this.IsChildOfActivity = isChildOfActivity;

            this.Validate();
        }

        public abstract ActivitySetting Clone();

        //仅用于首次发布流程时修正索引
        protected internal virtual void SetFlowNodeIndex(int i)
        {
            this.FlowNodeIndex = i;
            AssertHelper.ThrowIfInvalidFlowNodeIndex(this.FlowNodeIndex);
        }

        private void Validate()
        {
            if (string.IsNullOrEmpty(this.ActivityName))
                throw new InvalidOperationException("ActivityName不能为空");
            AssertHelper.ThrowIfInvalidFlowNodeIndex(this.FlowNodeIndex);
        }
    }
}