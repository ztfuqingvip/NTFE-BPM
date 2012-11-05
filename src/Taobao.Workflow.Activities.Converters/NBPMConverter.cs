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
    /// BPM.NET文本转换器
    /// </summary>
    public class NBPMConverter : ConverterBase, IWorkflowConverter
    {
        string IWorkflowConverter.Parse(string workflowDefinition, string activitySettingsDefinition)
        {
            return null;
        }
        IList<ActivitySetting> IWorkflowConverter.ParseActivitySettings(string workflowDefinition, string activitySettingsDefinition)
        {
            return null;
        }
        WorkflowActivity IWorkflowConverter.Parse(string workflowDefinition, IList<ActivitySetting> activitySettings)
        {
            return null;
        }
        string IWorkflowConverter.Parse(WorkflowActivity activity, string originalWorkflowDefinition)
        {
            return null;
        }
    }
}
