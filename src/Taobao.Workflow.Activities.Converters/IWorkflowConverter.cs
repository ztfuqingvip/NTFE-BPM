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
using Taobao.Workflow.Activities;
using Taobao.Workflow.Activities.Statements;

namespace Taobao.Workflow.Activities.Converters
{
    /// <summary>
    /// 流程转换器接口
    /// </summary>
    public interface IWorkflowConverter
    {
        /// <summary>
        /// 将指定的工作流描述定义和规则描述定义转换为新工作流描述定义
        /// </summary>
        /// <param name="workflowDefinition">指定的工作流描述定义</param>
        /// <param name="activitySettingsDefinition">指定的规则描述定义</param>
        /// <returns></returns>
        string Parse(string workflowDefinition, string activitySettingsDefinition);
        /// <summary>
        /// 将指定的工作流描述定义和活动设置列表解析成NTFE-BPM活动树
        /// </summary>
        /// <param name="workflowDefinition">指定的工作流描述定义</param>
        /// <param name="activitySettings">活动设置列表</param>
        /// <returns></returns>
        WorkflowActivity Parse(string workflowDefinition, IList<ActivitySetting> activitySettings);
        /// <summary>
        /// 将指定的NTFE-BPM活动树转换为工作流描述定义文本
        /// </summary>
        /// <param name="activity">指定的NTFE-BPM活动树</param>
        /// /// <param name="originalWorkflowDefinition">原始工作流定义</param>
        /// <returns></returns>
        string Parse(WorkflowActivity activity, string originalWorkflowDefinition);
        /// <summary>
        /// 将指定的工作流描述定义和规则描述定义解析活动设置列表
        /// </summary>
        /// <param name="workflowDefinition">指定的工作流描述定义</param>
        /// <param name="activitySettingsDefinition">指定的规则描述定义</param>
        /// <returns></returns>
        IList<ActivitySetting> ParseActivitySettings(string workflowDefinition, string activitySettingsDefinition);
        /// <summary>
        /// 解析自定义活动定义
        /// </summary>
        /// <param name="activitySettings">活动设置列表</param>
        /// <returns></returns>
        string ParseActivitySettingsDefinition(IList<ActivitySetting> activitySettings);
    }
}
