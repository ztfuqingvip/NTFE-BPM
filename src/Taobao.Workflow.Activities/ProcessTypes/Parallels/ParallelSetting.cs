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
    /// 并行节点设置
    /// </summary>
    public class ParallelSetting : ActivitySetting
    {
        public override bool CanHaveChildren { get { return true; } }
        /// <summary>
        /// 获取完成条件
        /// </summary>
        public virtual string CompletionCondition { get; private set; }

        protected ParallelSetting() { }
        /// <summary>
        /// 初始化并行节点设置
        /// </summary>
        /// <param name="flowNodeIndex">所在流程图节点的索引</param>
        /// <param name="activityName">节点名称</param>
        /// <param name="completionCondition">并行节点完成条件</param>
        /// <param name="isChildOfActivity">是否是其他节点的子节点</param>
        public ParallelSetting(int flowNodeIndex
            , string activityName
            , string completionCondition
            , bool isChildOfActivity)
            : base(flowNodeIndex
            , activityName
            , isChildOfActivity)
        {
            this.CompletionCondition = completionCondition;
        }

        public override ActivitySetting Clone()
        {
            return new ParallelSetting(this.FlowNodeIndex
                , this.ActivityName
                , this.CompletionCondition
                , this.IsChildOfActivity);
        }
    }
}