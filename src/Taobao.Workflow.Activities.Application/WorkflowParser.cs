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
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using CodeSharp.Core.Services;
using CodeSharp.Core;
using Taobao.Workflow.Activities.Statements;
using System.Xml.Linq;
using Taobao.Workflow.Activities.Converters;

namespace Taobao.Workflow.Activities.Application
{
    /// <summary>
    /// 工作流转换器实现
    /// </summary>
    [CodeSharp.Core.Component]
    public class WorkflowParser : IWorkflowParser
    {
        //简易工作流转换器cache
        private static readonly IDictionary<string, WorkflowActivity> _caches = new Dictionary<string, WorkflowActivity>();
        private ILog _log;
        private IWorkflowConverterFactory _workflowConverterFactory;
        public WorkflowParser(ILoggerFactory factory, IWorkflowConverterFactory workflowConverterFactory)
        {
            this._log = factory.Create(typeof(WorkflowParser));
            this._workflowConverterFactory = workflowConverterFactory;
        }

        #region IWorkflowParser Members
        public WorkflowActivity Parse(string workflowDefinition, IEnumerable<ActivitySetting> activitySettings)
        {
            var workflow = this._workflowConverterFactory
                .Create(workflowDefinition)
                .Parse(workflowDefinition, activitySettings.ToList());
            //总是先进行元数据初始化
            Taobao.Activities.ActivityUtilities.InitializeActivity(workflow);
            return workflow;
        }
        //带简易缓存
        public WorkflowActivity Parse(string key, string workflowDefinition, IEnumerable<ActivitySetting> activitySettings)
        {
            if (!_caches.ContainsKey(key))
                _caches.Add(key, this.Parse(workflowDefinition, activitySettings));
            return _caches[key];
        }
        //用于发布的
        public ActivitySetting[] ParseActivitySettings(string workflowDefinition, string activitySettingsDefinition)
        {
            var activitySettings = this._workflowConverterFactory
                .Create(workflowDefinition)
                .ParseActivitySettings(workflowDefinition, activitySettingsDefinition);
            return activitySettings.ToArray();
        }
        //将工作流转换为定义文本
        public virtual string Parse(WorkflowActivity activity, string originalWorkflowDefinition)
        {
            return this._workflowConverterFactory.Create().Parse(activity, originalWorkflowDefinition);
        }
        //将节点设置转换为定义文本
        public virtual string Parse(IEnumerable<ActivitySetting> activitySettings)
        {
            return this._workflowConverterFactory.Create().ParseActivitySettingsDefinition(activitySettings.ToList());
        }
        #endregion
    }
}