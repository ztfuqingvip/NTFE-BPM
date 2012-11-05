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
    /// <summary>
    /// 子流程节点设置
    /// </summary>
    public class SubProcessSetting : CustomSetting
    {
        /// <summary>
        /// 获取子流程类型名称
        /// </summary>
        public virtual string SubProcessTypeName { get; private set; }

        protected SubProcessSetting() : base() { }
        /// <summary>
        /// 初始化子流程节点设置
        /// </summary>
        /// <param name="flowNodeIndex">所在流程图节点的索引</param>
        /// <param name="subProcessTypeName">子流程类型名称</param>
        /// <param name="activityName"></param>
        /// <param name="startRule">开始规则，不设置则留空</param>
        /// <param name="finishRule">完成规则，不设置则留空</param>
        /// <param name="isChildActivity">是否是子节点</param>
        public SubProcessSetting(int flowNodeIndex
            , string subProcessTypeName
            , string activityName
            , StartRule startRule
            , FinishRule finishRule
            , bool isChildActivity)
            : base(flowNodeIndex
            , activityName
            , startRule
            , finishRule
            , isChildActivity)
        {
            this.SubProcessTypeName = subProcessTypeName;

            this.Validate();
        }
        public override ActivitySetting Clone()
        {
            //TODO:浅拷贝，修改为深拷贝
            //return this.MemberwiseClone() as ActivitySetting;
            return new SubProcessSetting(this.FlowNodeIndex, this.SubProcessTypeName, this.ActivityName, this.StartRule, this.FinishRule, this.IsChildOfActivity);
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.SubProcessTypeName))
                throw new InvalidOperationException("SubProcessTypeName不能为空");
        }
    }
}
