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
    /// 流程类型
    /// </summary>
    public class ProcessType : EntityBase<Guid>, IAggregateRoot
    {
        /// <summary>
        /// 获取流程名
        /// </summary>
        public virtual string Name { get; private set; }
        /// <summary>
        /// 获取或设置流程描述
        /// </summary>
        public virtual string Description { get; set; }
        /// <summary>
        /// 获取或设置分组名
        /// <remarks>简单分组信息</remarks>
        /// </summary>
        public virtual string Group { get; set; }
        /// <summary>
        /// 获取创建时间
        /// </summary>
        public virtual DateTime CreateTime { get; private set; }
        /// <summary>
        /// 获取是否是当前版本
        /// </summary>
        public virtual bool IsCurrent { get; protected internal set; }
        /// <summary>
        /// 获取流程版本号
        /// </summary>
        public virtual string Version { get; private set; }

        private IList<ActivitySetting> _settings { get; set; }
        /// <summary>
        /// 获取节点设置
        /// </summary>
        public virtual IEnumerable<ActivitySetting> ActivitySettings { get { return this._settings.OrderBy(o => o.FlowNodeIndex).AsEnumerable(); } }
        /// <summary>
        /// 获取工作流定义
        /// </summary>
        public virtual WorkflowDefinition Workflow { get; private set; }

        protected ProcessType()
        {
            this.CreateTime = DateTime.Now;
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="name"></param>
        /// <param name="workflowDefinition"></param>
        /// <param name="settings"></param>
        public ProcessType(string name, WorkflowDefinition workflowDefinition, params ActivitySetting[] settings)
            : this()
        {
            this.Name = name;
            this.Workflow = workflowDefinition;
            this._settings = settings;

            this.Version = this.CreateTime.ToString("yyyyMMddHHmmss");

            this.Validate();
        }

        //在NTFE-BPM中保证节点名称在流程范围内唯一，公开方法一律使用activityName作为参数或查找依据

        /// <summary>
        /// 获取子流程节点设置信息
        /// </summary>
        /// <param name="activityName"></param>
        /// <returns></returns>
        public virtual SubProcessSetting GetSubProcessSetting(string activityName)
        {
            return this.GetActivitySetting(activityName) as SubProcessSetting;
        }
        /// <summary>
        /// 获取人工节点设置信息
        /// </summary>
        /// <param name="activityName"></param>
        /// <returns></returns>
        public virtual HumanSetting GetHumanSetting(string activityName)
        {
            return this.GetActivitySetting(activityName) as HumanSetting;
        }
        /// <summary>
        /// 获取Server节点设置信息
        /// </summary>
        /// <param name="activityName"></param>
        /// <returns></returns> 
        public virtual ServerSetting GetServerSetting(string activityName)
        {
            return this.GetActivitySetting(activityName) as ServerSetting;
        }
        /// <summary>
        /// 获取自定义节点设置信息
        /// </summary>
        /// <param name="activityName"></param>
        /// <returns></returns> 
        public virtual CustomSetting GetCustomSetting(string activityName)
        {
            return this.GetActivitySetting(activityName) as ServerSetting;
        }
        /// <summary>
        /// 获取节点设置信息
        /// </summary>
        /// <param name="activityName"></param>
        /// <returns></returns>
        public virtual ActivitySetting GetActivitySetting(string activityName)
        {
            return this._settings.FirstOrDefault(o => o.ActivityName == activityName);
        }
        //获取节点设置信息
        protected internal virtual ActivitySetting GetActivitySetting(int flowNodeIndex)
        {
            return this._settings.FirstOrDefault(o => o.FlowNodeIndex == flowNodeIndex);
        }

        private void Validate()
        {
            if (string.IsNullOrEmpty(this.Name))
                throw new InvalidOperationException("Name不能为空");
            if (this.Workflow == null)
                throw new InvalidOperationException("WorkflowDefinition不能为空");
        }

        public class WorkflowDefinition
        {
            /// <summary>
            /// 获取工作流定义的序列化文本
            /// </summary>
            public virtual string Serialized { get; private set; }
            protected WorkflowDefinition() { }
            public WorkflowDefinition(string serialized)
            {
                this.Serialized = serialized;
            }
        }
    }
}