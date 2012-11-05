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
    /// Server/自动节点设置
    /// </summary>
    public class ServerSetting : CustomSetting
    {
        /// <summary>
        /// 获取执行内容的结果输出到流程变量名
        /// </summary>
        public virtual string ResultTo { get; private set; }
        /// <summary>
        /// 获取节点执行内容
        /// </summary>
        public virtual string Script { get; private set; }

        protected ServerSetting() : base() { }
        /// <summary>
        /// 初始化自动节点设置
        /// </summary>
        /// <param name="flowNodeIndex">所在流程图节点的索引</param>
        /// <param name="activityName">节点名称</param>
        /// <param name="script">执行内容</param>
        /// <param name="to">结果输出变量名，不输出则留空</param>
        /// <param name="startRule">开始规则，不设置则留空</param>
        /// <param name="finishRule">完成规则，不设置则留空</param>
        /// <param name="isChildOfActivity">是否是子节点</param>
        public ServerSetting(int flowNodeIndex
            , string activityName
            , string script
            , string to
            , StartRule startRule
            , FinishRule finishRule
            , bool isChildOfActivity)
            : base(flowNodeIndex
            , activityName
            , startRule
            , finishRule
            , isChildOfActivity)
        {
            this.Script = script;
            this.ResultTo = to;
        }

        public override ActivitySetting Clone()
        {
            return new ServerSetting(this.FlowNodeIndex
                , this.ActivityName
                , this.Script
                , this.ResultTo
                , this.StartRule
                , this.FinishRule
                , this.IsChildOfActivity);
        }
    }
}